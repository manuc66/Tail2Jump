using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using Be.Manuc66.Library.General;
using Be.Manuc66.Library.WPF;

namespace Tail2Jump.GUI
{
  /// <summary>
  /// Logique d'interaction pour LayoutManagementGrid.xaml
  /// </summary>
  public partial class LayoutManagementGrid : UserControl, INotifyPropertyChanged
  {

    #region Members
    private string _cpuTemperature = string.Empty;

    private bool _running;
    private double _askedCpuWork;
    private const double _currentCPUWork = 100;

    #endregion // Members

    #region Properties
    public static int Tier { get { return RenderCapability.Tier >> 16; } }

    public string CpuTemperature
    {
      get { return _cpuTemperature; }
      set
      {
        if (_cpuTemperature != value)
        {
          _cpuTemperature = value;
          OnPropertyChanged("CpuTemperature");
        }
      }
    }
    public double AskedCpuWork
    {
      get { return _askedCpuWork; }
      set
      {
        if (_askedCpuWork != value)
        {
          _askedCpuWork = value;
          OnPropertyChanged("AskedCpuWork");
        }
      }
    }
    public bool Running
    {
      get { return _running; }
      set
      {
        if (_running != value)
        {
          _running = value;
          OnPropertyChanged("Running");
        }
      }
    }
    #endregion // Properties

    #region Dependency properties
    #region Commands
    public ICommand<IEmpty> StartToBurnCommand { get; private set; }
    public bool CanStartToBurn
    {
      get
      {
        return _CanStartToBurn;
      }
      set
      {
        if (_CanStartToBurn != value)
        {
          _CanStartToBurn = value;
          OnPropertyChanged("CanStartToBurn");
          StartToBurnCommand.RaiseCanExecuteChanged();
        }
      }
    }
    private bool _CanStartToBurn = true;
    #endregion // Commands
    #endregion // Dependency properties

    #region Constructeur
    public LayoutManagementGrid()
    {
      InitializeComponent();

      DataContext = this;

      InitLayoutManagementGrid();

    }
    #endregion // Constructeur

    #region Methods
    public bool InitLayoutManagementGrid()
    {
      const bool result = false;

      StartToBurnCommand = new SimpleCommand<IEmpty>(e => CanStartToBurn, e => GoToBurn());



      new PerformanceCounter("Memory", "Available MBytes");

      //StartThreadCoreTemp();
      return result;
    }


    // Create the OnPropertyChanged method to raise the event
    protected void OnPropertyChanged(string name_)
    {
      PropertyChangedEventHandler handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(name_));
      }
    }

    private void GoToBurn()
    {
      if (Running)
      {
        Running = false;
      }
      else
      {
        var cpuLoadThread = new Thread[Environment.ProcessorCount];

        Running = true;

        for (var i = 0; i < cpuLoadThread.Length; i++)
        {
          cpuLoadThread[i] = new Thread(CpuLoadProc)
            {
              IsBackground = true,
              Name = "CPU Load Thread",
              Priority = ThreadPriority.Highest
            };

          cpuLoadThread[i].Start();
        }
      }
    }
    void CpuLoadProc()
    {
      var stopWatch = new Stopwatch();
      stopWatch.Start();

      while (Running)
      {
        if (stopWatch.ElapsedMilliseconds > AskedCpuWork)
        {
          Thread.Sleep(100 - (int)AskedCpuWork);
          stopWatch.Reset();
          stopWatch.Start();
        }
      }
    }

    private void RetrieveCoreTemp()
    {
      while (true)
      {
        var searcher = new ManagementObjectSearcher("root\\WMI",
                           "SELECT * FROM MSAcpi_ThermalZoneTemperature");

        var enumerator = searcher.Get().GetEnumerator();

        while (enumerator.MoveNext())
        {
          var tempObject = enumerator.Current;
          CpuTemperature = tempObject["CurrentTemperature"].ToString();
        }
        Thread.Sleep(1000);
      }
    }

    public void StartThreadDeConsommation()
    {
      var newThread = new Thread(BurnCpuThread) { Priority = ThreadPriority.Highest };
      newThread.Start();
    }

    public void StartThreadCoreTemp()
    {
      ThreadPool.QueueUserWorkItem(x_ => RetrieveCoreTemp());
    }

    public void BurnCpuThread()
    {
      int temp = 0;
      while (Running)
      {
        if (AskedCpuWork > _currentCPUWork)
        {
          temp = temp + 2;
          temp = temp - 2;
        }
        else
        {
          Thread.Sleep(5);
        }
      }
    }
    #endregion // Methods

    public event PropertyChangedEventHandler PropertyChanged;
  }
}
