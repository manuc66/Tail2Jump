using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Be.Manuc66.Library.FileIO;
using Be.Manuc66.Library.WPF;
using log4net;
using Microsoft.Practices.Prism.Mvvm;
using PartialView;
using Tai2JumpInfra;
using Tail2Jump.Config.Objects;
using Tail2Jump.Enums;
using Tail2Jump.GUI;
using Tail2Jump.Interfaces;
using Tail2Jump.Objects;

namespace Tail2Jump.Controller
{
  public class WatcherController : BindableBase, IWatcherController
  {
    private Dictionary<int, Tailer> _tailers;
    private int _tailerCounter;
    private ReaderWriterLockSlim _locker;
    private readonly ITailLineCache _fileAcessor;
    private readonly ITailLineCache _fileAcessorFiltered;
    private string _filterValue;
    private readonly ILog _log = LogManager.GetLogger(typeof(WatcherController));
    private ThreadSafeObservableCollection<IMonitoringInstance> _listWatcher;
    private bool _isFilterActivated;
    private readonly IPartialView _partialView;

    #region Properties
    public ThreadSafeObservableCollection<IMonitoringInstance> ListWatcher
    {
      get { return _listWatcher; }
      set { SetProperty(ref _listWatcher, value); }
    }
    public ITailLineCache FileAcessor
    {
      get
      {
        if (_isFilterActivated)
        {
          return _fileAcessorFiltered;
        }
        return _fileAcessor;
      }
    }
    #endregion // Properties

    #region Constructor
    public WatcherController(ITailLineCache mainfileAccessor, ITailLineCache filteredFileAcessor, IPartialView partialView)
    {
      InitController();
      _fileAcessor = mainfileAccessor;
      _fileAcessorFiltered = filteredFileAcessor;
      _partialView = partialView;
    }
    #endregion // COnstructor

    #region Methods
    private void InitController()
    {
      _tailers = new Dictionary<int, Tailer>();
      _tailerCounter = 0;
      _locker = new ReaderWriterLockSlim();
      ListWatcher = new ThreadSafeObservableCollection<IMonitoringInstance>();
    }
    #region CreateWatcher
    public IMonitoringInstance CreateWatcher(string path = null)
    {
      MonitoringInstance result = null;

      var newWatcherForm = new CreateEditWatcher();

      if (path == null)
      {
        path = GetPathFromClipBoard();
      }

      #region Add directory and file filter from path
      if (!string.IsNullOrWhiteSpace(path) && !ContainsInvalidPathChars(path))
      {
        try
        {
          var fullPath = Path.GetFullPath(path);
          if (Directory.Exists(fullPath))
          {
            newWatcherForm.Monitoring.FullDirectoryPath = fullPath;
          }
          else if (File.Exists(fullPath))
          {
            string directory = Path.GetDirectoryName(fullPath);
            string fileName = Path.GetFileName(fullPath);
            newWatcherForm.Monitoring.FullDirectoryPath = directory;
            newWatcherForm.Monitoring.Filter = fileName;
          }
        }
        catch (ArgumentException error) { _log.Debug(error); }
        catch (SecurityException error) { _log.Debug(error); }
        catch (NotSupportedException error) { _log.Debug(error); }
        catch (PathTooLongException error) { _log.Debug(error); }
      }
      #endregion

      if ((ModifierKeys.Control & Keyboard.Modifiers) != 0)
      {
        var newWatcherFormAdvanced = new CreateEditWatcherAdvanced();
        newWatcherFormAdvanced.ShowDialog();
      }
      else
      {
        newWatcherForm.Owner = Application.Current.MainWindow;
        newWatcherForm.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        newWatcherForm.ShowDialog();


        if (newWatcherForm.ReturnCode == FormReturnCode.Start)
        {
          result = newWatcherForm.Monitoring;

          CFG.AddCriterianSearch(result.FullDirectoryPath, result.Filter);
          CFG.Save();

          var tailer = new Tailer();
          _locker.EnterWriteLock();
          try
          {
            int id = _tailerCounter++;
            _tailers.Add(id, tailer);
            result.TailerId = id;
            result.SetDisplayedName();
            result.IsOnScreen = true;
            result.Status = MonitoringInstanceStatus.Stopped;

            ListWatcher.Add(result);
          }
          finally
          {
            _locker.ExitWriteLock();
          }
          Start(result);
        }
      }

      return result;
    }

    private static string GetPathFromClipBoard()
    {
      string path = Clipboard.GetText();

      if (!string.IsNullOrEmpty(path)) return path;

      var fileDropList = Clipboard.GetFileDropList();

      if (fileDropList.Count <= 0) return path;

      return fileDropList[0];
    }

    private bool ContainsInvalidPathChars(string path)
    {
      return path.Any(charInPath => Path.GetInvalidPathChars().Contains(charInPath));
    }

