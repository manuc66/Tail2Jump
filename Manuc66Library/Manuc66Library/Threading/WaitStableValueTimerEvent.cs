using System;
using System.Threading;
using System.Threading.Tasks;
using Be.Manuc66.Library.General;
using log4net;

namespace Be.Manuc66.Library.Threading
{
  public class WaitStableValueTimerEvent<T> : IDisposable
  {
    #region Event
    #region ReceivedItem
    private StrongTypedEvent<WaitStableValueTimerEvent<T>, T> _Handle;
    public event StrongTypedEvent<WaitStableValueTimerEvent<T>, T>.EventEventHandler eHandle
    {
      add { _Handle.eEvent += value; }
      remove { _Handle.eEvent -= value; }
    }
    #endregion // ReceivedItem
    #endregion // Event

    #region Properties
    public int TimerValue { get; set; }
    #endregion // Properties

    private T _value;
    private readonly object _lockObject = new object();
    private ManualResetEvent _refreshThreadReset;
    private readonly CancellationTokenSource _cancellationTokenSourceTimerTask;

    #region
    public WaitStableValueTimerEvent(int timerValue_)
    {
      TimerValue = timerValue_;
      _refreshThreadReset = new ManualResetEvent(false);
      _Handle = new StrongTypedEvent<WaitStableValueTimerEvent<T>, T>(true);

      _cancellationTokenSourceTimerTask = new CancellationTokenSource();
      Task.Factory.StartNew(TimerThread, TaskCreationOptions.LongRunning, _cancellationTokenSourceTimerTask.Token);
    }
    #endregion // Constructor

    #region Methods
    public void Set(T value_)
    {
      lock (_lockObject)
      {
        _value = value_;
        _refreshThreadReset.Set();
      }
    }

    private void TimerThread(object o_)
    {
      while (true)
      {
        _refreshThreadReset.WaitOne();
        while (true)
        {
          // boucle d'attente temporisée
          if (_refreshThreadReset.WaitOne(TimerValue))
          {
            lock (_lockObject)
            { // si on a reçu une valeur, on relance une attente sur timer
              _refreshThreadReset.Reset();
              continue;
            }
          }
          lock (_lockObject)
          { // on a pas reçu une valeur, on lance un event et on termine la boucle d'attente temporisée
            if (_Handle.IsSubscriber)
            {
              _Handle.RaiseEvent(this, _value);
            }
            _refreshThreadReset.Reset();
            break;
          }
        }
      }
    }

    #region IDisposable Members
    ///<summary>
    ///Implementation of the IDisposable interface
    ///</summary>
    public void Dispose()
    {
      // Call internal Dispose(bool)
      Dispose(true);

      // Prevent the destructor from being called
      GC.SuppressFinalize(this);
    }

    ///<summary>
    /// Central method for cleaning up resources
    ///</summary>
    protected virtual void Dispose(bool explicit_)
    {
      // If explicit is true, then this method was called through the
      // public Dispose()
      if (explicit_)
      {
        // Release or cleanup managed resources

        _cancellationTokenSourceTimerTask.Cancel();

        if (_refreshThreadReset != null)
        {
          _refreshThreadReset.Dispose();
          _refreshThreadReset = null;
        }
      }

    }

    ~WaitStableValueTimerEvent()
    {
      // Since other managed objects are disposed automatically, we
      // should not try to dispose any managed resources.
      // We therefore pass false to Dispose()
      Dispose(false);
    }
    #endregion IDisposable Members
    #endregion // Methods
  }
}
