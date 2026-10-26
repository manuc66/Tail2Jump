using System;
using Be.Manuc66.Library.Events;
using NUnit.Framework;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class EventAggregatorTests
  {
    private class StringEvent : PubSubEvent<string>
    {
    }

    [Test]
    public void Publish_NotifiesEverySubscriber()
    {
      var aggregator = new EventAggregator();
      int calls = 0;
      aggregator.GetEvent<StringEvent>().Subscribe(_ => calls++);
      aggregator.GetEvent<StringEvent>().Subscribe(_ => calls++);

      aggregator.GetEvent<StringEvent>().Publish("payload");

      Assert.That(calls, Is.EqualTo(2));
    }

    [Test]
    public void Publish_ContinuesWhenASubscriberThrows()
    {
      var aggregator = new EventAggregator();
      bool secondCalled = false;
      aggregator.GetEvent<StringEvent>().Subscribe(_ => throw new InvalidOperationException("boom"));
      aggregator.GetEvent<StringEvent>().Subscribe(_ => secondCalled = true);

      Assert.DoesNotThrow(() => aggregator.GetEvent<StringEvent>().Publish("payload"));
      Assert.That(secondCalled, Is.True);
    }

    [Test]
    public void Unsubscribe_RemovesTheHandler()
    {
      var aggregator = new EventAggregator();
      int calls = 0;
      Action<string> handler = _ => calls++;
      aggregator.GetEvent<StringEvent>().Subscribe(handler);

      aggregator.GetEvent<StringEvent>().Unsubscribe(handler);
      aggregator.GetEvent<StringEvent>().Publish("payload");

      Assert.That(calls, Is.Zero);
    }
  }
}