    public void RemoveWatcher(ICollection<IMonitoringInstance> toStart)
    {
      foreach (var item in toStart)
      {
        RemoveWatcher(item);
      }
    }
    public void RemoveWatcher(IMonitoringInstance toRemove)
    {
      _locker.EnterUpgradeableReadLock();
      try
      {
        Tailer tailer;
        if (_tailers.TryGetValue(toRemove.TailerId, out tailer))
        {
          _locker.EnterWriteLock();
          try
          {
            if (tailer.Stop())
            {
              _tailers.Remove(toRemove.TailerId);
              ListWatcher.Remove(toRemove);

              tailer.eMoreData -= m_Tailer_eMoreData;
              tailer.Dispose();
            }
            else
            {
              _log.Error("Fail to stop tailer, remove cancelled!");
            }
          }
          finally
          {
            _locker.ExitWriteLock();
          }
        }
      }
      finally
      {
        _locker.ExitUpgradeableReadLock();
      }
    }

    public bool Start(List<IMonitoringInstance> toStart)
    {
      bool result = toStart.Count > 0 && toStart.Aggregate(true, (current, t) => current & Start(t));
      return result;
    }

    public bool Start(IMonitoringInstance toStart)
    {
      bool result;

      Tailer tailer;

      _locker.EnterReadLock();
      try
      {
        _tailers.TryGetValue(toStart.TailerId, out tailer);
      }
      finally
      {
        _locker.ExitReadLock();
      }

      if (tailer == null)
      {
        _log.Error("Tailer " + toStart.TailerId + " not found");
        result = false;
      }
      else
      {
        tailer.IncludeSubdirectories = toStart.IncludeSubdirectories;
        tailer.ParentDirectory = toStart.FullDirectoryPath;
        tailer.Filter = toStart.Filter;

        result = tailer.Start();
        if (result)
        {
          toStart.Status = MonitoringInstanceStatus.Started;
        }
        else
        {
          _log.Error("Tailer " + toStart.TailerId + " fail to start!");
        }
        tailer.eMoreData += m_Tailer_eMoreData;
      }


      return result;
    }

    public bool Stop(List<IMonitoringInstance> toStop)
    {
      bool result = toStop.Count <= 0 || toStop.Aggregate(true, (current, t) => current & Stop(t));
      return result;
    }

    public bool Stop(IMonitoringInstance toStop)
    {
      bool result;
      Tailer tailer;

      _locker.EnterReadLock();
      try
      {
        _tailers.TryGetValue(toStop.TailerId, out tailer);
      }
      finally
      {
        _locker.ExitReadLock();
      }


      if (tailer == null)
      {
        result = false;
        _log.Error("Tailer " + toStop.TailerId + " not found");
      }
      else
      {
        result = tailer.Stop();
        if (result)
        {
          toStop.Status = MonitoringInstanceStatus.Stopped;
        }
      }
      return result;
    }

    public void SetCurrentViewer(IList<IMonitoringInstance> toShow)
    {
      throw new NotImplementedException();
    }
    #endregion // CreateWatcher
    public void Clear()
    {
      _fileAcessor.Clear();

      if (_fileAcessorFiltered != null)
      {
        _fileAcessorFiltered.Clear();
      }

      _partialView.RefreshView(true);
    }

    public void ApplyFilter(string filter)
    {
      _filterValue = filter;
      _fileAcessorFiltered.Clear();
      _fileAcessorFiltered.AddData(_fileAcessor, filter);
      _isFilterActivated = true;


      _partialView.Dispatcher.BeginInvoke(new Action(() => _partialView.RefreshView(true)));
    }

    public void ResetFilter()
    {
      _isFilterActivated = false;
      _filterValue = string.Empty;

      _partialView.Dispatcher.BeginInvoke(new Action(() => _partialView.RefreshView(true)));
    }
    private void m_Tailer_eMoreData(Tailer sender, TailEventData newData)
    {
      _fileAcessor.AddData(newData);
      if (string.IsNullOrEmpty(_filterValue))
      {
        if (_fileAcessorFiltered != null)
        {
          _fileAcessorFiltered.Clear();
        }
      }
      else
      {
        _fileAcessorFiltered.AddData(newData, _filterValue);
      }

      _partialView.Dispatcher.BeginInvoke(new Action(() =>
      {
        _partialView.RefreshView();


        var mainWindow = Application.Current.MainWindow as MainWindow;
        if (mainWindow != null && mainWindow.FlashWindow && !mainWindow.FlashingStarted && !mainWindow.IsActive)
        {
          mainWindow.FlashWindow();
          mainWindow.FlashingStarted = true;
        }
      }));

    }
    #endregion // Methods

  }
}
