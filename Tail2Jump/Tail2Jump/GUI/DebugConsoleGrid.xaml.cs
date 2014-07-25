using System.ComponentModel;
using System.Windows.Controls;
using Be.Manuc66.Library.WPF;
using Microsoft.Practices.ServiceLocation;
using Tail2Jump.Interfaces;
using Tail2Jump.Objects;

namespace Tail2Jump.GUI
{
  /// <summary>
  /// Logique d'interaction pour DebugConsoleGrid.xaml
  /// </summary>
  public partial class DebugConsoleGrid : UserControl, INotifyPropertyChanged
  {
    #region Dependency properties
    #endregion // Dependency properties

    private ILogController LogController { get; set; }

    public DebugConsoleGrid()
    {
      InitializeComponent();
      LogController = ServiceLocator.Current.GetInstance<ILogController>();

      DataContext = this;

      InitDebugConsoleGrid();
    }

    #region Properties
    public ThreadSafeObservableCollection<LogInstance> ListLogs { get; set; }
    #endregion // Properties

    #region Methods
    public bool InitDebugConsoleGrid()
    {
      const bool result = false;
      ListLogs = LogController.ListLogs;
      return result;
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
