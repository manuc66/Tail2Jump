using System;
using Be.Manuc66.Library.FileIO;
using NUnit.Framework;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class TailLineCacheTests
  {
    [Test]
    public void AddData_PrefixesLinesWithTheFileName()
    {
      using var cache = new TailLineCache();

      cache.AddData(new TailEventData("app.log", "line1" + Environment.NewLine + "line2", 0, 0));

      Assert.That(cache.LineIndexed, Is.EqualTo(2));
      Assert.That(cache.GetLines(0, 2), Is.EqualTo(new[] { "app.log|line1", "app.log|line2" }));
    }

    [Test]
    public void AddData_WithFilter_KeepsOnlyMatchingLines()
    {
      using var cache = new TailLineCache();

      cache.AddData(new TailEventData("app.log",
        "error: boom" + Environment.NewLine + "info: ok", 0, 0), "ERROR");

      Assert.That(cache.LineIndexed, Is.EqualTo(1));
      Assert.That(cache.GetLines(0, 1), Is.EqualTo(new[] { "app.log|error: boom" }));
    }

    [Test]
    public void AddData_WithFilterOnFileName_KeepsAllLinesOfThatFile()
    {
      using var cache = new TailLineCache();

      cache.AddData(new TailEventData("other.log",
        "something" + Environment.NewLine + "else", 0, 0), "app.log");
      cache.AddData(new TailEventData("app.log",
        "something" + Environment.NewLine + "else", 0, 0), "app.log");

      Assert.That(cache.LineIndexed, Is.EqualTo(2));
      Assert.That(cache.GetLines(0, 2), Is.EqualTo(new[] { "app.log|something", "app.log|else" }));
    }

    [Test]
    public void GetLines_ReturnsAnEmptyArrayOutOfBounds()
    {
      using var cache = new TailLineCache();
      cache.AddData(new TailEventData("app.log", "line1", 0, 0));

      Assert.That(cache.GetLines(-1, 5), Is.Empty);
      Assert.That(cache.GetLines(2, 5), Is.Empty);
    }

    [Test]
    public void AddData_FromAnotherCache_ReplaysFilteredLines()
    {
      using var source = new TailLineCache();
      using var target = new TailLineCache();

      source.AddData(new TailEventData("app.log", "keep me" + Environment.NewLine + "drop me", 0, 0));
      target.AddData(source, "keep");

      Assert.That(target.GetLines(0, 1), Is.EqualTo(new[] { "app.log|keep me" }));
    }

    [Test]
    public void AddData_WithCrLf_SplitsLinesWithoutCarriageReturn()
    {
      using var cache = new TailLineCache();

      cache.AddData(new TailEventData("app.log", "one\r\ntwo\r\nthree", 0, 0));

      Assert.That(cache.GetLines(0, 3), Is.EqualTo(new[] { "app.log|one", "app.log|two", "app.log|three" }));
    }

    [Test]
    public void AddData_FromAnotherCacheWithoutFilter_DoesNotThrow()
    {
      using var source = new TailLineCache();
      using var target = new TailLineCache();
      source.AddData(new TailEventData("app.log", "line", 0, 0));

      Assert.DoesNotThrow(() => target.AddData(source, null));
      Assert.That(target.GetLines(0, 1), Is.EqualTo(new[] { "app.log|line" }));
    }

    [Test]
    public void GetLines_WithNegativeLength_ReturnsAnEmptyArray()
    {
      using var cache = new TailLineCache();
      cache.AddData(new TailEventData("app.log", "line", 0, 0));

      Assert.That(cache.GetLines(0, -1), Is.Empty);
    }

    [Test]
    public void Clear_RemovesEverything()
    {
      using var cache = new TailLineCache();
      cache.AddData(new TailEventData("app.log", "line1", 0, 0));

      cache.Clear();

      Assert.That(cache.LineIndexed, Is.Zero);
    }
  }
}
