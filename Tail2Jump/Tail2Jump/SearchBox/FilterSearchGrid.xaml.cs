using System.ComponentModel;
using System.Windows;
using Be.Manuc66.Library.General;
using Be.Manuc66.Library.WPF;
using Microsoft.Practices.Prism.PubSubEvents;
using Tai2JumpInfra.Events;
using Tail2Jump.Interfaces;

namespace Tail2Jump.SearchBox
{
  /// <summary>
  /// Logique d'interaction pour DebugConsoleGrid.xaml
  /// </summary>
  public partial class FilterSearchGrid : INotifyPropertyChanged, IFilterSearchGrid
  {
    #region Dependency properties
    #region TextSearch
    public string TextSearch
    {
      get { return (string)GetValue(TextSearchProperty); }
      set { SetValue(TextSearchProperty, value); }
    }
    public static readonly DependencyProperty TextSearchProperty = DependencyProperty.Register(
      "TextSearch", typeof(string), typeof(FilterSearchGrid), new PropertyMetadata(string.Empty, TextSearchPropertyChangedCallback));
    #endregion // TextSearch
    #region IgnoreCase
    public bool IgnoreCase
    {
      get { return (bool)GetValue(IgnoreCaseProperty); }
      set { SetValue(IgnoreCaseProperty, value); }
    }
    public static readonly DependencyProperty IgnoreCaseProperty = DependencyProperty.Register(
      "IgnoreCase", typeof(bool), typeof(FilterSearchGrid), new PropertyMetadata(true, TextSearchPropertyChangedCallback));
    #endregion // IgnoreCase
    #region ApplyFilterCommand
    public ICommand<IEmpty> ApplyFilterCommand
    {
      get { return (ICommand<IEmpty>)GetValue(ApplyFilterCommandProperty); }
      set { SetValue(ApplyFilterCommandProperty, value); }
    }
    public static readonly DependencyProperty ApplyFilterCommandProperty = DependencyProperty.Register(
      "ApplyFilterCommand", typeof(ICommand<IEmpty>), typeof(FilterSearchGrid), new PropertyMetadata());
    #endregion // ApplyFilterCommand
    #region ResetFilterCommand
    public ICommand<IEmpty> ResetFilterCommand
    {
      get { return (ICommand<IEmpty>)GetValue(ResetFilterCommandProperty); }
      set { SetValue(ResetFilterCommandProperty, value); }
    }
    public static readonly DependencyProperty ResetFilterCommandProperty = DependencyProperty.Register(
      "ResetFilterCommand", typeof(ICommand<IEmpty>), typeof(FilterSearchGrid), new PropertyMetadata());
    #endregion // ApplyFilterCommand
    #endregion // Dependency properties

    private IEventAggregator EventAggregator { get; set; }
    private IWatcherController WatcherController { get; set; }

    public FilterSearchGrid(IEventAggregator eventAggregator_, IWatcherController watcherController_)
    {
      InitializeComponent();
      EventAggregator = eventAggregator_;
      WatcherController = watcherController_;
      DataContext = this;
      ApplyFilterCommand = new SimpleCommand<IEmpty>(e_ => true, e_ => WatcherController.ApplyFilter(TextSearch));
      ResetFilterCommand = new SimpleCommand<IEmpty>(e_ => true, e_ => WatcherController.ResetFilter());
    }

    #region Properties
    #endregion // Properties

    #region Methods

    static void TextSearchPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ((FilterSearchGrid)d).TextSearchPropertyChangedCallback(e);
    }

    private void TextSearchPropertyChangedCallback(DependencyPropertyChangedEventArgs e)
    {
      EventAggregator.GetEvent<SearchInfoChanged>().Publish((string)e.NewValue);
    }

    // Create the OnPropertyChanged method to raise the event
    protected void OnPropertyChanged(string name)
    {
      PropertyChangedEventHandler handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(name));
      }
    }
    #endregion // Methods

    public event PropertyChangedEventHandler PropertyChanged;
  }
}
