using System.Collections.Specialized;
using NUnit.Framework;
using Tail2Jump.Collections;

namespace Tail2Jump.App.Tests
{
  [TestFixture]
  public class BulkObservableCollectionTests
  {
    [Test]
    public void AddRange_RaisesASingleCollectionChangedEvent()
    {
      var collection = new BulkObservableCollection<int>();
      int events = 0;
      NotifyCollectionChangedAction? action = null;
      collection.CollectionChanged += (_, e) =>
      {
        events++;
        action = e.Action;
      };

      collection.AddRange(new[] { 1, 2, 3, 4, 5 });

      Assert.That(events, Is.EqualTo(1));
      Assert.That(action, Is.EqualTo(NotifyCollectionChangedAction.Add));
      Assert.That(collection, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void RemoveFirst_RemovesTheItemsWithASingleEvent()
    {
      var collection = new BulkObservableCollection<int>();
      collection.AddRange(new[] { 1, 2, 3, 4, 5 });
      int events = 0;
      collection.CollectionChanged += (_, _) => events++;

      collection.RemoveFirst(2);

      Assert.That(events, Is.EqualTo(1));
      Assert.That(collection, Is.EqualTo(new[] { 3, 4, 5 }));
    }

    [Test]
    public void AddRange_WithNoItems_DoesNothing()
    {
      var collection = new BulkObservableCollection<int>();
      int events = 0;
      collection.CollectionChanged += (_, _) => events++;

      collection.AddRange(System.Array.Empty<int>());

      Assert.That(events, Is.Zero);
      Assert.That(collection, Is.Empty);
    }
  }
}
