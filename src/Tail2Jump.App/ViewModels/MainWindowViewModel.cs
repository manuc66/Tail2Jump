using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using Be.Manuc66.Library.Collections;
using Be.Manuc66.Library.DevTools;
using Be.Manuc66.Library.FileIO;
using Tail2Jump.Config.Objects;
using Tail2Jump.Controllers;
using Tail2Jump.Enums;
using Tail2Jump.Objects;
using Tail2Jump.Logging;
using Tai2JumpInfra;
using Tai2JumpInfra.MonitoringInstanceStates;
using Be.Manuc66.Library.Mvvm;

namespace Tail2Jump.ViewModels
{
  public class MainWindowViewModel : ObservableObject, IDisposable
  {
    private const string BaseTitle = "Tail2Jump";

    private readonly WatcherEngine _engine;
    private readonly ILogController _log;
    private readonly EditorTools _editorTools = new EditorTools();
    private readonly SynchronizationContext? _uiContext;
    private readonly ObservableCollection<LogLineViewModel> _lines = new ObservableCollection<LogLineViewModel>();

    private int _renderedLineCount;
    private ITailLineCache? _renderedCache;
    private IOpenWithInfo? _openIn;
    private bool _autoScroll = true;
    private bool _flashWindow;
    private bool _showSearch;
    private bool _showConsole;
    private bool _showPerformance;
    private bool _isPanelCollapsed;
    private string _searchText = string.Empty;
    private bool _searchActivated;
    private bool _filterActivated;
    private bool _ignoreCase = true;
    private readonly Be.Manuc66.Library.Threading.TemporisedEvent _searchDebounce =
      new Be.Manuc66.Library.Threading.TemporisedEvent(200);
    private string _title = BaseTitle;

    public MainWindowViewModel(WatcherEngine engine, ILogController log)
    {
      _engine = engine ?? throw new ArgumentNullException(nameof(engine));
      _log = log ?? throw new ArgumentNullException(nameof(log));
      _uiContext = SynchronizationContext.Current;

      AddWatcherCommand = new RelayCommand(() => _ = AddWatcherAsync());
      AddAdvancedWatcherCommand = new RelayCommand(() => _ = AddAdvancedWatchersAsync());
      RemoveWatcherCommand = new RelayCommand(RemoveSelectedWatchers, () => SelectedWatchers.Count > 0);
      StartCommand = new RelayCommand(StartWatched, () => CanStart);
      StopCommand = new RelayCommand(StopWatched, () => CanStop);
      ClearCommand = new RelayCommand(Clear, () => CanClear);
      TogglePanelCommand = new RelayCommand(() => IsPanelCollapsed = !IsPanelCollapsed);

      Performance = new PerformanceViewModel(new Tail2Jump.Monitoring.SystemMetrics(), _uiContext);

      _engine.DataUpdated += OnDataUpdated;
      Watchers.CollectionChanged += OnWatchersChanged;
      _searchDebounce.eHandle += (_, _) => PostToUi(ApplyHighlighting);

      CFG.Load();
      RefreshEditors();
    }

    public Func<System.Threading.Tasks.Task<NewWatcherInput?>>? NewWatcherDialog { get; set; }

    public Func<System.Threading.Tasks.Task<IReadOnlyList<NewWatcherInput>?>>? AdvancedWatcherDialog { get; set; }

    public ThreadSafeObservableCollection<IMonitoringInstance> Watchers => _engine.ListWatcher;

    public ObservableCollection<LogLineViewModel> Lines => _lines;

    public ObservableCollection<IMonitoringInstance> SelectedWatchers { get; } =
      new ObservableCollection<IMonitoringInstance>();

    public ObservableCollection<IOpenWithInfo> Editors { get; } = new ObservableCollection<IOpenWithInfo>();

    public ThreadSafeObservableCollection<LogInstance> Logs => _log.ListLogs;

    public RelayCommand AddWatcherCommand { get; }
    public RelayCommand AddAdvancedWatcherCommand { get; }
    public RelayCommand RemoveWatcherCommand { get; }
    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand TogglePanelCommand { get; }

    /// <summary>Raised after <see cref="Lines"/> changed, so the view can scroll to the end.</summary>
    public event Action? LinesUpdated;

    /// <summary>Raised when new lines arrive and window blinking is enabled.</summary>
    public event Action? AttentionRequested;

    public string Title
    {
      get { return _title; }
      set { SetProperty(ref _title, value); }
    }

    public bool AutoScroll
    {
      get { return _autoScroll; }
      set { SetProperty(ref _autoScroll, value); }
    }

    public bool FlashWindow
    {
      get { return _flashWindow; }
      set { SetProperty(ref _flashWindow, value); }
    }

