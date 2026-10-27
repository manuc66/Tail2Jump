using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Tail2Jump.Collections
{
  /// <summary>
  /// ObservableCollection with range notifications: adding or removing a batch
  /// raises a single CollectionChanged event instead of one per item.
  /// </summary>
  public class BulkObservableCollection<T> : ObservableCollection<T>
  {
    public void AddRange(IEnumerable<T> items)
    {
      var added = items as IList<T> ?? items.ToList();
      if (added.Count == 0) return;

      int startIndex = Count;
      foreach (var item in added)
      {
        Items.Add(item);
      }

      OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
      OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

      var changedItems = added as IList ?? added.ToList();
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(
        NotifyCollectionChangedAction.Add, changedItems, startIndex));
    }

    public void RemoveFirst(int count)
    {
      if (count <= 0) return;

      count = System.Math.Min(count, Count);
      if (count == 0) return;

      var removed = new List<T>(count);
      for (int i = 0; i < count; i++)
      {
        removed.Add(Items[i]);
      }

      // Rebuild the remaining items once instead of shifting the list for
      // every removed element.
      var remaining = new List<T>(Count - count);
      for (int i = count; i < Count; i++)
      {
        remaining.Add(Items[i]);
      }

      Items.Clear();
      foreach (var item in remaining)
      {
        Items.Add(item);
      }

      OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
      OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

      OnCollectionChanged(new NotifyCollectionChangedEventArgs(
        NotifyCollectionChangedAction.Remove, removed, 0));
    }
  }
}
