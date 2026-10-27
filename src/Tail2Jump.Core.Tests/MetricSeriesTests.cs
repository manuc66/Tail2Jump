using System;
using NUnit.Framework;
using Tail2Jump.Charting;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class MetricSeriesTests
  {
    [Test]
    public void Add_KeepsOnlyTheConfiguredCapacity()
    {
      var series = new MetricSeries(3);

      series.Add(1);
      series.Add(2);
      series.Add(3);
      series.Add(4);

      Assert.That(series.Values, Is.EqualTo(new[] { 2.0, 3.0, 4.0 }));
      Assert.That(series.Latest, Is.EqualTo(4));
    }

    [Test]
    public void Latest_IsZeroWhenEmpty()
    {
      Assert.That(new MetricSeries(3).Latest, Is.Zero);
    }

    [Test]
    public void Constructor_WithAnInvalidCapacity_Throws()
    {
      Assert.Throws<ArgumentOutOfRangeException>(() => new MetricSeries(0));
    }
  }
}