    public bool CanAutoScrollToEnd => true;

    public bool CanBlink => true;

    public bool ShowSearch
    {
      get { return _showSearch; }
      set
      {
        if (!SetProperty(ref _showSearch, value)) return;

        SearchActivated = value;
        if (!value) FilterActivated = false;
      }
    }

    public bool ShowConsole
    {
      get { return _showConsole; }
      set { SetProperty(ref _showConsole, value); }
    }

    public bool ShowPerformance
    {
      get { return _showPerformance; }
      set
      {
        if (!SetProperty(ref _showPerformance, value)) return;
        Performance.IsRunning = value;
      }
    }

    public PerformanceViewModel Performance { get; }

    public bool IsPanelCollapsed
    {
      get { return _isPanelCollapsed; }
      set
      {
        if (!SetProperty(ref _isPanelCollapsed, value)) return;
        OnPropertyChanged(nameof(PanelToggleGlyph));
      }
    }

    public string PanelToggleGlyph => IsPanelCollapsed ? "›" : "‹";

    public string SearchText
    {
      get { return _searchText; }
      set
      {
        if (!SetProperty(ref _searchText, value)) return;
        if (FilterActivated) ApplyFilter(value);
        ScheduleHighlight();
      }
    }

    /// <summary>Highlights the search text in the displayed lines.</summary>
    public bool SearchActivated
    {
      get { return _searchActivated; }
      set
      {
        if (!SetProperty(ref _searchActivated, value)) return;
        ScheduleHighlight();
      }
    }

    /// <summary>Restricts the displayed lines to the ones matching the search text.</summary>
    public bool FilterActivated
    {
      get { return _filterActivated; }
      set
      {
        if (!SetProperty(ref _filterActivated, value)) return;
        if (value) ApplyFilter(SearchText);
        else ResetFilter();
      }
    }

    public bool IgnoreCase
    {
      get { return _ignoreCase; }
      set
      {
        if (!SetProperty(ref _ignoreCase, value)) return;
        ScheduleHighlight();
      }
    }

    public IOpenWithInfo? OpenIn
    {
      get { return _openIn; }
      set { SetProperty(ref _openIn, value); }
    }

    public bool CanStart => SelectedWatchers.Count == 0
      ? Watchers.Any(w => w.State.CanStart)
      : SelectedWatchers.Any(w => w.State.CanStart);

    public bool CanStop => SelectedWatchers.Count == 0
      ? Watchers.Any(w => w.State.CanStop)
      : SelectedWatchers.Any(w => w.State.CanStop);

    public bool CanClear => true;

    public int WatcherCount => Watchers.Count;

    public void SetSelection(IEnumerable<IMonitoringInstance> selection)
    {
      SelectedWatchers.Clear();
      foreach (var item in selection ?? Enumerable.Empty<IMonitoringInstance>())
      {
        SelectedWatchers.Add(item);
      }

      RemoveWatcherCommand.RaiseCanExecuteChanged();
      RaiseStateChanged();
    }

    public async System.Threading.Tasks.Task AddWatcherAsync()
    {
      if (NewWatcherDialog == null) return;

      NewWatcherInput? input;
      try
      {
        input = await NewWatcherDialog();
      }
      catch (Exception error)
      {
        AddLog(LogType.Exception, "Failed to open the new watcher dialog", error);
        return;
      }

      if (input == null) return;

      CreateWatcher(input.Path, input.Filter, input.IncludeSubdirectories);
    }

    public async System.Threading.Tasks.Task AddAdvancedWatchersAsync()
    {
      if (AdvancedWatcherDialog == null) return;

      IReadOnlyList<NewWatcherInput>? inputs;
      try
      {
        inputs = await AdvancedWatcherDialog();
      }
      catch (Exception error)
      {
        AddLog(LogType.Exception, "Failed to open the advanced watcher dialog", error);
        return;
      }

      if (inputs == null) return;

      foreach (var input in inputs)
      {
        CreateWatcher(input.Path, input.Filter, input.IncludeSubdirectories);
      }
    }

    public IMonitoringInstance? CreateWatcher(string path, string filter, bool includeSubdirectories)
    {
      var instance = _engine.CreateWatcher(path, filter, includeSubdirectories);
      if (instance == null)
      {
        AddLog(LogType.Error, $"Unable to watch \"{path}\"");
        return null;
      }

      CFG.RememberCriterion(instance.FullDirectoryPath ?? string.Empty, instance.Filter ?? "*", instance.IncludeSubdirectories);
      CFG.Save();

      AddLog(LogType.Valid, $"Watching \"{instance.FullDirectoryPath}\" ({instance.Filter})");
      RaiseStateChanged();
      return instance;
    }

