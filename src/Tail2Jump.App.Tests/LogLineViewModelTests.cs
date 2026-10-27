using System.Linq;
using NUnit.Framework;
using Tail2Jump.ViewModels;

namespace Tail2Jump.App.Tests
{
  [TestFixture]
  public class LogLineViewModelTests
  {
    [Test]
    public void Segments_AreParsedOnceAndCached()
    {
      var line = new LogLineViewModel("hello world");

      var first = line.Segments;
      var second = line.Segments;

      Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void ConstructorFromEntry_BuildsTheRawLineOnDemand()
    {
      var line = new LogLineViewModel("app.log", "hello");

      Assert.That(line.RawLine, Is.EqualTo("app.log|hello"));
      Assert.That(line.RawLine, Is.SameAs(line.RawLine));
    }

    [Test]
    public void ApplySearch_BeforeTheLineIsRealized_DoesNotNotify()
    {
      var line = new LogLineViewModel("app.log", "an ERROR happened");
      int notifications = 0;
      line.PropertyChanged += (_, _) => notifications++;

      line.ApplySearch("error", ignoreCase: true);

      Assert.That(notifications, Is.Zero);
      Assert.That(line.Segments.Any(segment => segment.IsHighlighted), Is.True);
    }

    [Test]
    public void ApplySearch_AfterTheLineIsRealized_NotifiesOnce()
    {
      var line = new LogLineViewModel("app.log", "an ERROR happened");
      _ = line.Segments;
      int notifications = 0;
      line.PropertyChanged += (_, _) => notifications++;

      line.ApplySearch("error", ignoreCase: true);

      Assert.That(notifications, Is.EqualTo(1));
    }

    [Test]
    public void ApplySearch_InvalidatesTheCachedSegments()
    {
      var line = new LogLineViewModel("an ERROR happened");
      var initial = line.Segments;

      line.ApplySearch("error", ignoreCase: true);

      Assert.That(line.Segments, Is.Not.SameAs(initial));
      Assert.That(line.Segments.Any(segment => segment.IsHighlighted), Is.True);
    }

    [Test]
    public void ApplySearch_WithTheSameParameters_KeepsTheCache()
    {
      var line = new LogLineViewModel("an ERROR happened");
      line.ApplySearch("error", ignoreCase: true);
      var cached = line.Segments;

      line.ApplySearch("error", ignoreCase: true);

      Assert.That(line.Segments, Is.SameAs(cached));
    }
  }
}
