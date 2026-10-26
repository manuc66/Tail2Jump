using System;
using System.Globalization;
using System.Threading;
using Be.Manuc66.Library.Mvvm;
using Tail2Jump.Charting;
using Tail2Jump.Monitoring;

namespace Tail2Jump.ViewModels
{
  /// <summary>
  /// Samples CPU and memory once per second and keeps a rolling series for the charts.
  /// </summary>
  public class PerformanceViewModel : ObservableObject, IDisposable
  {
    private const int SampleCount = 60;
    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(1);

    private readonly ISystemMetrics _metrics;
    private readonly SynchronizationContext _uiContext;
    private Timer _timer;
    private bool _isRunning;
    private bool _disposed;

    public PerformanceViewModel(ISystemMetrics metrics, SynchronizationContext uiContext = null)
    {
      _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
      _uiContext = uiContext;
      Cpu = new MetricSeries(SampleCount, autoScale: false, maximum: 100);
      Memory = new MetricSeries(SampleCount, autoScale: true);

      // Start with a flat line so the chart has a full width from the beginning.
      for (int i = 0; i < SampleCount - 1; i++)
      {
        Cpu.Add(0);
        Memory.Add(0);
      }
    }

    public MetricSeries Cpu { get; }

    public MetricSeries Memory { get; }

    public string CpuDisplay => Cpu.Latest.ToString("0.0", CultureInfo.InvariantCulture) + " %";

    public string MemoryDisplay => Memory.Latest.ToString("0", CultureInfo.InvariantCulture) + " MB";

    public string MemoryDetails
    {
      get
      {
        long total = _metrics.GetTotalMemoryBytes();
        long available = _metrics.GetAvailableMemoryBytes();

        if (total <= 0) return "Physical memory unavailable";

        const double megabyte = 1024.0 * 1024.0;
        var availableMb = (available / megabyte).ToString("0", CultureInfo.InvariantCulture);
        var totalMb = (total / megabyte).ToString("0", CultureInfo.InvariantCulture);
        return $"{availableMb} MB free of {totalMb} MB";
      }
    }

    public bool IsRunning
    {
      get { return _isRunning; }
      set
      {
        if (!SetProperty(ref _isRunning, value)) return;

        if (value)
        {
          StartTimer();
        }
        else
        {
          StopTimer();
        }
      }
    }

    /// <summary>Adds one sample. Public so it can be driven by tests or a debugger.</summary>
    public void SampleOnce()
    {
      double cpu = _metrics.GetCpuUsagePercent();
      double memoryMb = _metrics.GetProcessMemoryBytes() / (1024.0 * 1024.0);

      Cpu.Add(cpu);
      Memory.Add(memoryMb);

      OnPropertyChanged(nameof(CpuDisplay));
      OnPropertyChanged(nameof(MemoryDisplay));
      OnPropertyChanged(nameof(MemoryDetails));
    }

    private void StartTimer()
    {
      StopTimer();
      _timer = new Timer(_ => PostToUi(SampleOnce), null, SampleInterval, SampleInterval);
    }

    private void StopTimer()
    {
      _timer?.Dispose();
      _timer = null;
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

    public void Dispose()
    {
      if (_disposed) return;
      _disposed = true;
      StopTimer();
    }
  }
}
