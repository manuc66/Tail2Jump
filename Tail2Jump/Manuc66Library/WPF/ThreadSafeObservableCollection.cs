using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;

namespace Be.Manuc66.Library.WPF
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
}