    public void HandleDrop(IEnumerable<string> paths)
    {
      foreach (var path in paths.Where(p => !string.IsNullOrWhiteSpace(p)))
      {
        CreateWatcher(path, "*", false);
      }
    }

    private void RemoveSelectedWatchers()
    {
      _engine.RemoveWatcher(SelectedWatchers.ToList());
      SetSelection(Array.Empty<IMonitoringInstance>());
    }

    private void StartWatched()
    {
      var targets = SelectedWatchers.Count > 0 ? SelectedWatchers.ToList() : Watchers.ToList();
      _engine.Start(targets);
      RaiseStateChanged();
    }

    private void StopWatched()
    {
      var targets = SelectedWatchers.Count > 0 ? SelectedWatchers.ToList() : Watchers.ToList();
      _engine.Stop(targets);
      RaiseStateChanged();
    }

    private void Clear()
    {
      _lines.Clear();
      _renderedLineCount = 0;
      _engine.Clear();
    }

    private void ScheduleHighlight()
    {
      if (string.IsNullOrEmpty(_searchText) || !_searchActivated)
      {
        ApplyHighlighting();
        return;
      }

      _searchDebounce.Set();
    }

    private void ApplyHighlighting()
    {
      var searchText = _searchActivated ? _searchText : null;

      foreach (var line in _lines)
      {
        line.ApplySearch(searchText, _ignoreCase);
      }
    }

    private void PostToUi(Action action)
    {
      if (_uiContext == null)
      {
        action();
        return;
      }

      _uiContext.Post(_ => action(), null);
    }

    private void ApplyFilter(string filter)
    {
      _engine.ApplyFilter(filter);
      RefreshLines(true);
    }

    private void ResetFilter()
    {
      _engine.ResetFilter();
      RefreshLines(true);
    }

    private void OnDataUpdated()
    {
      PostToUi(() =>
      {
        RefreshLines();

        if (FlashWindow)
        {
          AttentionRequested?.Invoke();
        }
      });
    }

    public void RefreshLines(bool reset = false)
    {
      var cache = _engine.FileAccessor;

      if (reset || !ReferenceEquals(cache, _renderedCache))
      {
        _lines.Clear();
        _renderedLineCount = 0;
        _renderedCache = cache;
      }

      int total = cache.LineIndexed;
      if (total < _renderedLineCount)
      {
        _lines.Clear();
        _renderedLineCount = 0;
      }

      const int chunkSize = 1000;
      while (_renderedLineCount < total)
      {
        var batch = cache.GetLines(_renderedLineCount, Math.Min(chunkSize, total - _renderedLineCount));
        if (batch.Length == 0) break;

        foreach (var rawLine in batch)
        {
          var line = new LogLineViewModel(rawLine);
          if (_searchActivated && !string.IsNullOrEmpty(_searchText))
          {
            line.ApplySearch(_searchText, _ignoreCase);
          }

          _lines.Add(line);
        }

        _renderedLineCount += batch.Length;
      }

      LinesUpdated?.Invoke();
    }

    public void OpenAtLine(string linkText)
    {
      var fileInfo = _editorTools.GetFilePathWithLineNumber(linkText);
      if (fileInfo == null)
      {
        AddLog(LogType.Error, $"Cannot resolve file location from \"{linkText}\"");
        return;
      }

      var editor = OpenIn;
      if (editor == null)
      {
        AddLog(LogType.Error, "No editor configured to open files");
        return;
      }

      if (!editor.OpenFileAtLine(fileInfo))
      {
        AddLog(LogType.Error, $"Failed to open \"{fileInfo.FilePath}\" in {editor.DisplayName}");
      }
    }

    public void RefreshEditors()
    {
      var current = OpenIn;
      var detected = _editorTools.DetectEditors();

      Editors.Clear();
      foreach (var editor in detected)
      {
        Editors.Add(editor);
      }

      OpenIn = current != null && Editors.Contains(current) ? current : Editors.FirstOrDefault();
    }

    public void AddLog(LogType type, string text, Exception? exception = null)
    {
      _log.AddLog(type, text, exception);
    }

    private void OnWatchersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
      OnPropertyChanged(nameof(WatcherCount));
      RaiseStateChanged();
    }

    private void RaiseStateChanged()
    {
      OnPropertyChanged(nameof(CanStart));
      OnPropertyChanged(nameof(CanStop));
      OnPropertyChanged(nameof(CanClear));
      StartCommand.RaiseCanExecuteChanged();
      StopCommand.RaiseCanExecuteChanged();
      ClearCommand.RaiseCanExecuteChanged();
    }

    public void Dispose()
    {
      _engine.DataUpdated -= OnDataUpdated;
      _searchDebounce.Dispose();
      Performance.Dispose();
      _engine.Dispose();
    }
  }
}
