using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Be.Manuc66.Library.Collections
{
  /// <summary>
  /// Provides a thread-safe <see cref="ObservableCollection{T}"/>.
  /// Mutations are marshalled to the synchronization context captured at
  /// construction time (the UI thread for controls, or none in tests).
  /// </summary>
  public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
  {
    private readonly SynchronizationContext _context;
    private readonly ReaderWriterLockSlim _lock;

    public ThreadSafeObservableCollection()
    {
      _context = SynchronizationContext.Current;
      _lock = new ReaderWriterLockSlim();
    }

    private void Invoke(Action action)
    {
      // Already on the owning thread (or no synchronization context): run inline so
      // that Count/Contains are up to date as soon as Add/Remove returns.
      if (_context == null || ReferenceEquals(SynchronizationContext.Current, _context))
      {
        action();
        return;
      }

      _context.Send(_ => action(), null);
    }

    protected override void ClearItems()
    {
      Invoke(() =>
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
      });
    }

    protected override void InsertItem(int index, T item)
    {
      Invoke(() =>
      {
        if (index > Count) return;

        _lock.EnterWriteLock();
        try
        {
          base.InsertItem(index, item);
        }
        finally
        {
          _lock.ExitWriteLock();
        }
      });
    }

    protected override void MoveItem(int oldIndex, int newIndex)
    {
      Invoke(() =>
      {
        _lock.EnterReadLock();
        int itemCount;
        try
        {
          itemCount = Count;
        }
        finally
        {
          _lock.ExitReadLock();
        }

        if (oldIndex >= itemCount || newIndex >= itemCount || oldIndex == newIndex)
          return;

        _lock.EnterWriteLock();
        try
        {
          base.MoveItem(oldIndex, newIndex);
        }
        finally
        {
          _lock.ExitWriteLock();
        }
      });
    }

    protected override void RemoveItem(int index)
    {
      Invoke(() =>
      {
        if (index >= Count) return;

        _lock.EnterWriteLock();
        try
        {
          base.RemoveItem(index);
        }
        finally
        {
          _lock.ExitWriteLock();
        }
      });
    }

    protected override void SetItem(int index, T item)
    {
      Invoke(() =>
      {
        _lock.EnterWriteLock();
        try
        {
          base.SetItem(index, item);
        }
        finally
        {
          _lock.ExitWriteLock();
        }
      });
    }

    /// <summary>
    /// Returns a snapshot copy of the collection.
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

    public void AddRange(IEnumerable<T> collection)
    {
      if (collection == null) return;

      var toAppend = new List<T>(collection);
      Invoke(() =>
      {
        _lock.EnterWriteLock();
        try
        {
          // Use the base implementation: calling Add() here would try to take
          // the (non-recursive) write lock a second time.
          foreach (var item in toAppend)
          {
            base.InsertItem(Count, item);
          }
        }
        finally
        {
          _lock.ExitWriteLock();
        }
      });
    }
  }
}
