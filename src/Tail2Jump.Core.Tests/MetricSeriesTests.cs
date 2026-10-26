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
    public void AutoScale_UpdatesTheMaximum()
    {
      var series = new MetricSeries(10, autoScale: true);

      series.Add(100);
      series.Add(200);

      Assert.That(series.Maximum, Is.GreaterThanOrEqualTo(200));
    }

    [Test]
    public void FixedScale_KeepsTheGivenMaximum()
    {
      var series = new MetricSeries(10, autoScale: false, maximum: 100);

      series.Add(42);

      Assert.That(series.Maximum, Is.EqualTo(100));
    }

    [Test]
    public void Clear_RemovesValuesAndResetsTheMaximum()
    {
      var series = new MetricSeries(10, autoScale: true);
      series.Add(500);

      series.Clear();

      Assert.That(series.Values, Is.Empty);
      Assert.That(series.Maximum, Is.EqualTo(1));
    }
  }
}
