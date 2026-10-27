using System;
using System.Collections.Generic;
using System.Threading;

namespace Be.Manuc66.Library.FileIO
{
  public class TailLineCache : ITailLineCache
  {
    public const int DefaultMaxLines = 200_000;

    private const int ReplayChunkSize = 4096;

    private static readonly string[] NewLineSeparators = { "\r\n", "\n", "\r" };

    private readonly int _maxLines;
    private ReaderWriterLockSlim? _locker;
    private List<string> _cache = new List<string>();

    public int LineIndexed { get; private set; }

    public int FirstLineIndex { get; private set; }

    public TailLineCache(int maxLines = DefaultMaxLines)
    {
      if (maxLines <= 0) throw new ArgumentOutOfRangeException(nameof(maxLines));

      _maxLines = maxLines;
      _locker = new ReaderWriterLockSlim();
    }

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

        int retLen = Math.Min(start + len, LineIndexed) - start;
        if (retLen <= 0) return Array.Empty<string>();

        // Copy directly into the result array instead of allocating an
        // intermediate list for every requested page.
        var result = new string[retLen];
        _cache.CopyTo(start - FirstLineIndex, result, 0, retLen);
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
        _cache = new List<string>();
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
      if (tailData?.NewLines == null || _locker == null) return;

      string[] lines = tailData.NewLines.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries);
      if (lines.Length == 0) return;

      string fileName = tailData.FileName;
      bool fileNameMatches = filter == null ||
                             fileName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

      var locker = _locker;
      locker.EnterWriteLock();
      try
      {
        if (fileNameMatches)
        {
          foreach (string line in lines)
          {
            _cache.Add(string.Concat(fileName, "|", line));
          }
        }
        else
        {
          foreach (string line in lines)
          {
            if (line.IndexOf(filter!, StringComparison.OrdinalIgnoreCase) >= 0)
            {
              _cache.Add(string.Concat(fileName, "|", line));
            }
          }
        }

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

      var locker = _locker;
      locker.EnterWriteLock();
      try
      {
        // Replay in chunks: this avoids materializing the whole source cache
        // when applying a filter on a large log.
        int total = other.LineIndexed;
        for (int start = other.FirstLineIndex; start < total; start += ReplayChunkSize)
        {
          string[] lines = other.GetLines(start, Math.Min(ReplayChunkSize, total - start));
          if (lines.Length == 0) break;

          foreach (string line in lines)
          {
            SplitLine(line, out string fileName, out string content);

            bool fileNameMatches = filter == null ||
                                   fileName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

            if (fileNameMatches || content.IndexOf(filter!, StringComparison.OrdinalIgnoreCase) >= 0)
            {
              _cache.Add(line);
            }
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

    private static void SplitLine(string line, out string fileName, out string content)
    {
      int separator = line.IndexOf('|');
      if (separator > 0)
      {
        fileName = line.Substring(0, separator);
        content = line.Substring(separator + 1);
      }
      else
      {
        fileName = string.Empty;
        content = line;
      }
    }

    public void Dispose()
    {
      if (_locker == null) return;

      _locker.Dispose();
      _locker = null;
    }
  }
}
