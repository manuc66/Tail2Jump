using System;
using System.Collections.Generic;
using System.Threading;

namespace Be.Manuc66.Library.FileIO
{
  /// <summary>
  /// Thread-safe cache of log lines. Lines are stored as (file name, content)
  /// pairs: the "file|content" string is only built for the lines actually
  /// requested by the UI, which avoids duplicating the file prefix for every
  /// cached line.
  /// </summary>
  public class TailLineCache : ITailLineCache
  {
    public const int DefaultMaxLines = 200_000;

    private const int ReplayChunkSize = 4096;

    private readonly int _maxLines;
    private ReaderWriterLockSlim? _locker;
    private List<LineEntry> _cache = new List<LineEntry>();

    public TailLineCache(int maxLines = DefaultMaxLines)
    {
      if (maxLines <= 0) throw new ArgumentOutOfRangeException(nameof(maxLines));

      _maxLines = maxLines;
      _locker = new ReaderWriterLockSlim();
    }

    public int LineIndexed { get; private set; }

    public int FirstLineIndex { get; private set; }

    public string[] GetLines(int start, int len)
    {
      if (len < 0 || _locker == null) return Array.Empty<string>();

      var locker = _locker;
      locker.EnterReadLock();
      try
      {
        if (start < 0 || start >= LineIndexed)
        {
          return Array.Empty<string>();
        }

        if (start < FirstLineIndex) start = FirstLineIndex;

        int count = Math.Min(len, LineIndexed - start);
        if (count <= 0) return Array.Empty<string>();

        var result = new string[count];
        int offset = start - FirstLineIndex;

        for (int i = 0; i < count; i++)
        {
          result[i] = Format(_cache[offset + i]);
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
        _cache = new List<LineEntry>();
        LineIndexed = 0;
        FirstLineIndex = 0;
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
        AppendLines(lines, fileName, filter, fileNameMatches);
        Trim();
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

    private void AppendLines(IReadOnlyList<string> lines, string fileName, string? filter, bool fileNameMatches)
    {
      if (fileNameMatches)
      {
        for (int i = 0; i < lines.Count; i++)
        {
          _cache.Add(new LineEntry(fileName, lines[i]));
        }

        return;
      }

      foreach (string line in lines)
      {
        if (line.IndexOf(filter!, StringComparison.OrdinalIgnoreCase) >= 0)
        {
          _cache.Add(new LineEntry(fileName, line));
        }
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
            _cache.Add(new LineEntry(fileName, content));
          }
        }

        Trim();
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
      int total = LineIndexed;

      for (int start = FirstLineIndex; start < total; start += ReplayChunkSize)
      {
        LineEntry[] entries = GetEntries(start, Math.Min(ReplayChunkSize, total - start));
        if (entries.Length == 0) break;

        target.AppendEntries(entries, filter);
      }
    }

    private LineEntry[] GetEntries(int start, int len)
    {
      var locker = _locker;
      if (locker == null) return Array.Empty<LineEntry>();

      locker.EnterReadLock();
      try
      {
        if (start < FirstLineIndex) start = FirstLineIndex;
        if (start >= LineIndexed) return Array.Empty<LineEntry>();

        int count = Math.Min(len, LineIndexed - start);
        if (count <= 0) return Array.Empty<LineEntry>();

        var entries = new LineEntry[count];
        _cache.CopyTo(start - FirstLineIndex, entries, 0, count);
        return entries;
      }
      finally
      {
        locker.ExitReadLock();
      }
    }

    private void AppendEntries(IEnumerable<LineEntry> entries, string? filter)
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
            _cache.Add(entry);
          }
        }

        Trim();
      }
      finally
      {
        locker.ExitWriteLock();
      }
    }

    private void Trim()
    {
      int excess = _cache.Count - _maxLines;
      if (excess > 0)
      {
        _cache.RemoveRange(0, excess);
        FirstLineIndex += excess;
      }

      LineIndexed = FirstLineIndex + _cache.Count;
    }

    private static string Format(LineEntry entry)
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

    private readonly struct LineEntry
    {
      public LineEntry(string fileName, string content)
      {
        FileName = fileName;
        Content = content;
      }

      public string FileName { get; }
      public string Content { get; }
    }
  }
}
