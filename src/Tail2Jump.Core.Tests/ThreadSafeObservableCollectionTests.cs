using Be.Manuc66.Library.Collections;
using NUnit.Framework;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class ThreadSafeObservableCollectionTests
  {
    [Test]
    public void Add_IsAppliedBeforeReturning()
    {
      var collection = new ThreadSafeObservableCollection<int>();

      collection.Add(42);

      Assert.That(collection, Has.Count.EqualTo(1));
      Assert.That(collection[0], Is.EqualTo(42));
    }

    [Test]
    public void Remove_IsAppliedBeforeReturning()
    {
      var collection = new ThreadSafeObservableCollection<int>();
      collection.Add(1);

      collection.Remove(1);

      Assert.That(collection, Is.Empty);
    }

    [Test]
    public void AddRange_AddsEveryItem()
    {
      var collection = new ThreadSafeObservableCollection<int>();

      collection.AddRange(new[] { 1, 2, 3 });

      Assert.That(collection, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void ToSyncArray_ReturnsASnapshot()
    {
      var collection = new ThreadSafeObservableCollection<int>();
      collection.AddRange(new[] { 1, 2, 3 });

      Assert.That(collection.ToSyncArray(), Is.EqualTo(new[] { 1, 2, 3 }));
    }
  }
}
