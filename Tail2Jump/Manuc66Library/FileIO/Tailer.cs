using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using log4net;

namespace Be.Manuc66.Library.FileIO
{
  public class Tailer
  {
    #region Delegates

    public delegate void ValueChangedHandler<in T>(object sender, T oldValue, T newValue);

    #endregion

    private readonly ILog _log = LogManager.GetLogger(typeof(Tailer));
    private readonly object _monitorPreviousSeekPosition = new object();
    private FileSystemWatcher _fileSystemWatcher;
    private bool _disposed;
    private Dictionary<string, long> _previousSeekPositions;

    public string Filter { get; set; }

    public string ParentDirectory { get; set; }

    public bool IncludeSubdirectories { get; set; }

    public event Action<Tailer, TailEventData> eMoreData;


    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public bool Start()
    {
      string directoryName;
      if (Directory.Exists(ParentDirectory))
      {
        directoryName = ParentDirectory;
      }
      else
      {
        var targetFile = new FileInfo(ParentDirectory);
        directoryName = targetFile.DirectoryName;
      }
      ParentDirectory = directoryName;

      if (directoryName == null || !Directory.Exists(directoryName))
      {
        return false;
      }
      lock (_monitorPreviousSeekPosition)
      {
        _previousSeekPositions = new Dictionary<string, long>();
      }
      foreach (
        string filePath in
          Directory.EnumerateFiles(directoryName, Filter,
                                   IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
        )
      {

        var len = GetFileActualLength(filePath);

        SetPreviousPosition(filePath, len);

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

    private long GetFileActualLength(string filePath)
    {
      long len = 0;
      using (var fs = OpenFileShareMode(filePath))
      {
        if (fs == null)
        {
          _log.Error("fail to read file, set file lenght to ZERO for: " + filePath);
        }
        else
        {
          len = fs.Length;
        }
      }
      return len;
    }

    private void SetPreviousPosition(string filePath, long pos)
    {
      if (pos == 0)
      {

      }
      lock (_monitorPreviousSeekPosition)
      {
        _previousSeekPositions[filePath] = pos;
      }
    }
    private long GetPreviousPosition(string filePath)
    {
      return _previousSeekPositions[filePath];
    }

    private void FileSystemWatcherRenamed(object sender, RenamedEventArgs e)
    {
      lock (_monitorPreviousSeekPosition)
      {
        if (_previousSeekPositions.ContainsKey(e.OldFullPath))
        {
          _previousSeekPositions[e.FullPath] = _previousSeekPositions[e.OldFullPath];
          _previousSeekPositions.Remove(e.OldFullPath);
        }
      }
    }

    private FileStream OpenFileShareMode(string filePath)
    {
      FileStream result;

      try
      {
        result = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                                FileShare.ReadWrite | FileShare.Delete | FileShare.Inheritable);
      }
      catch (ArgumentException error)
      {
        _log.Error(error);
        result = null;
      }
      catch (IOException error)
      {
        _log.Error(error);
        result = null;
      }
      catch (NotSupportedException error)
      {
        _log.Error(error);
        result = null;
      }
      catch (UnauthorizedAccessException error)
      {
        _log.Error(error);
        result = null;
      }
      catch (SecurityException error)
      {
        _log.Error(error);
        result = null;
      }

      return result;
    }

    public bool Stop()
    {
      if (_fileSystemWatcher != null)
      {
        _fileSystemWatcher.Changed -= TargetFileChanged;
        _fileSystemWatcher.Created -= TargetFileChanged;
        _fileSystemWatcher.Deleted -= TargetFileDeleted;
        _fileSystemWatcher.Error -= FileSystemWatcherError;
        _fileSystemWatcher.EnableRaisingEvents = false;
        _fileSystemWatcher.Dispose();
        _fileSystemWatcher = null;
      }
      GC.Collect();
      GC.WaitForPendingFinalizers();


      return true;
    }


    private void FileSystemWatcherError(object sender, ErrorEventArgs e)
    {
      _log.Error(e.GetException());
    }

    public void TargetFileDeleted(object source, FileSystemEventArgs e)
    {
      if (e == null)
      {
        return;
      }
      lock (_monitorPreviousSeekPosition)
      {
        if (_previousSeekPositions.ContainsKey(e.FullPath))
        {
          SetPreviousPosition(e.FullPath, 0);
        }
      }
    }

    private readonly object _lockTargetFileChanged = new object();

    public void TargetFileChanged(object source, FileSystemEventArgs fsEvent)
    {
      lock (_lockTargetFileChanged)
      {
        if (fsEvent == null)
        {
          return;
        }
        lock (_monitorPreviousSeekPosition)
        {
          if (!_previousSeekPositions.ContainsKey(fsEvent.FullPath))
          {
            SetPreviousPosition(fsEvent.FullPath, 0);
          }
        }
        byte[] bytesRead  = ReadAvaibleBytes(fsEvent.FullPath);

        if (bytesRead.Length <= 0) return;

        string stringRead = Encoding.UTF8.GetString(bytesRead, 0, bytesRead.Length);
        int indexNewLine;
        uint byteConsumed = 0;
        if (stringRead.Length > 0 && stringRead != Environment.NewLine &&
            (indexNewLine = stringRead.LastIndexOf(Environment.NewLine, StringComparison.InvariantCultureIgnoreCase)) >= 0)
        {
          string toShow = stringRead.Substring(0, indexNewLine + Environment.NewLine.Length);

          long fileSeekPos;

          byteConsumed += (uint)Encoding.UTF8.GetBytes(toShow).Length;
          lock (_monitorPreviousSeekPosition)
          {
            fileSeekPos = GetPreviousPosition(fsEvent.FullPath);
            SetPreviousPosition(fsEvent.FullPath, fileSeekPos + byteConsumed);
          }

          toShow = stringRead.Substring(0, indexNewLine);

          string file = Path.GetFileName(fsEvent.FullPath);

          if (eMoreData != null)
          {
            eMoreData(this, new TailEventData(file, toShow, fileSeekPos, byteConsumed));
          }
        }
        else
        {
          _log.Debug(
            "Target file changed but no new lines..., maybe the file has been processed by this method before this execution" +
            string.Format("{0} - {1}", fsEvent.ChangeType, fsEvent.FullPath));
        }
      }
    }

    private byte[] ReadAvaibleBytes(string fullPath)
    {
      //read from current seek position to end of file
      using (FileStream fs = OpenFileShareMode(fullPath))
      {
        if (fs == null)
        {
          _log.Error(fullPath + " fail to get tailed");
          return new byte[0];
        }

        long fileLength;
        long pos = GetActualFilePos(fullPath, fs, out fileLength);

        long newPos = pos;
        pos = fs.Seek(newPos, SeekOrigin.Begin);
        if (newPos != pos)
        {
          _log.Error("File seek did not do what I want...");
        }

        SetPreviousPosition(fullPath, pos);

        long size = fileLength - pos;
        if (size <= 0)
        {
          return new byte[0];
        }

        var bytesRead = new byte[size];
        int numBytes = fs.Read(bytesRead, 0, bytesRead.Length);

        if (numBytes <= 0)
        {
          _log.Error("Nothing readed in the file: " + fullPath + " expected data read: " + bytesRead.Length);
          return new byte[0];
        }
        var toReturn = new byte[numBytes];
        Array.Copy(bytesRead, toReturn, numBytes);

        return toReturn;
      }

    }

    private long GetActualFilePos(string fullPath, FileStream fs, out long fileLength)
    {
      long pos;
      lock (_monitorPreviousSeekPosition)
      {
        pos = GetPreviousPosition(fullPath);
        int retry = 0;

        fileLength = fs.Length;
        while (retry < 5 && fileLength - pos < 0)
        {
          // si le fichier est plus petit que la taille connue on attend avant de considerer qu'il a été tronqué car il pourrais être en cour de re-ecriture!
          fileLength = fs.Length;
          Thread.Sleep(100);
          retry++;
        }

        if (fileLength - pos >= 0) return pos;

        //seems like if the file has been reseted!
        _log.Error("File reset detected: " + fullPath);
        SetPreviousPosition(fullPath, 0);
        pos = 0;
      }
      return pos;
    }

    public void Dispose()
    {
      Dispose(true);

      // Use SupressFinalize in case a subclass
      // of this type implements a finalizer.
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      // If you need thread safety, use a lock around these 
      // operations, as well as in your methods that use the resource.
      if (!_disposed)
      {
        if (disposing)
        {
          if (_fileSystemWatcher != null)
          {
            _fileSystemWatcher.Dispose();
          }
        }

        // Indicate that the instance has been disposed.

        _fileSystemWatcher = null;
        _disposed = true;
      }
    }
  }
}