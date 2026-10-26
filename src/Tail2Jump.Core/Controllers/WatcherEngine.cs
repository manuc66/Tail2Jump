using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Be.Manuc66.Library.Collections;
using Be.Manuc66.Library.FileIO;
using Microsoft.Extensions.Logging;
using Tai2JumpInfra;
using Tai2JumpInfra.MonitoringInstanceStates;
using Tail2Jump.Objects;

namespace Tail2Jump.Controllers
{
  /// <summary>
  /// Headless watcher engine: owns the tailers and the line caches.
  /// The UI layer is responsible for dialogs and for dispatching
  /// <see cref="DataUpdated"/> to the UI thread.
  /// </summary>
  public class WatcherEngine : IDisposable
  {
    private readonly object _locker = new object();
    private readonly Dictionary<int, Tailer> _tailers = new Dictionary<int, Tailer>();
    private readonly ITailLineCache _fileAccessor;
    private readonly ITailLineCache _fileAccessorFiltered;
    private readonly ILogger _log;
    private int _tailerCounter;
    private string _filterValue;
    private bool _isFilterActivated;
    private bool _disposed;

    public WatcherEngine(ITailLineCache fileAccessor, ITailLineCache filteredFileAccessor, ILogger<WatcherEngine> log = null)
    {
      _fileAccessor = fileAccessor ?? throw new ArgumentNullException(nameof(fileAccessor));
      _fileAccessorFiltered = filteredFileAccessor ?? throw new ArgumentNullException(nameof(filteredFileAccessor));
      _log = (ILogger)log ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
      ListWatcher = new ThreadSafeObservableCollection<IMonitoringInstance>();
    }

    public ThreadSafeObservableCollection<IMonitoringInstance> ListWatcher { get; }

    public ITailLineCache FileAccessor => _isFilterActivated ? _fileAccessorFiltered : _fileAccessor;

    /// <summary>Raised (on a background thread) when new lines have been added.</summary>
    public event Action DataUpdated;

    /// <summary>Raised (on a background thread) when the filter changes.</summary>
    public event Action FilterChanged;

    public IMonitoringInstance CreateWatcher(string path, string filter = "*", bool includeSubdirectories = false)
    {
      if (string.IsNullOrWhiteSpace(path)) return null;

      MonitoringInstance instance;

      try
      {
        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath))
        {
          instance = new MonitoringInstance
          {
            FullDirectoryPath = Path.GetDirectoryName(fullPath),
            Filter = Path.GetFileName(fullPath)
          };
        }
        else if (Directory.Exists(fullPath))
        {
          instance = new MonitoringInstance
          {
            FullDirectoryPath = fullPath,
            Filter = string.IsNullOrWhiteSpace(filter) ? "*" : filter
          };
        }
        else
        {
          _log.LogError("Path does not exist: {Path}", path);
          return null;
        }
      }
      catch (Exception error) when (error is ArgumentException || error is NotSupportedException ||
                                    error is PathTooLongException || error is UnauthorizedAccessException)
      {
        _log.LogError(error, "Invalid path: {Path}", path);
        return null;
      }

      instance.IncludeSubdirectories = includeSubdirectories;
      instance.SetDisplayedName();
      instance.IsOnScreen = true;
      instance.State = new StoppedMonitoringInstance();

      var tailer = new Tailer();
      tailer.eMoreData += OnMoreData;

      lock (_locker)
      {
        int id = _tailerCounter++;
        _tailers.Add(id, tailer);
        instance.TailerId = id;
        ListWatcher.Add(instance);
      }

      Start(instance);
      return instance;
    }

    public bool Start(IEnumerable<IMonitoringInstance> toStart)
    {
      var instances = toStart?.ToList() ?? new List<IMonitoringInstance>();
      return instances.Count > 0 && instances.Aggregate(true, (current, instance) => current & Start(instance));
    }

    public bool Start(IMonitoringInstance toStart)
    {
      if (toStart == null) return false;

      Tailer tailer;
      lock (_locker)
      {
        _tailers.TryGetValue(toStart.TailerId, out tailer);
      }

      if (tailer == null)
      {
        _log.LogError("Tailer {TailerId} not found", toStart.TailerId);
        return false;
      }

      tailer.IncludeSubdirectories = toStart.IncludeSubdirectories;
      tailer.ParentDirectory = toStart.FullDirectoryPath;
      tailer.Filter = toStart.Filter;

      if (!tailer.Start())
      {
        _log.LogError("Tailer {TailerId} failed to start", toStart.TailerId);
        return false;
      }

      toStart.State = new StartedMonitoringInstance();
      return true;
    }

    public bool Stop(IEnumerable<IMonitoringInstance> toStop)
    {
      var instances = toStop?.ToList() ?? new List<IMonitoringInstance>();
      return instances.Count == 0 || instances.Aggregate(true, (current, instance) => current & Stop(instance));
    }

    public bool Stop(IMonitoringInstance toStop)
    {
      if (toStop == null) return false;

      Tailer tailer;
      lock (_locker)
      {
        _tailers.TryGetValue(toStop.TailerId, out tailer);
      }

      if (tailer == null)
      {
        _log.LogError("Tailer {TailerId} not found", toStop.TailerId);
        return false;
      }

      if (!tailer.Stop()) return false;

      toStop.State = new StoppedMonitoringInstance();
      return true;
    }

    public void RemoveWatcher(ICollection<IMonitoringInstance> toRemove)
    {
      foreach (var item in toRemove.ToList())
      {
        RemoveWatcher(item);
      }
    }

    public void RemoveWatcher(IMonitoringInstance toRemove)
    {
      if (toRemove == null) return;

      lock (_locker)
      {
        if (!_tailers.TryGetValue(toRemove.TailerId, out Tailer tailer)) return;

        tailer.eMoreData -= OnMoreData;
        tailer.Stop();
        tailer.Dispose();
        _tailers.Remove(toRemove.TailerId);
        ListWatcher.Remove(toRemove);
      }
    }

    public void Clear()
    {
      _fileAccessor.Clear();
      _fileAccessorFiltered.Clear();
      DataUpdated?.Invoke();
    }

    public void ApplyFilter(string filter)
    {
      if (string.IsNullOrEmpty(filter))
      {
        ResetFilter();
        return;
      }

      _filterValue = filter;
      _fileAccessorFiltered.Clear();
      _fileAccessorFiltered.AddData(_fileAccessor, filter);
      _isFilterActivated = true;

      FilterChanged?.Invoke();
      DataUpdated?.Invoke();
    }

    public void ResetFilter()
    {
      _isFilterActivated = false;
      _filterValue = string.Empty;

      FilterChanged?.Invoke();
      DataUpdated?.Invoke();
    }

    private void OnMoreData(Tailer sender, TailEventData newData)
    {
      _fileAccessor.AddData(newData);

      if (string.IsNullOrEmpty(_filterValue))
      {
        _fileAccessorFiltered.Clear();
      }
      else
      {
        _fileAccessorFiltered.AddData(newData, _filterValue);
      }

      DataUpdated?.Invoke();
    }

    public void Dispose()
    {
      if (_disposed) return;
      _disposed = true;

      lock (_locker)
      {
        foreach (var tailer in _tailers.Values)
        {
          tailer.eMoreData -= OnMoreData;
          tailer.Stop();
          tailer.Dispose();
        }
        _tailers.Clear();
      }

      _fileAccessor.Dispose();
      _fileAccessorFiltered.Dispose();
    }
  }
}
