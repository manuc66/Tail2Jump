using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Be.Manuc66.Library;
using System.Diagnostics;
using System.Threading;
using System;
using System.Management;
using System.Windows.Media;
using Be.Manuc66.Library.General;

namespace Tail2Jump
{
  /// <summary>
  /// Logique d'interaction pour BurningGrid.xaml
  /// </summary>
  public partial class BurningGrid : UserControl, INotifyPropertyChanged
  {

    #region Members
    private string m_CPUTemperature = string.Empty;

    private bool m_Running;
    private double m_AskedCPUWork;
    private const double CurrentCPUWork = 100;

    #endregion // Members

    #region Properties
    public static int Tier { get { return RenderCapability.Tier >> 16; } }

    public string CPUTemperature
    {
      get { return m_CPUTemperature; }
      set
      {
        if (m_CPUTemperature != value)
        {
          m_CPUTemperature = value;
          OnPropertyChanged("CPUTemperature");
        }
      }
    }
    public double AskedCPUWork
    {
      get { return m_AskedCPUWork; }
      set
      {
        if (m_AskedCPUWork != value)
        {
          m_AskedCPUWork = value;
          OnPropertyChanged("AskedCPUWork");
        }
      }
    }
    public bool Running
    {
      get { return m_Running; }
      set
      {
        if (m_Running != value)
        {
          m_Running = value;
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
        return m_CanStartToBurn;
      }
      set
      {
        if (m_CanStartToBurn != value)
        {
          m_CanStartToBurn = value;
          OnPropertyChanged("CanStartToBurn");
          StartToBurnCommand.RaiseCanExecuteChanged();
        }
      }
    }
    private bool m_CanStartToBurn = true;
    #endregion // Commands
    #endregion // Dependency properties

    #region Constructeur

    public BurningGrid()
    {
      InitializeComponent();

      DataContext = this;
      InitDebugConsoleGrid();
      string support = string.Empty;
      if (Tier == 0)
      {
        support = "None";
      }
      else if (Tier == 1)
      {
        support = "Middle";
      }
      else if (Tier == 2)
      {
        support = "Advanced";
      }
      else
      {
        support = "Unknow";
      }

      gridLabelStress.Text = support;

      IsVisibleChanged += BurningGrid_IsVisibleChanged;
    }

    void BurningGrid_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (!(bool)e.NewValue)
      {
        monitoringChartCPU.ChartInPause();
        //Running = false;
      }
      else
      {
        monitoringChartCPU.ChartInJob();
        //InitDebugConsoleGrid();
      }
    }
    #endregion // Constructeur

    #region Methods
    public bool InitDebugConsoleGrid()
    {
      const bool result = false;

      StartToBurnCommand = new SimpleCommand<IEmpty>(e => CanStartToBurn, e => GoToBurn());

      new PerformanceCounter("Memory", "Available MBytes");
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

        for (int i = 0; i < cpuLoadThread.Length; i++)
        {
          cpuLoadThread[i] = new Thread(cpuLoadProc)
          {
            IsBackground = true,
            Name = "CPU Load Thread",
            Priority = ThreadPriority.Highest
          };

          cpuLoadThread[i].Start();
        }
      }
    }
    void cpuLoadProc()
    {
      var stopWatch = new Stopwatch();
      stopWatch.Start();

      while (Running)
      {
        if (stopWatch.ElapsedMilliseconds > AskedCPUWork)
        {
          Thread.Sleep(100 - (int)AskedCPUWork);
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

        ManagementObjectCollection.ManagementObjectEnumerator enumerator =
            searcher.Get().GetEnumerator();

        while (enumerator.MoveNext())
        {
          ManagementBaseObject tempObject = enumerator.Current;
          CPUTemperature = tempObject["CurrentTemperature"].ToString();
        }

        Thread.Sleep(1000);
      }


    }

    public void StartThreadDeConsommation()
    {
      var newThread = new Thread(BurnCpuThread) {Priority = ThreadPriority.Highest};
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
        if (AskedCPUWork > CurrentCPUWork)
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
