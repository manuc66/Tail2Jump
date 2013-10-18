using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;

namespace Be.Manuc66.Library
{


  /// <summary>
  /// Provides a threadsafe ObservableCollection of T
  /// </summary>

  public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
  {
    #region Data
    private readonly Dispatcher _dispatcher;
    private readonly ReaderWriterLockSlim _lock;
    #endregion
    #region Ctor
    public ThreadSafeObservableCollection()
    {
      _dispatcher = Dispatcher.CurrentDispatcher;
      _lock = new ReaderWriterLockSlim();
    }

    #endregion
    #region Overrides
    /// <summary>
    /// Clear all items
    /// </summary>
    protected override void ClearItems()
    {
      _dispatcher.InvokeIfRequired(() =>
          {
            _lock.EnterWriteLock();
            try
            {
              base.ClearItems();
            }
            finally
            {
              _lock.ExitWriteLock();
            }
          }, DispatcherPriority.DataBind);
    }



    /// <summary>
    /// Inserts an item
    /// </summary>
    protected override void InsertItem(int index_, T item_)
    {
      _dispatcher.InvokeIfRequired(() =>
      {
        if (index_ > Count)
          return;

        _lock.EnterWriteLock();
        try
        {
          base.InsertItem(index_, item_);
        }
        finally
        {
          _lock.ExitWriteLock();
        }
      }, DispatcherPriority.DataBind);
    }



    /// <summary>
    /// Moves an item
    /// </summary>
    protected override void MoveItem(int oldIndex_, int newIndex_)
    {
      _dispatcher.InvokeIfRequired(() =>
      {
        _lock.EnterReadLock();
        Int32 itemCount = Count;
        _lock.ExitReadLock();

        if (oldIndex_ >= itemCount |
            newIndex_ >= itemCount |
            oldIndex_ == newIndex_)
          return;

        _lock.EnterWriteLock();
        try
        {
          base.MoveItem(oldIndex_, newIndex_);
        }
        finally
        {
          _lock.ExitWriteLock();
        }
      }, DispatcherPriority.DataBind);
    }

    /// <summary>
    /// Removes an item
    /// </summary>
    protected override void RemoveItem(int index_)
    {
      _dispatcher.InvokeIfRequired(() =>
      {
        if (index_ >= Count)
          return;

        _lock.EnterWriteLock();
        try
        {
          base.RemoveItem(index_);
        }
        finally
        {
          _lock.ExitWriteLock();
        }
      }, DispatcherPriority.DataBind);
    }

    /// <summary>
    /// Sets an item
    /// </summary>
    protected override void SetItem(int index_, T item_)
    {
      _dispatcher.InvokeIfRequired(() =>
      {
        _lock.EnterWriteLock();
        try
        {
          base.SetItem(index_, item_);
        }
        finally
        {
          _lock.ExitWriteLock();
        }
      }, DispatcherPriority.DataBind);
    }

    #endregion
    #region Public Methods

    /// <summary>
    /// Return as a cloned copy of this Collection
    /// </summary>
    public T[] ToSyncArray()
    {
      _lock.EnterReadLock();
      try
      {
        var sync = new T[Count];
        CopyTo(sync, 0);
        return sync;
      }
      finally
      {
        _lock.ExitReadLock();
      }
    }
    public void AddRange(IEnumerable<T> collection_)
    {
      var toAppend = new List<T>(collection_);

      _dispatcher.InvokeIfRequired(() =>
      {
        _lock.EnterWriteLock();
        try
        {
          foreach (T t in toAppend)
          {
            Add(t);
          }
        }
        finally
        {
          _lock.ExitWriteLock();
        }
      }, DispatcherPriority.DataBind);
    }
    #endregion
  }

  /// <summary>
  /// WPF Threading extension methods
  /// </summary>
  public static class WpfControlThreadingExtensions
  {
    #region Public Methods
    /// <summary>
    /// A simple WPF threading extension method, to invoke a delegate
    /// on the correct thread if it is not currently on the correct thread
    /// Which can be used with DispatcherObject types
    /// </summary>
    /// <param name="disp_">The Dispatcher object on which to do the Invoke</param>
    /// <param name="dotIt_">The delegate to run</param>
    /// <param name="priority_">The DispatcherPriority</param>
    public static void InvokeIfRequired(this Dispatcher disp_, Action dotIt_, DispatcherPriority priority_)
    {
      if (dotIt_ == null || disp_ == null)
      {
        return;
      }
      if (disp_.Thread != Thread.CurrentThread)
      {
        disp_.Invoke(priority_, dotIt_);
      }
      else
        dotIt_();
    }
    #endregion

  }
}
