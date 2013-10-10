using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Be.Manuc66.Library;
using Be.Manuc66.Library.DevTools;
using Be.Manuc66.Library.General;
using log4net;
using log4net.Config;
using Microsoft.Practices.Prism.Events;
using Tai2JumpInfra;
using Tai2JumpInfra.Events;
using Tail2Jump.Interfaces;

namespace Tail2Jump
{

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : INotifyPropertyChanged, IMainWindow
  {
    #region Variables

    private bool _canStart;
    private bool _canStop;
    private bool _canBlink;
    private bool _flashWindow;
    private bool _canClear;
    private bool _canChangeRecursive;
    private bool _includeSubdirectories;
    private string _parentDirectory;
    private bool _canAutoScrollToEnd;
    private bool _menuSearchIsChecked;
    private string _filter;
    private const string BaseTitle = "Tail2Jump";
    private ThreadSafeObservableCollection<string> _locationList;
    private ThreadSafeObservableCollection<string> _filterList;
    private static readonly ILog Log = LogManager.GetLogger(typeof(MainWindow));
    private bool _flashingStarted;
    private IOpenWithInfo _openIn;
    ThreadSafeObservableCollection<IOpenWithInfo> _vsList;
    private readonly VSTools _mVsTools;
    #endregion

    #region Properties
    public ThreadSafeObservableCollection<IOpenWithInfo> VsList
    {
      get { return _vsList; }
      set
      {
        if (_vsList != value)
        {
          _vsList = value;
          OnPropertyChanged("VSList");
        }
      }
    }
    public ThreadSafeObservableCollection<string> FilterList
    {
      get { return _filterList; }
      set
      {
        if (_filterList != value)
        {
          _filterList = value;
          OnPropertyChanged("FilterList");
        }
      }
    }
    public ThreadSafeObservableCollection<string> LocationList
    {
      get { return _locationList; }
      set
      {
        if (_locationList != value)
        {
          _locationList = value;
          OnPropertyChanged("OpenInVS");
        }
      }
    }

    public bool MenuSearchIsChecked
    {
      get { return _menuSearchIsChecked; }
      set
      {
        if (_menuSearchIsChecked != value)
        {
          _menuSearchIsChecked = value;
          OnPropertyChanged("MenuSearchIsChecked");
        }
      }
    }
    public ICommand<IEmpty> StartCommand { get; private set; }
    public ICommand<IEmpty> StopCommand { get; private set; }
    public ICommand<IEmpty> ClearCommand { get; private set; }
    public ICommand<DragEventArgs> DragOverCommand { get; private set; }
    public ICommand<DragEventArgs> DropCommand { get; private set; }
    public bool FlashingStarted
    {
      get
      {
        return _flashingStarted;
      }
      set
      {
        if (_flashingStarted != value)
        { _flashingStarted = value; OnPropertyChanged("FlashingStarted"); }
      }
    }
    public bool CanStart
    {
      get
      {
        return _canStart;
      }
      set
      {
        if (_canStart != value)
        { _canStart = value; OnPropertyChanged("CanStart"); StartCommand.RaiseCanExecuteChanged(); }
      }
    }
    public bool CanBlink
    {
      get
      {
        return _canBlink;
      }
      set
      {
        if (_canBlink != value)
        { _canBlink = value; OnPropertyChanged("CanBlink"); }
      }
    }
    public bool CanStop
    {
      get
      {
        return _canStop;
      }
      set
      {
        if (_canStop != value)
        { _canStop = value; OnPropertyChanged("CanStop"); StopCommand.RaiseCanExecuteChanged(); }
      }
    }
    public bool CanClear
    {
      get
      {
        return _canClear;
      }
      set
      {
        if (_canClear != value)
        {
          _canClear = value;
          OnPropertyChanged("CanClear");
          ClearCommand.RaiseCanExecuteChanged();
        }
      }
    }
    public bool CanChangeRecursive
    {
      get
      {
        return _canChangeRecursive;
      }
      set
      {
        if (_canChangeRecursive != value)
        {
          _canChangeRecursive = value;
          OnPropertyChanged("CanChangeRecursive");
        }
      }
    }
    public bool CanAutoScrollToEnd
    {
      get
      {
        return _canAutoScrollToEnd;
      }
      set
      {
        if (_canAutoScrollToEnd != value)
        {
          _canAutoScrollToEnd = value;
          OnPropertyChanged("CanAutoScrollToEnd");
        }
      }
    }
    public bool IncludeSubdirectories
    {
      get
      {
        return _includeSubdirectories;
      }
      set
      {
        if (_includeSubdirectories != value)
        {
          _includeSubdirectories = value;
          OnPropertyChanged("IncludeSubdirectories");
        }
      }
    }
    public string ParentDirectory
    {
      get
      {
        return _parentDirectory;
      }
      set
      {
        if (_parentDirectory != value)
        {
          _parentDirectory = value;
          OnPropertyChanged("ParentDirectory");
        }
      }
    }
    public string Filter
    {
      get
      {
        return _filter;
      }
      set
      {
        if (_filter != value)
        {
          _filter = value;
          OnPropertyChanged("Filter");
        }
      }
    }
    public bool FlashWindow
    {
      get
      {
        return _flashWindow;
      }
      set
      {
        if (_flashWindow != value)
        {
          _flashWindow = value;
          OnPropertyChanged("FlashWindow");
        }
      }
    }
    public bool AutoScrollToEnd
    {
      get
      {
        return _textView.AutoScroll;
      }
      set
      {
        if (_textView.AutoScroll != value)
        {
          _textView.AutoScroll = value;
          OnPropertyChanged("AutoScrollToEnd");
        }
      }
    }

    private IWatcherController WatcherController { get; set; }
    private ILogController LogController { get; set; }
    private readonly IPartialView _textView;
    private readonly IEventAggregator _eventAggregator;
    #endregion

    public event PropertyChangedEventHandler PropertyChanged;

    #region Constructor
    public MainWindow(IWatcherController watcherController, ILogController logController,  IPartialView partialView, IEventAggregator eventAggregator, VSTools vsTools)
    {
      InitializeComponent();
      WatcherController = watcherController;
      _textView = partialView;
      LogController = logController;
      _eventAggregator = eventAggregator;
      _mVsTools = vsTools;

      _eventAggregator.GetEvent<MonitoringSelectionChanged>().Subscribe(m_monitoringPanel_eSelectionChanged);
      _eventAggregator.GetEvent<SearchActivationStatusChanged>().Subscribe(event_ => MenuSearchIsChecked = event_);

      StartCommand = new SimpleCommand<IEmpty>(e => CanStart, e =>
      {
        CanStart = false;
        if (Start())
        {
          CanStop = true;
          CanClear = true;
          CanChangeRecursive = false;
        }
        else
        {
          CanStart = true;
        }
      });
      StopCommand = new SimpleCommand<IEmpty>(e => CanStop, e =>
      {
        CanStop = false;
        if (Stop())
        {
          CanStart = true;
          CanClear = true;
          CanChangeRecursive = true;
        }
        else
        {
          CanStop = true;
        }
      });
      ClearCommand = new SimpleCommand<IEmpty>(e => CanClear, e => Clear());
      DragOverCommand = new SimpleCommand<DragEventArgs>(e => true, e =>
        {
          e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.All : DragDropEffects.None;
          e.Handled = false;
        });
      DropCommand = new SimpleCommand<DragEventArgs>(e => true, e =>
      {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var paths = (string[])e.Data.GetData(DataFormats.FileDrop);

        foreach (var path in paths.Where(path => path != null))
        {
          WatcherController.CreateWatcher(path);
        }
      });
      CanClear = true;

      // On récupère le chemin du fichier de config
      var configFile = Directory.GetCurrentDirectory() + @"\log4net.config";
      LocationList = new ThreadSafeObservableCollection<string>();
      FilterList = new ThreadSafeObservableCollection<string>();

      // On remplace le BasicConfigurator par le XmlConfigurator
      // et on charge la configuration définie dans le fichier log4net.config
      XmlConfigurator.Configure(new FileInfo(configFile));

      // Register the IOleMessageFilter to handle any threading errors.
      MessageFilter.Register();

      CanStart = false;
      CanStop = false;
      CanChangeRecursive = true;
      DataContext = this;
      //ParentDirectory = @"C:\Logs\Demo";
      //Filter = "*.log";
      AutoScrollToEnd = true;
      CanAutoScrollToEnd = true;
      CanBlink = true;
      Closing += MainWindowClosing;
      Activated += MainWindowActivated;
      Deactivated += MainWindowDeactivated;
      Title = BaseTitle;
      VsList = new ThreadSafeObservableCollection<IOpenWithInfo>();

      LogController.AddLog(LogType.Valid, " >> Congratulation Dude, T2J has loaded without any fatal Errors d-_-b ");
    }

    void m_monitoringPanel_eSelectionChanged(IEnumerable<IMonitoringInstance> e)
    {
      CanStop = false;
      CanStart = false;
      foreach (IMonitoringInstance item in e)
      {
        switch (item.Status)
        {
          case MonitoringInstanceStatus.Started:
            CanStop = true;
            break;
          case MonitoringInstanceStatus.None:
          case MonitoringInstanceStatus.Paused:
          case MonitoringInstanceStatus.Stopped:
            CanStart = true;
            break;
        }
      }
      StartCommand.RaiseCanExecuteChanged();
      StopCommand.RaiseCanExecuteChanged();
    }


    #endregion
    #region Event Handlers
    private void MainWindowDeactivated(object sender, EventArgs e)
    {
      this.StopFlashingWindow();
      _flashingStarted = false;
    }
    private void MainWindowActivated(object sender, EventArgs e)
    {
      this.StopFlashingWindow();
      _flashingStarted = false;
      ThreadPool.QueueUserWorkItem(x => RefreshVsList());
    }

    private void MainWindowClosing(object sender, CancelEventArgs e)
    {
      // Configure the message box to be displayed
      const string messageBoxText = "Do you really want to exit?";
      var caption = "Exit \"" + Title + "\"?";
      const MessageBoxButton button = MessageBoxButton.YesNo;
      const MessageBoxImage icon = MessageBoxImage.Warning;

      // Display message box
      MessageBoxResult result = MessageBox.Show(this, messageBoxText, caption, button, icon);

      // Process message box results
      switch (result)
      {
        case MessageBoxResult.Yes:
          if (Stop())
          {
            // Sauvegarde de tout ce qui se trouve dans CFG.
            //CFG.Load();
            //Config oldConf = CFG.Conf;

            //CFG.Conf = new Config();
            //CFG.Conf.Search = new ListCriterianSearch();

            //Utils.MergeList(oldConf.DirectoryHistory, LocationList, CFG.Conf.DirectoryHistory);
            //Utils.MergeList(oldConf.FilterHistory, FilterList, CFG.Conf.FilterHistory);

            //CriterianSearch lastSearch = new CriterianSearch();
            //lastSearch.FileExtension = Filter;
            //lastSearch.PathDirectory = ParentDirectory;
            //lastSearch.Recursive = IncludeSubdirectories;
            //CFG.Conf.Search.Add(lastSearch);
            //CFG.Save();
          }
          else
          {
            e.Cancel = true;
          }
          // and turn off the IOleMessageFilter.
          MessageFilter.Revoke();
          break;
        case MessageBoxResult.No:
          e.Cancel = true;
          break;
      }
    }

    #endregion
    #region Methods
    // Create the OnPropertyChanged method to raise the event
    protected void OnPropertyChanged(string name)
    {
      var handler = PropertyChanged;
      if (handler == null) return;
      handler(this, new PropertyChangedEventArgs(name));
    }

    private void RefreshVsList()
    {
      _mVsTools.RefreshVSList(VsList, ref _openIn);
      Dispatcher.Invoke(DispatcherPriority.DataBind, new Action(() =>
      {
        comboSelecOpenIn.SelectedItem = _openIn;
      }));
    }

    private bool Start()
    {
      _eventAggregator.GetEvent<WatcherActionAsk>().Publish(WatcherAction.Start);
      return true;
    }
    private bool Stop()
    {
      _eventAggregator.GetEvent<WatcherActionAsk>().Publish(WatcherAction.Stop);
      return true;
    }

    private void Clear()
    {
      try
      {
        CanClear = false;
        WatcherController.Clear();
      }
      catch (Exception error)
      {
        Log.Error(error);
      }
      finally
      {
        CanClear = true;
      }
    }
    #endregion

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      _eventAggregator.GetEvent<OpenInChanged>().Publish(comboSelecOpenIn.SelectedItem as IOpenWithInfo);
    }
  }
}
