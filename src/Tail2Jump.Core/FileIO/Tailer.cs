using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Be.Manuc66.Library.FileIO
{
  /// <summary>
  /// Watches a directory for file changes and raises new complete lines.
  /// Cross-platform port of the original WPF implementation.
  /// </summary>
  public class Tailer : IDisposable
  {
    private readonly ILogger _log;
    private readonly object _monitorPreviousSeekPosition = new object();
    private readonly Dictionary<string, long> _previousSeekPositions = new Dictionary<string, long>();
    private FileSystemWatcher? _fileSystemWatcher;
    private bool _disposed;

    public Tailer(ILogger<Tailer>? log = null)
    {
      _log = log is null ? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance : log;
    }

    public string? Filter { get; set; }

    public string? ParentDirectory { get; set; }

    public bool IncludeSubdirectories { get; set; }

    public event Action<Tailer, TailEventData>? eMoreData;

    public bool Start()
    {
      if (string.IsNullOrWhiteSpace(ParentDirectory)) return false;

      try
      {
        string? directoryName;
        if (Directory.Exists(ParentDirectory))
        {
          directoryName = Path.GetFullPath(ParentDirectory);
          if (string.IsNullOrWhiteSpace(Filter)) Filter = "*";
        }
        else
        {
          var targetFile = new FileInfo(ParentDirectory);
          directoryName = targetFile.DirectoryName;
          if (string.IsNullOrWhiteSpace(Filter)) Filter = targetFile.Name;
        }

        if (directoryName == null) return false;

        directoryName = Path.GetFullPath(directoryName);
        if (!Directory.Exists(directoryName)) return false;

        ParentDirectory = directoryName;
        Filter ??= "*";

        StopWatcher();

        lock (_monitorPreviousSeekPosition)
        {
          _previousSeekPositions.Clear();
        }

        var enumerationOptions = new EnumerationOptions
        {
          RecurseSubdirectories = IncludeSubdirectories,
          IgnoreInaccessible = true,
          MatchType = MatchType.Simple,
          MatchCasing = MatchCasing.PlatformDefault
        };

        foreach (string filePath in Directory.EnumerateFiles(directoryName, Filter, enumerationOptions))
        {
          StorePreviousPosition(filePath, GetFileActualLength(filePath));
        }

        _fileSystemWatcher = new FileSystemWatcher
        {
          Path = ParentDirectory,
          NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess | NotifyFilters.Size |
                         NotifyFilters.Attributes | NotifyFilters.FileName | NotifyFilters.DirectoryName |
                         NotifyFilters.CreationTime | NotifyFilters.Security,
          Filter = Filter,
          IncludeSubdirectories = IncludeSubdirectories
        };

        _fileSystemWatcher.Changed += TargetFileChanged;
        _fileSystemWatcher.Created += TargetFileChanged;
        _fileSystemWatcher.Deleted += TargetFileDeleted;
        _fileSystemWatcher.Error += FileSystemWatcherError;
        _fileSystemWatcher.Renamed += FileSystemWatcherRenamed;
        _fileSystemWatcher.EnableRaisingEvents = true;

        return true;
      }
      catch (Exception error)
      {
        _log.LogError(error, "Failed to watch {Directory} (filter: {Filter})", ParentDirectory, Filter);
        StopWatcher();
        return false;
      }
    }

    private long GetFileActualLength(string filePath)
    {
      long len = 0;
      using (var fs = OpenFileShareMode(filePath))
      {
        if (fs == null)
        {
          _log.LogError("fail to read file, set file length to ZERO for: {FilePath}", filePath);
        }
        else
        {
          len = fs.Length;
        }
      }
      return len;
    }

    private void StorePreviousPosition(string filePath, long pos)
    {
      lock (_monitorPreviousSeekPosition)
      {
        _previousSeekPositions[filePath] = pos;
      }
    }

    private bool TryGetPreviousPosition(string filePath, out long pos)
    {
      lock (_monitorPreviousSeekPosition)
      {
        return _previousSeekPositions.TryGetValue(filePath, out pos);
      }
    }

    private void FileSystemWatcherRenamed(object sender, RenamedEventArgs e)
    {
      lock (_monitorPreviousSeekPosition)
      {
        if (_previousSeekPositions.TryGetValue(e.OldFullPath, out long pos))
        {
          _previousSeekPositions[e.FullPath] = pos;
          _previousSeekPositions.Remove(e.OldFullPath);
        }
      }
    }

    private FileStream? OpenFileShareMode(string filePath)
    {
      try
      {
        return new FileStream(filePath, FileMode.Open, FileAccess.Read,
          FileShare.ReadWrite | FileShare.Delete);
      }
      catch (Exception error) when (error is ArgumentException || error is IOException ||
                                    error is NotSupportedException || error is UnauthorizedAccessException)
      {
        _log.LogError(error, "Failed to open {FilePath}", filePath);
        return null;
      }
    }

    public bool Stop()
    {
      StopWatcher();
      return true;
    }

    private void StopWatcher()
    {
      if (_fileSystemWatcher == null) return;

      _fileSystemWatcher.Changed -= TargetFileChanged;
      _fileSystemWatcher.Created -= TargetFileChanged;
      _fileSystemWatcher.Deleted -= TargetFileDeleted;
      _fileSystemWatcher.Error -= FileSystemWatcherError;
      _fileSystemWatcher.Renamed -= FileSystemWatcherRenamed;
      _fileSystemWatcher.EnableRaisingEvents = false;
      _fileSystemWatcher.Dispose();
      _fileSystemWatcher = null;
    }

    private void FileSystemWatcherError(object sender, ErrorEventArgs e)
    {
      _log.LogError(e.GetException(), "FileSystemWatcher error");
    }

    public void TargetFileDeleted(object source, FileSystemEventArgs e)
    {
      if (e == null) return;

      lock (_monitorPreviousSeekPosition)
      {
        if (_previousSeekPositions.ContainsKey(e.FullPath))
        {
          StorePreviousPosition(e.FullPath, 0);
        }
      }
    }

    private readonly object _lockTargetFileChanged = new object();

    public void TargetFileChanged(object source, FileSystemEventArgs fsEvent)
    {
      lock (_lockTargetFileChanged)
      {
        if (fsEvent == null) return;

        if (!TryGetPreviousPosition(fsEvent.FullPath, out _))
        {
          StorePreviousPosition(fsEvent.FullPath, 0);
        }

        byte[] bytesRead = ReadAvailableBytes(fsEvent.FullPath);
        if (bytesRead.Length <= 0) return;

        string stringRead = Encoding.UTF8.GetString(bytesRead, 0, bytesRead.Length);

        if (!LineSplitter.TrySplitCompleteLines(stringRead, out var completeLines, out int consumedChars))
        {
          _log.LogDebug("Target file changed but no complete line: {ChangeType} - {Path}",
            fsEvent.ChangeType, fsEvent.FullPath);
          return;
        }

        long fileSeekPos;
        string file = Path.GetFileName(fsEvent.FullPath);
        uint byteConsumed = (uint)Encoding.UTF8.GetByteCount(stringRead.Substring(0, consumedChars));

        lock (_monitorPreviousSeekPosition)
        {
          fileSeekPos = _previousSeekPositions.TryGetValue(fsEvent.FullPath, out long previous)
            ? previous
            : 0;
          _previousSeekPositions[fsEvent.FullPath] = fileSeekPos + byteConsumed;
        }

        string toShow = string.Join(Environment.NewLine, completeLines);

        var tailEvent = new TailEventData(file, toShow, fileSeekPos, byteConsumed);
        var handlers = eMoreData;
        if (handlers != null)
        {
          // Invoke each subscriber separately: one failing handler must neither
          // take down the watcher thread nor prevent the other handlers.
          foreach (Action<Tailer, TailEventData> handler in handlers.GetInvocationList())
          {
            try
            {
              handler(this, tailEvent);
            }
            catch (Exception error)
            {
              _log.LogError(error, "Tail event handler failed for {Path}", fsEvent.FullPath);
            }
          }
        }
      }
    }

    private byte[] ReadAvailableBytes(string fullPath)
    {
      using (var fs = OpenFileShareMode(fullPath))
      {
        if (fs == null)
        {
          _log.LogError("{FullPath} fail to get tailed", fullPath);
          return new byte[0];
        }

        long pos = GetActualFilePos(fullPath, fs, out long fileLength);

        pos = fs.Seek(pos, SeekOrigin.Begin);
        StorePreviousPosition(fullPath, pos);

        long size = fileLength - pos;
        if (size <= 0) return new byte[0];

        var bytesRead = new byte[size];
        int numBytes = fs.Read(bytesRead, 0, bytesRead.Length);

        if (numBytes <= 0)
        {
          _log.LogError("Nothing read in the file: {FullPath}, expected data read: {Expected}",
            fullPath, bytesRead.Length);
          return new byte[0];
        }

        var toReturn = new byte[numBytes];
        Array.Copy(bytesRead, toReturn, numBytes);
        return toReturn;
      }
    }

    private long GetActualFilePos(string fullPath, FileStream fs, out long fileLength)
    {
      lock (_monitorPreviousSeekPosition)
      {
        long pos = _previousSeekPositions.TryGetValue(fullPath, out long previous) ? previous : 0;
        int retry = 0;

        fileLength = fs.Length;
        while (retry < 5 && fileLength - pos < 0)
        {
          // The file is smaller than the known size: it may be rewritten,
          // wait a bit before considering it truncated.
          Thread.Sleep(100);
          fileLength = fs.Length;
          retry++;
        }

        if (fileLength - pos >= 0) return pos;

        _log.LogError("File reset detected: {FullPath}", fullPath);
        _previousSeekPositions[fullPath] = 0;
        return 0;
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (_disposed) return;

      if (disposing)
      {
        StopWatcher();
      }

      _disposed = true;
    }
  }
}
