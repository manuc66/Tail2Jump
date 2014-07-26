using System;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;

namespace Be.Manuc66.Library.FileIO
{
  public class TailLineCache : ITailLineCache
  {
    private ReaderWriterLockSlim _locker;
    private List<string> _cache = new List<string>();
    public int LineIndexed { get; private set; }
    public TailLineCache()
    {
      _locker = new ReaderWriterLockSlim();
    }
    public string[] GetLines(int start, int len)
    {
      string[] result;
      _locker.EnterReadLock();
      try
      {
        if (start < LineIndexed && start >= 0)
        {
          int retLen = Math.Min(start + len, LineIndexed) - start;
          result = _cache.GetRange(start, retLen).ToArray();
        }
        else
        {
          result = new string[0];
        }
      }
      finally
      {
        _locker.ExitReadLock();
      }
      return result;
    }


    public void Clear()
    {
      _locker.EnterWriteLock();
      try
      {
        _cache = new List<string>();
        LineIndexed = _cache.Count;
      }
      finally
      {
        _locker.ExitWriteLock();
      }
    }
    public void AddData(TailEventData tailData, string filter = null)
    {
      if (tailData == null) return;
      _locker.EnterWriteLock();
      try
      {
        string[] lines = tailData.NewLines.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        if (filter == null || tailData.FileName.ToLower(CultureInfo.InvariantCulture).Contains(filter.ToLower(CultureInfo.InvariantCulture)))
        {
          for (int i = 0; i < lines.Length; i++)
          {
            lines[i] = tailData.FileName + "|" + lines[i];
          }

          _cache.AddRange(lines);
        }
        else
        {
          foreach (string line in lines)
          {
            if (line.ToLowerInvariant().Contains(filter.ToLowerInvariant()))
            {
              _cache.Add(tailData.FileName + "|" + line);
            }
          }
        }
        LineIndexed = _cache.Count;
      }
      finally
      {
        _locker.ExitWriteLock();
      }
    }
    public void AddData(ITailLineCache other, string filter)
    {
      if (other == null)
      {
        return;
      }
      string[] lines = other.GetLines(0, other.LineIndexed);
      foreach (var line in lines)
      {
        int fileNameSepIndex = line.IndexOf('|');
        if (fileNameSepIndex > 0)
        {
          string fileName = line.Substring(0, fileNameSepIndex);
          string content = line.Substring(fileNameSepIndex + 1, line.Length - 1 - fileNameSepIndex);
          AddData(new TailEventData(fileName, content, 0, 0), filter);
        }
        else
        {
          AddData(new TailEventData(string.Empty, line, 0, 0), filter);
        }
      }
    }

    public void Dispose()
    {
      if (_locker != null)
      {
        _locker.Dispose();
        _locker = null;
      }
    }
  }
}
