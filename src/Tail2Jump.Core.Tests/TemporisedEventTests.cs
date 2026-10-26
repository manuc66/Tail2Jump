using System;
using System.Collections.Generic;
using System.Threading;
using Be.Manuc66.Library.Threading;
using NUnit.Framework;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class TemporisedEventTests
  {
    [Test]
    public void Set_RaisesTheEventAfterTheDelay()
    {
      using var temporisedEvent = new TemporisedEvent(50);
      using var signal = new ManualResetEventSlim(false);

      temporisedEvent.eHandle += (_, _) => signal.Set();
      temporisedEvent.Set();

      Assert.That(signal.Wait(TimeSpan.FromSeconds(5)), Is.True);
    }

    [Test]
    public void MultipleSetCalls_AreCoalesced()
    {
      using var temporisedEvent = new TemporisedEvent(150);
      int count = 0;
      using var signal = new ManualResetEventSlim(false);

      temporisedEvent.eHandle += (_, _) =>
      {
        Interlocked.Increment(ref count);
        signal.Set();
      };

      for (int i = 0; i < 10; i++)
      {
        temporisedEvent.Set();
        Thread.Sleep(10);
      }

      Assert.That(signal.Wait(TimeSpan.FromSeconds(5)), Is.True);
      Thread.Sleep(300);

      Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void Dispose_PreventsFurtherEvents()
    {
      var temporisedEvent = new TemporisedEvent(50);
      int count = 0;
      temporisedEvent.eHandle += (_, _) => Interlocked.Increment(ref count);

      temporisedEvent.Set();
      temporisedEvent.Dispose();

      Thread.Sleep(300);
      Assert.That(count, Is.Zero);
    }
  }
}
