using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Be.Manuc66.Library.General;
using Be.Manuc66.Library.WPF;
using Microsoft.Practices.Prism.Events;
using Tai2JumpInfra;
using Tai2JumpInfra.Events;
using Tail2Jump.Interfaces;
using Tail2Jump.Objects;

namespace Tail2Jump.MonitoringPanel
{
  /// <summary>
  /// Logique d'interaction pour UserControl1.xaml
  /// </summary>
  public partial class MonitoringPanelW8 : INotifyPropertyChanged, IMonitoringPanelW8
  {
    #region Dependency properties
    #region Properties
    #region MenuSearchIsChecked
    public bool MenuSearchIsChecked
    {
      get { return (bool)GetValue(MenuSearchIsCheckedProperty); }
      set { SetValue(MenuSearchIsCheckedProperty, value); }
    }
    public static readonly DependencyProperty MenuSearchIsCheckedProperty = DependencyProperty.Register(
      "MenuSearchIsChecked", typeof(bool), typeof(MonitoringPanelW8), new PropertyMetadata(false, MenuSearchIsCheckedChanged));

    private static void MenuSearchIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ((MonitoringPanelW8)d).MenuSearchIsCheckedChanged(e);
    }
    private void MenuSearchIsCheckedChanged( DependencyPropertyChangedEventArgs e_)
    {
     _eventAggregator.GetEvent<SearchActivationStatusChanged>().Publish((bool)e_.NewValue);
    }
    #endregion // MenuSearchIsChecked

    #region MenuDebugConsoleIsChecked
    public bool MenuDebugConsoleIsChecked
    {
      get { return (bool)GetValue(MenuDebugConsoleIsCheckedProperty); }
      set { SetValue(MenuDebugConsoleIsCheckedProperty, value); }
    }
    public static readonly DependencyProperty MenuDebugConsoleIsCheckedProperty = DependencyProperty.Register(
      "MenuDebugConsoleIsChecked", typeof(bool), typeof(MonitoringPanelW8), new PropertyMetadata(false));
    #endregion // MenuDebugConsoleIsChecked

    #region MenuStressCPUIsChecked
    public bool MenuStressCPUIsChecked
    {
      get { return (bool)GetValue(MenuStressCPUIsCheckedProperty); }
      set { SetValue(MenuStressCPUIsCheckedProperty, value); }
    }
    public static readonly DependencyProperty MenuStressCPUIsCheckedProperty = DependencyProperty.Register(
      "MenuStressCPUIsChecked", typeof(bool), typeof(MonitoringPanelW8), new PropertyMetadata(false));
    #endregion // MenuStressCPUIsChecked

    #region MonitoringList
    public ThreadSafeObservableCollection<MonitoringInstance> MonitoringList
    {
      get { return (ThreadSafeObservableCollection<MonitoringInstance>)GetValue(MonitoringListProperty); }
      set { SetValue(MonitoringListProperty, value); }

    }
    public static readonly DependencyProperty MonitoringListProperty = DependencyProperty.Register(
      "MonitoringList", typeof(ThreadSafeObservableCollection<MonitoringInstance>), typeof(MonitoringPanelW8), new PropertyMetadata(null));
    #endregion // MenuSearchIsChecked
    #endregion Properties
    #region Commands
    public ICommand<IEmpty> StartNewWatcherCommand { get; private set; }
    public ICommand<IEmpty> RemoveSelectedWatcherCommand { get; private set; }
    public bool CanStartNewWatcher
    {
      get
      {
        return m_CanStartNewWatcher;
      }
      set
      {
        if (m_CanStartNewWatcher != value)
        {
          m_CanStartNewWatcher = value;
          OnPropertyChanged("CanStartNewWatcher");
          StartNewWatcherCommand.RaiseCanExecuteChanged();
        }
      }
    }
    private bool m_CanStartNewWatcher = true;
    #endregion // Commands
    #endregion ////////// Dependency properties

    private IWatcherController WatcherController { get; set; }
    private readonly IEventAggregator _eventAggregator;
    public MonitoringPanelW8(IWatcherController watcherController_, IEventAggregator eventAggregator_)
    {
      InitializeComponent();
      WatcherController = watcherController_;
      _eventAggregator = eventAggregator_;


      var watcherActionAskEvent = _eventAggregator.GetEvent<WatcherActionAsk>();
      watcherActionAskEvent.Subscribe(PerformAction);
      DataContext = this;

      InitMonitoringPanel();
    }

    #region Properties
    public ThreadSafeObservableCollection<IMonitoringInstance> ListWatcher { get; set; }
    #endregion // Properties

    #region Methods
    public bool InitMonitoringPanel()
    {
      ListWatcher = WatcherController.ListWatcher;

      StartNewWatcherCommand = new SimpleCommand<IEmpty>(e_ => CanStartNewWatcher, e_ => CreateNewWatcher());

      RemoveSelectedWatcherCommand = new SimpleCommand<IEmpty>(e_ => m_instanceLst.SelectedItems.Count > 0, e_ => RelomovdWatcher());

      return true;
    }

    private void RelomovdWatcher()
    {
      WatcherController.RemoveWatcher(new List<IMonitoringInstance>(m_instanceLst.SelectedItems.Cast<IMonitoringInstance>()));

    }

    public IMonitoringInstance CreateNewWatcher()
    {
      IMonitoringInstance result = WatcherController.CreateWatcher();

      return result;
    }

    public List<IMonitoringInstance> GetSelectedMonitoringInstances()
    {
      return new List<IMonitoringInstance>(m_instanceLst.SelectedItems.OfType<IMonitoringInstance>());
    }

    // Create the OnPropertyChanged method to raise the event
    protected void OnPropertyChanged(string name_)
    {
      var handler = PropertyChanged;
      if (handler == null) return;
      handler(this, new PropertyChangedEventArgs(name_));
    }
    #endregion // Methods

    public event PropertyChangedEventHandler PropertyChanged;

    private void m_instanceLst_SelectionChanged(object sender_, SelectionChangedEventArgs e_)
    {
      _eventAggregator.GetEvent<MonitoringSelectionChanged>().Publish(m_instanceLst.SelectedItems.Cast<IMonitoringInstance>());
      RemoveSelectedWatcherCommand.RaiseCanExecuteChanged();
    }

    private void Image_MouseLeftButtonDown_1(object sender_, MouseButtonEventArgs e_)
    {
      MonitoringPanelColumn.Width = MonitoringPanelColumn.ActualWidth > 0 ? new GridLength(0) : new GridLength(140);
    }

    private void PerformAction(WatcherAction action_)
    {
      switch (action_)
      {
        case WatcherAction.Start:
          WatcherController.Start(GetSelectedMonitoringInstances());
          break;
        case WatcherAction.Stop:
          WatcherController.Stop(GetSelectedMonitoringInstances());
          break;
      }

    }
  }
}
