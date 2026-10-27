using System;
using System.Collections.Generic;
using System.Threading;

namespace Tail2Jump.FileIO
{
  /// <summary>
  /// Thread-safe cache of log lines. Lines are stored as (file name, content)
  /// pairs in a ring buffer: no shifting when the capacity is reached, and the
  /// "file|content" string is only built for the lines requested by the UI.
  /// </summary>
  public class TailLineCache : ITailLineCache
  {
    public const int DefaultMaxLines = 200_000;

    private const int ReplayChunkSize = 4096;

    private readonly LineRingBuffer _lines;
    private ReaderWriterLockSlim? _locker;

    public TailLineCache(int maxLines = DefaultMaxLines)
    {
      if (maxLines <= 0) throw new ArgumentOutOfRangeException(nameof(maxLines));

      _lines = new LineRingBuffer(maxLines);
      _locker = new ReaderWriterLockSlim();
    }

    public int LineIndexed => _lines.LineIndexed;

    public int FirstLineIndex => _lines.FirstLineIndex;

    public string[] GetLines(int start, int len)
    {
      if (len < 0 || _locker == null) return Array.Empty<string>();

      var locker = _locker;
      locker.EnterReadLock();
      try
      {
        if (start < 0 || start >= _lines.LineIndexed)
        {
          return Array.Empty<string>();
        }

        if (start < _lines.FirstLineIndex) start = _lines.FirstLineIndex;

        int count = Math.Min(len, _lines.LineIndexed - start);
        if (count <= 0) return Array.Empty<string>();

        var result = new string[count];
        for (int i = 0; i < count; i++)
        {
          result[i] = Format(_lines[start + i]);
        }

        return result;
      }
      finally
      {
        locker.ExitReadLock();
      }
    }

    public void Clear()
    {
      if (_locker == null) return;

      var locker = _locker;
      locker.EnterWriteLock();
      try
      {
        _lines.Clear();
      }
      finally
      {
        locker.ExitWriteLock();
      }
    }

