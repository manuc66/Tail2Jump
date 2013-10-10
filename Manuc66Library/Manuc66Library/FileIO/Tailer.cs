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

    public delegate void ValueChangedHandler<in T>(object sender_, T oldValue_, T newValue_);

    #endregion

    private readonly StrongTypedEvent<Tailer, TailEventData> _evtMoreData =
      new StrongTypedEvent<Tailer, TailEventData>(false);

    private readonly ILog _log = LogManager.GetLogger(typeof(Tailer));
    private readonly object _monitorPreviousSeekPosition = new object();
    private FileSystemWatcher _fileSystemWatcher;
    private bool _disposed;
    private Dictionary<string, long> _previousSeekPositions;

    public string Filter { get; set; }

    public string ParentDirectory { get; set; }

    public bool IncludeSubdirectories { get; set; }

    public event StrongTypedEvent<Tailer, TailEventData>.EventEventHandler eMoreData
    {
      add { _evtMoreData.eEvent += value; }
      remove { _evtMoreData.eEvent -= value; }
    }

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

        lock (_monitorPreviousSeekPosition)
        {
          long len = 0;
          using (FileStream fs = OpenFileShareMode(filePath))
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
          SetPreviousPosition(filePath, len);
        }
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

    private void SetPreviousPosition(string filePath_, long pos_)
    {
      if (pos_ == 0)
      {

      }
      _previousSeekPositions[filePath_] = pos_;
    }
    private long GetPreviousPosition(string filePath_)
    {
      return _previousSeekPositions[filePath_];
    }

    private void FileSystemWatcherRenamed(object sender_, RenamedEventArgs e_)
    {
      lock (_monitorPreviousSeekPosition)
      {
        if (_previousSeekPositions.ContainsKey(e_.OldFullPath))
        {
          _previousSeekPositions[e_.FullPath] = _previousSeekPositions[e_.OldFullPath];
          _previousSeekPositions.Remove(e_.OldFullPath);
        }
      }
    }

    private FileStream OpenFileShareMode(string filePath_)
    {
      FileStream result;

      try
      {
        result = new FileStream(filePath_, FileMode.Open, FileAccess.Read,
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


    private void FileSystemWatcherError(object sender_, ErrorEventArgs e_)
    {
      _log.Error(e_.GetException());
    }

    public void TargetFileDeleted(object source_, FileSystemEventArgs e_)
    {
      if (e_ == null)
      {
        return;
      }
      lock (_monitorPreviousSeekPosition)
      {
        if (_previousSeekPositions.ContainsKey(e_.FullPath))
        {
          SetPreviousPosition(e_.FullPath, 0);
        }
      }
    }

    private readonly object _lockTargetFileChanged = new object();

    public void TargetFileChanged(object source_, FileSystemEventArgs fsEvent_)
    {
      lock (_lockTargetFileChanged)
      {
        if (fsEvent_ == null)
        {
          return;
        }
        lock (_monitorPreviousSeekPosition)
        {
          if (!_previousSeekPositions.ContainsKey(fsEvent_.FullPath))
          {
            SetPreviousPosition(fsEvent_.FullPath, 0);
          }
        }
        //read from current seek position to end of file
        int numBytes;
        byte[] bytesRead;
        bool fileProcessed;
        using (FileStream fs = OpenFileShareMode(fsEvent_.FullPath))
        {
          if (fs == null)
          {
            fileProcessed = false;
            bytesRead = new byte[0];
            numBytes = 0;
            _log.Error(fsEvent_.FullPath + " fail to get tailed");
          }
          else
          {
            long fileLength;
            long pos = GetActualFilePos(fsEvent_, fs, out fileLength);


            long newPos = pos;
            pos = fs.Seek(newPos, SeekOrigin.Begin);
            if (newPos != pos)
            {
              _log.Error("File seek did not do what I want...");
            }

            lock (_monitorPreviousSeekPosition)
            {
              SetPreviousPosition(fsEvent_.FullPath, pos);
            }
            long size = fileLength - pos;
            if (size <= 0)
            {
              fileProcessed = false;
              bytesRead = new byte[0];
              numBytes = 0;
            }
            else
            {
              bytesRead = new byte[size];
              numBytes = fs.Read(bytesRead, 0, bytesRead.Length);
              if (numBytes > 0)
              {
                fileProcessed = true;
              }
              else
              {
                _log.Error("Nothing readed in the file: " + fsEvent_.FullPath + " expected data read: " +
                            bytesRead.Length);
                fileProcessed = false;
              }
            }
          }
        }

        if (fileProcessed)
        {
          string stringRead = Encoding.UTF8.GetString(bytesRead, 0, numBytes);
          int indexNewLine;
          uint byteConsumed = 0;
          if (stringRead.Length > 0 && stringRead != Environment.NewLine &&
              (indexNewLine = stringRead.LastIndexOf(Environment.NewLine, StringComparison.Ordinal)) >= 0)
          {
            string toShow = stringRead.Substring(0, indexNewLine + Environment.NewLine.Length);

            long fileSeekPos;

            byteConsumed += (uint)Encoding.UTF8.GetBytes(toShow).Length;
            lock (_monitorPreviousSeekPosition)
            {
              fileSeekPos = GetPreviousPosition(fsEvent_.FullPath);
              SetPreviousPosition(fsEvent_.FullPath, fileSeekPos + byteConsumed);
            }

            toShow = stringRead.Substring(0, indexNewLine);

            string file = Path.GetFileName(fsEvent_.FullPath);

            _evtMoreData.RaiseEvent(this, new TailEventData(file, toShow, fileSeekPos, byteConsumed));
          }
          else
          {
            _log.Debug(
              "Target file changed but no new lines..., maybe the file has been processed by this method before this execution" +
              string.Format("{0} - {1}", fsEvent_.ChangeType, fsEvent_.FullPath));
          }
        }
      }
    }

    private long GetActualFilePos(FileSystemEventArgs fsEvent_, FileStream fs_, out long fileLength_)
    {
      long pos;
      lock (_monitorPreviousSeekPosition)
      {
        pos = GetPreviousPosition(fsEvent_.FullPath);
        int retry = 0;

        fileLength_ = fs_.Length;
        while (retry < 5 && fileLength_ - pos < 0)
        {
          // si le fichier est plus petit que la taille connue on attend avant de considerer qu'il a été tronqué car il pourrais être en cour de re-ecriture!
          fileLength_ = fs_.Length;
          Thread.Sleep(100);
          retry++;
        }

        if (fileLength_ - pos < 0)
        {
          //seems like if the file has been reseted!
          _log.Error("File reset detected: " + fsEvent_.FullPath);
          SetPreviousPosition(fsEvent_.FullPath, 0);
          pos = 0;
        }

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

    protected virtual void Dispose(bool disposing_)
    {
      // If you need thread safety, use a lock around these 
      // operations, as well as in your methods that use the resource.
      if (!_disposed)
      {
        if (disposing_)
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