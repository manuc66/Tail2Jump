using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Practices.ServiceLocation;
using Tail2Jump.Enums;
using Tail2Jump.Interfaces;
using Tail2Jump.Objects;

namespace Tail2Jump.GUI.Controls
{
  /// <summary>
  /// Logique d'interaction pour MonitoringChart.xaml
  /// </summary>
  public partial class MonitoringChart : INotifyPropertyChanged
  {
    private PerformanceCounter _cpuCounter;
    DispatcherTimer _dispatcherTimer;
    const int MemorySamples = 60;

    #region DependencyProperties
    #region GraphicsType
    public GraphicsType GraphicsType
    {
      get { return (GraphicsType)GetValue(GraphicsTypeProperty); }
      set { SetValue(GraphicsTypeProperty, value); }
    }
    public static readonly DependencyProperty GraphicsTypeProperty = DependencyProperty.Register(
      "GraphicsType", typeof(GraphicsType), typeof(MonitoringChart), new PropertyMetadata(GraphicsType.CpuUsage));
    #endregion // GraphicsType
    #endregion // DependencyProperties
    private ILogController LogController { get; set; }

    public MonitoringChart()
    {
      InitializeComponent();
      LogController = ServiceLocator.Current.GetInstance<ILogController>();
      InitMemoryWatch();
      DataContext = this;
    }

    public void InitMemoryWatch()
    {
      // keep 60 seconds worth of memory by default
      LogController.AddLog(LogType.Base, "Démarrage de l'initialisation du CPU Watcher");
      if (GraphicsType == GraphicsType.CpuUsage)
      {
        _cpuCounter = new PerformanceCounter
          {
            CategoryName = "Processor",
            CounterName = "% Processor Time",
            InstanceName = "_Total"
          };
      }

      MemoryStats = new RingBuffer<IDataSample>(MemorySamples);

      _dispatcherTimer = new DispatcherTimer();

      _dispatcherTimer.Tick += DispatcherTimerTick;
      _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);

      ChartInJob();
    }



    private void DispatcherTimerTick(object sender_, EventArgs e_)
    {
      LatestMemorySample = GenerateData();
      _memoryStats.Add(LatestMemorySample);
    }

    private IDataSample GenerateData()
    {
      IDataSample result = null;

      if (GraphicsType == GraphicsType.CpuUsage)
      {
        result = new CpuUsageSample();
        ((CpuUsageSample)result).Generate(GetCurrentCpuUsage());
      }
      else if (GraphicsType == GraphicsType.Memory)
      {
        result = new MemorySample();
        ((MemorySample)result).Generate(string.Empty);
      }

      return result;
    }

    public string GetCurrentCpuUsage()
    {
      float temp = _cpuCounter.NextValue();

      if (temp > -1 && temp < 20)
      {
        LogController.AddLog(LogType.Valid, "CPU : " + temp + "% Utilisation processeur.");
      }
      else if (temp >= 20 && temp < 40)
      {
        LogController.AddLog(LogType.Base, "CPU : " + temp + "% Utilisation processeur.");
      }
      else if (temp >= 40 && temp < 60)
      {
        LogController.AddLog(LogType.Exception, "CPU : " + temp + "% Utilisation processeur.");
      }
      else if (temp >= 60 && temp < 80)
      {
        LogController.AddLog(LogType.Error, "CPU : " + temp + "% Utilisation processeur.");
      }
      else if (temp >= 80 && temp <= 100)
      {
        LogController.AddLog(LogType.Critical, "CPU : " + temp + "% Utilisation processeur.");
      }

      return temp + "%";
    }

    private RingBuffer<IDataSample> _memoryStats;
    public RingBuffer<IDataSample> MemoryStats
    {
      get { return _memoryStats; }
      set
      {
        _memoryStats = value;
        if (_memoryStats != value)
        {
          if (PropertyChanged != null)
          {
            PropertyChanged(this, new PropertyChangedEventArgs("MemoryStats"));
          }
        }
      }
    }
    private IDataSample _latestMemorySample;
    public IDataSample LatestMemorySample
    {
      get { return _latestMemorySample; }
      set
      {
        _latestMemorySample = value;
        if (_latestMemorySample != value)
        {
          if (PropertyChanged != null)
          {
            PropertyChanged(this, new PropertyChangedEventArgs("LatestMemorySample"));
          }
        }
      }
    }


    public void ChartInPause()
    {
      _dispatcherTimer.Stop();
    }

    public void ChartInJob()
    {
      var dateTime = DateTime.Now - TimeSpan.FromSeconds(MemorySamples);
      MemoryStats.Clear();
      // create blank past memory samples
      for (var i = 0; i < MemorySamples - 1; i++)
      {
        dateTime = dateTime + TimeSpan.FromSeconds(1);
        MemoryStats.Add(new MemorySample { Timestamp = dateTime });
      }

      _dispatcherTimer.Start();
    }

    #region INotifyPropertyChanged Interface

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion
  }

  public interface IDataSample
  {
    long ByteCount { get; set; }
    DateTime Timestamp { get; set; }

    void Generate(string data_);

    string HumanReadableByteCount { get; }
  }

  public class MemorySample : IDataSample
  {
    public void Generate(string data_)
    {

      ByteCount = GC.GetTotalMemory(false);
      Timestamp = DateTime.Now;
    }

    public string HumanReadableByteCount
    {
      get
      {
        var megaBytes = ByteCount / (1024 * 1024);
        return string.Format("{0}MB", megaBytes);
      }
    }
    public long ByteCount { get; set; }
    public DateTime Timestamp { get; set; }


  }

  public class CpuUsageSample : IDataSample
  {
    public void Generate(string data_)
    {
      double temp = Convert.ToDouble(data_.TrimEnd('%'));
      ByteCount = (long)temp;//long.Parse(_Data.TrimEnd('%'));
      Timestamp = DateTime.Now;
    }

    public string HumanReadableByteCount
    {
      get
      {
        var megaBytes = ByteCount;
        return string.Format("{0}%", megaBytes);
      }
    }
    public long ByteCount { get; set; }
    public DateTime Timestamp { get; set; }


  }
}
