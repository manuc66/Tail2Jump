using System;
using System.Threading;

namespace Be.Manuc66.Library.Threading
{
  public class TemporisedEvent : IDisposable
  {
    #region Event
    #region ReceivedItem
    public delegate void EventHandler(object sender, EventArgs data);
    public event EventHandler eHandle = delegate { };
    #endregion // ReceivedItem
    #endregion // Event

    #region Properties
    public int TimerValue { get; set; }
    #endregion // Properties

    #region Members variables
    private readonly object _lockObject = new object();
    private ManualResetEvent _refreshThreadReset;
    #endregion // Members variables

    #region Constructor
    public TemporisedEvent(int timerValue_)
    {
      TimerValue = timerValue_;
      _refreshThreadReset = new ManualResetEvent(false);

      ThreadPool.RegisterWaitForSingleObject(_refreshThreadReset, Wait, this, int.MaxValue, true);
    }
    #endregion // Constructor

    #region Methods
    public void Set()
    {
      lock (_lockObject)
      {
        _refreshThreadReset.Set();
      }
    }

    void Wait(object state_, bool timedOut_)
    {
      if (timedOut_) return;
      var mre = new ManualResetEvent(false);
      ThreadPool.RegisterWaitForSingleObject(mre, Process, mre, TimerValue, true);
    }

    void Process(object state_, bool timedOut_)
    {
      if (timedOut_)
      {
        ((ManualResetEvent)state_).Dispose();
        lock (_lockObject)
        {
          ThreadPool.QueueUserWorkItem(x => eHandle(this, EventArgs.Empty));

          _refreshThreadReset.Reset();
          ThreadPool.RegisterWaitForSingleObject(_refreshThreadReset, Wait, this, -1, true);
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
      }

      _refreshThreadReset.Set();

      if (_refreshThreadReset != null)
      {
        _refreshThreadReset.Dispose();
        _refreshThreadReset = null;
      }
    }

    ~TemporisedEvent()
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