    public void AddData(TailEventData tailData, string? filter = null)
    {
      if (tailData == null || _locker == null) return;

      var lines = tailData.Lines;
      if (lines.Count == 0) return;

      string fileName = tailData.FileName;
      bool fileNameMatches = filter == null ||
                             (fileName.Length > 0 &&
                              fileName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

      var locker = _locker;
      locker.EnterWriteLock();
      try
      {
        if (fileNameMatches)
        {
          for (int i = 0; i < lines.Count; i++)
          {
            _lines.Add(new TailLineEntry(fileName, lines[i]));
          }
        }
        else
        {
          foreach (string line in lines)
          {
            if (line.IndexOf(filter!, StringComparison.OrdinalIgnoreCase) >= 0)
            {
              _lines.Add(new TailLineEntry(fileName, line));
            }
          }
        }
      }
      finally
      {
        locker.ExitWriteLock();
      }
    }

    public void AddData(ITailLineCache other, string? filter)
    {
      if (other == null || _locker == null || ReferenceEquals(other, this)) return;

      if (other is TailLineCache source)
      {
        source.CopyTo(this, filter);
        return;
      }

      // Unknown cache implementation: replay through the public API.
      int total = other.LineIndexed;
      for (int start = other.FirstLineIndex; start < total; start += ReplayChunkSize)
      {
        string[] lines = other.GetLines(start, Math.Min(ReplayChunkSize, total - start));
        if (lines.Length == 0) break;

        AppendFormattedLines(lines, filter);
      }
    }

    private void AppendFormattedLines(IEnumerable<string> lines, string? filter)
    {
      var locker = _locker;
      if (locker == null) return;

      locker.EnterWriteLock();
      try
      {
        foreach (string line in lines)
        {
          int separator = line.IndexOf('|');
          string fileName = separator > 0 ? line.Substring(0, separator) : string.Empty;
          string content = separator > 0 ? line.Substring(separator + 1) : line;

          if (Matches(fileName, content, filter))
          {
            _lines.Add(new TailLineEntry(fileName, content));
          }
        }
      }
      finally
      {
        locker.ExitWriteLock();
      }
    }

    private static bool Matches(string fileName, string content, string? filter)
    {
      if (filter == null) return true;

      return (fileName.Length > 0 && fileName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
             content.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal void CopyTo(TailLineCache target, string? filter)
    {
      int total = _lines.LineIndexed;
      int start = _lines.FirstLineIndex;

      while (start < total)
      {
        TailLineEntry[] entries = GetEntriesCore(start, Math.Min(ReplayChunkSize, total - start), out int actualStart);
        if (entries.Length == 0) break;

        target.AppendEntries(entries, filter);

        // The source may have trimmed lines while we were replaying: continue
        // after what was actually returned to avoid duplicates or gaps.
        start = actualStart + entries.Length;
      }
    }

    public TailLineEntry[] GetEntries(int start, int len)
    {
      return GetEntriesCore(start, len, out _);
    }

    private TailLineEntry[] GetEntriesCore(int start, int len, out int actualStart)
    {
      actualStart = start;
      var locker = _locker;
      if (locker == null) return Array.Empty<TailLineEntry>();

      locker.EnterReadLock();
      try
      {
        if (start < _lines.FirstLineIndex) start = _lines.FirstLineIndex;
        if (start >= _lines.LineIndexed) return Array.Empty<TailLineEntry>();

        actualStart = start;
        int count = Math.Min(len, _lines.LineIndexed - start);
        if (count <= 0) return Array.Empty<TailLineEntry>();

        var entries = new TailLineEntry[count];
        _lines.CopyTo(start, entries, count);
        return entries;
      }
      finally
      {
        locker.ExitReadLock();
      }
    }

    private void AppendEntries(IEnumerable<TailLineEntry> entries, string? filter)
    {
      var locker = _locker;
      if (locker == null) return;

      locker.EnterWriteLock();
      try
      {
        foreach (var entry in entries)
        {
          if (Matches(entry.FileName, entry.Content, filter))
          {
            _lines.Add(entry);
          }
        }
      }
      finally
      {
        locker.ExitWriteLock();
      }
    }

    private static string Format(TailLineEntry entry)
    {
      return entry.FileName.Length == 0
        ? entry.Content
        : string.Concat(entry.FileName, "|", entry.Content);
    }

    public void Dispose()
    {
      if (_locker == null) return;

      _locker.Dispose();
      _locker = null;
    }

    

    /// <summary>
    /// Fixed-capacity ring buffer of line entries. When full, the oldest entry
    /// is overwritten without shifting the other items.
    /// </summary>
    private sealed class LineRingBuffer
    {
      private readonly int _maxCapacity;
      private TailLineEntry[] _buffer;
      private int _head;
      private int _count;

      public LineRingBuffer(int maxCapacity)
      {
        _maxCapacity = maxCapacity;
        _buffer = new TailLineEntry[Math.Min(maxCapacity, 1024)];
      }

      public int FirstLineIndex { get; private set; }

      public int LineIndexed { get; private set; }

      public TailLineEntry this[int index]
      {
        get
        {
          int offset = index - FirstLineIndex;
          return _buffer[(_head + offset) % _buffer.Length];
        }
      }

      public void Add(TailLineEntry entry)
      {
        if (_count == _buffer.Length && _buffer.Length < _maxCapacity)
        {
          Grow();
        }

        if (_count < _buffer.Length)
        {
          _buffer[(_head + _count) % _buffer.Length] = entry;
          _count++;
        }
        else
        {
          _buffer[_head] = entry;
          _head = (_head + 1) % _buffer.Length;
          FirstLineIndex++;
        }

        LineIndexed = FirstLineIndex + _count;
      }

      public void CopyTo(int start, TailLineEntry[] destination, int count)
      {
        int offset = start - FirstLineIndex;
        int firstChunk = Math.Min(count, _buffer.Length - ((_head + offset) % _buffer.Length));

        Array.Copy(_buffer, (_head + offset) % _buffer.Length, destination, 0, firstChunk);

        if (firstChunk < count)
        {
          Array.Copy(_buffer, 0, destination, firstChunk, count - firstChunk);
        }
      }

      public void Clear()
      {
        _head = 0;
        _count = 0;
        FirstLineIndex = 0;
        LineIndexed = 0;
      }

      private void Grow()
      {
        int newSize = Math.Min(_buffer.Length * 2, _maxCapacity);
        var newBuffer = new TailLineEntry[newSize];

        for (int i = 0; i < _count; i++)
        {
          newBuffer[i] = this[FirstLineIndex + i];
        }

        _buffer = newBuffer;
        _head = 0;
      }
    }
  }
}
