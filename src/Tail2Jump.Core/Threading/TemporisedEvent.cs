using System;
using System.Threading;

namespace Be.Manuc66.Library.Threading
{
  /// <summary>
  /// Debounced event: <see cref="Set"/> arms a timer that raises
  /// <see cref="eHandle"/> once after <see cref="TimerValue"/> milliseconds
  /// without a new call.
  /// </summary>
  public class TemporisedEvent : IDisposable
  {
    public delegate void EventHandler(object sender, EventArgs data);

    private readonly object _lockObject = new object();
    private Timer? _timer;
    private bool _disposed;

    public event EventHandler eHandle = delegate { };

    public int TimerValue { get; set; }

    public TemporisedEvent(int timerValue)
    {
      if (timerValue < 0) throw new ArgumentOutOfRangeException(nameof(timerValue));
      TimerValue = timerValue;
    }

    public void Set()
    {
      lock (_lockObject)
      {
        if (_disposed) return;

        if (_timer == null)
        {
          _timer = new Timer(OnTimer, null, Timeout.Infinite, Timeout.Infinite);
        }

        _timer.Change(TimerValue, Timeout.Infinite);
      }
    }

    private void OnTimer(object? state)
    {
      lock (_lockObject)
      {
        if (_disposed) return;
      }

      ThreadPool.QueueUserWorkItem(_ =>
      {
        eHandle(this, EventArgs.Empty);
      });
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      lock (_lockObject)
      {
        if (_disposed) return;
        _disposed = true;

        if (_timer != null)
        {
          _timer.Dispose();
          _timer = null;
        }
      }
    }
  }
}
