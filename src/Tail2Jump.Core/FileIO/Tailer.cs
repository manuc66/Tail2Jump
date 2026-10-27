using System;
using System.Collections.Generic;
using System.IO;
using System.Buffers;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Tail2Jump.FileIO
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

    private void TargetFileDeleted(object source, FileSystemEventArgs e)
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

    private const int StackBufferThreshold = 4096;
    private const int MaxChunkBytes = 1024 * 1024;

    private readonly object _lockTargetFileChanged = new object();

    private void TargetFileChanged(object source, FileSystemEventArgs fsEvent)
    {
      lock (_lockTargetFileChanged)
      {
        if (fsEvent == null) return;

        if (!TryGetPreviousPosition(fsEvent.FullPath, out _))
        {
          StorePreviousPosition(fsEvent.FullPath, 0);
        }

        using var fs = OpenFileShareMode(fsEvent.FullPath);
        if (fs == null)
        {
          _log.LogError("{FullPath} fail to get tailed", fsEvent.FullPath);
          return;
        }

        long position = GetActualFilePos(fsEvent.FullPath, fs, out long fileLength);
        position = fs.Seek(position, SeekOrigin.Begin);
        StorePreviousPosition(fsEvent.FullPath, position);

        long remaining = fileLength - position;
        if (remaining <= 0) return;

        string fileName = Path.GetFileName(fsEvent.FullPath);

        // Read the backlog in bounded chunks: a multi-gigabyte file must not
        // trigger a multi-gigabyte allocation. Small chunks use the stack.
        while (remaining > 0)
        {
          int chunkSize = (int)Math.Min(remaining, MaxChunkBytes);
          int numBytes;
          int consumedBytes;

          if (chunkSize <= StackBufferThreshold)
          {
            consumedBytes = ProcessStackChunk(fs, fsEvent.FullPath, fileName, chunkSize, out numBytes);
          }
          else
          {
            byte[] rented = ArrayPool<byte>.Shared.Rent(chunkSize);
            try
            {
              consumedBytes = ProcessChunk(fs, fsEvent.FullPath, fileName,
                rented.AsSpan(0, chunkSize), out numBytes);
            }
            finally
            {
              ArrayPool<byte>.Shared.Return(rented);
            }
          }

          if (numBytes <= 0)
          {
            _log.LogError("Nothing read in the file: {FullPath}", fsEvent.FullPath);
            return;
          }

          if (consumedBytes <= 0)
          {
            _log.LogDebug("Target file changed but no complete line: {ChangeType} - {Path}",
              fsEvent.ChangeType, fsEvent.FullPath);
            return;
          }

          position += consumedBytes;
          remaining = fileLength - position;

          // A partial line is left for the next event.
          if (consumedBytes < numBytes) return;
        }
      }
    }

    private int ProcessStackChunk(FileStream stream, string fullPath, string fileName, int chunkSize,
      out int numBytes)
    {
      Span<byte> buffer = stackalloc byte[StackBufferThreshold];
      return ProcessChunk(stream, fullPath, fileName, buffer[..chunkSize], out numBytes);
    }

    private int ProcessChunk(FileStream stream, string fullPath, string fileName, Span<byte> buffer,
      out int numBytes)
    {
      numBytes = ReadFully(stream, buffer);
      if (numBytes <= 0) return 0;

      if (!Utf8LineSplitter.TrySplitCompleteLines(buffer[..numBytes], out var lines, out int consumedBytes))
      {
        return 0;
      }

      if (consumedBytes <= 0) return 0;

      long fileSeekPos;
      lock (_monitorPreviousSeekPosition)
      {
        fileSeekPos = _previousSeekPositions.TryGetValue(fullPath, out long previous)
          ? previous
          : 0;
        _previousSeekPositions[fullPath] = fileSeekPos + consumedBytes;
      }

      RaiseMoreData(fullPath, fileName, lines);
      return consumedBytes;
    }

    private static int ReadFully(FileStream stream, Span<byte> buffer)
    {
      int total = 0;
      while (total < buffer.Length)
      {
        int read = stream.Read(buffer[total..]);
        if (read <= 0) break;
        total += read;
      }

      return total;
    }

    private void RaiseMoreData(string fullPath, string fileName, IReadOnlyList<string> lines)
    {
      var tailEvent = new TailEventData(fileName, lines);

      var handlers = eMoreData;
      if (handlers == null) return;

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
          _log.LogError(error, "Tail event handler failed for {Path}", fullPath);
        }
      }
    }

    private static readonly TimeSpan RecentlyWrittenThreshold = TimeSpan.FromSeconds(1);
    private const int TruncationRetryCount = 5;
    private const int TruncationRetryDelayMs = 100;

    private long GetActualFilePos(string fullPath, FileStream fs, out long fileLength)
    {
      long pos;
      lock (_monitorPreviousSeekPosition)
      {
        pos = _previousSeekPositions.TryGetValue(fullPath, out long previous) ? previous : 0;
      }

      fileLength = fs.Length;
      int retry = 0;

      // Only a file that was just written to is worth waiting for: a writer
      // mid-rewrite temporarily shrinks the file, but an idle file smaller
      // than the last known offset has been truncated. Waiting on an idle
      // file would stall every other file tailed by this instance.
      while (retry < TruncationRetryCount && fileLength < pos && WasRecentlyWritten(fs))
      {
        Thread.Sleep(TruncationRetryDelayMs);
        fileLength = fs.Length;
        retry++;
      }

      if (fileLength >= pos) return pos;

      _log.LogError("File reset detected: {FullPath}", fullPath);
      lock (_monitorPreviousSeekPosition)
      {
        _previousSeekPositions[fullPath] = 0;
      }
      return 0;
    }

    private static bool WasRecentlyWritten(FileStream fs)
    {
      try
      {
        return File.GetLastWriteTimeUtc(fs.Name) >= DateTime.UtcNow - RecentlyWrittenThreshold;
      }
      catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
      {
        return false;
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
