using System.Linq;
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
    public void AddData_WithNullContent_DoesNotThrow()
    {
      using var cache = new TailLineCache();

      Assert.DoesNotThrow(() => cache.AddData(new TailEventData("app.log", (string?)null, 0, 0)));
      Assert.That(cache.LineIndexed, Is.Zero);
    }

    [Test]
    public void AddData_WithFilter_IsCaseInsensitive()
    {
      using var cache = new TailLineCache();

      cache.AddData(new TailEventData("app.log",
        "error lower" + Environment.NewLine + "ERROR upper" + Environment.NewLine + "info", 0, 0), "Error");

      Assert.That(cache.LineIndexed, Is.EqualTo(2));
    }

    [Test]
    public void AddData_FromAnotherCache_ReplaysLargeCachesInChunks()
    {
      using var source = new TailLineCache();
      using var target = new TailLineCache();

      var builder = new System.Text.StringBuilder();
      for (int i = 0; i < 5000; i++)
      {
        builder.Append("line ").Append(i).Append('\n');
      }

      source.AddData(new TailEventData("app.log", builder.ToString(), 0, 0));
      target.AddData(source, "line 4999");

      Assert.That(target.LineIndexed, Is.EqualTo(1));
      Assert.That(target.GetLines(0, 1)[0], Is.EqualTo("app.log|line 4999"));
    }

    [Test]
    public void AddData_FromItself_IsIgnored()
    {
      using var cache = new TailLineCache();
      cache.AddData(new TailEventData("app.log", "line", 0, 0));

      Assert.DoesNotThrow(() => cache.AddData(cache, null));
      Assert.That(cache.LineIndexed, Is.EqualTo(1));
    }

    [Test]
    public void AddData_BeyondCapacity_DropsTheOldestLines()
    {
      using var cache = new TailLineCache(maxLines: 5);

      cache.AddData(new TailEventData("app.log",
        "1" + Environment.NewLine + "2" + Environment.NewLine + "3" + Environment.NewLine +
        "4" + Environment.NewLine + "5" + Environment.NewLine + "6" + Environment.NewLine +
        "7" + Environment.NewLine + "8" + Environment.NewLine + "9" + Environment.NewLine + "10", 0, 0));

      Assert.That(cache.LineIndexed, Is.EqualTo(10));
      Assert.That(cache.FirstLineIndex, Is.EqualTo(5));
      Assert.That(cache.GetLines(0, 10), Is.EqualTo(new[]
      {
        "app.log|6", "app.log|7", "app.log|8", "app.log|9", "app.log|10"
      }));
      Assert.That(cache.GetLines(5, 5), Is.EqualTo(new[]
      {
        "app.log|6", "app.log|7", "app.log|8", "app.log|9", "app.log|10"
      }));
    }

    [Test]
    public void AddData_FromATrimmedCache_OnlyReplaysKeptLines()
    {
      using var source = new TailLineCache(maxLines: 2);
      using var target = new TailLineCache();

      source.AddData(new TailEventData("app.log",
        "one" + Environment.NewLine + "two" + Environment.NewLine + "three", 0, 0));

      target.AddData(source, null);

      Assert.That(target.GetLines(0, 10), Is.EqualTo(new[] { "app.log|two", "app.log|three" }));
    }

    [Test]
    public void RingBuffer_WrapsWhenTheCapacityIsReached()
    {
      using var cache = new TailLineCache(maxLines: 5);

      for (int i = 0; i < 10; i++)
      {
        cache.AddData(new TailEventData("app.log", i.ToString(), 0, 0));
      }

      Assert.That(cache.LineIndexed, Is.EqualTo(10));
      Assert.That(cache.FirstLineIndex, Is.EqualTo(5));
      Assert.That(cache.GetLines(5, 5), Is.EqualTo(new[]
      {
        "app.log|5", "app.log|6", "app.log|7", "app.log|8", "app.log|9"
      }));
    }

    [Test]
    public void RingBuffer_GrowsBeyondItsInitialCapacity()
    {
      using var cache = new TailLineCache(maxLines: 5_000);
      var lines = System.Linq.Enumerable.Range(0, 2_000).Select(i => "line " + i).ToArray();

      cache.AddData(new TailEventData("app.log", lines, 0, 0));

      Assert.That(cache.LineIndexed, Is.EqualTo(2_000));
      Assert.That(cache.GetLines(1_990, 10)[0], Is.EqualTo("app.log|line 1990"));
      Assert.That(cache.GetLines(1_990, 10)[^1], Is.EqualTo("app.log|line 1999"));
    }

    [Test]
    public void RingBuffer_ReadAcrossTheWrapBoundary()
    {
      using var cache = new TailLineCache(maxLines: 8);
      var lines = System.Linq.Enumerable.Range(0, 20).Select(i => "line " + i).ToArray();

      cache.AddData(new TailEventData("app.log", lines, 0, 0));

      Assert.That(cache.FirstLineIndex, Is.EqualTo(12));
      Assert.That(cache.GetLines(12, 8), Is.EqualTo(new[]
      {
        "app.log|line 12", "app.log|line 13", "app.log|line 14", "app.log|line 15",
        "app.log|line 16", "app.log|line 17", "app.log|line 18", "app.log|line 19"
      }));
    }

    [Test]
    public void UsingTheCacheAfterDispose_DoesNotThrow()
    {
      var cache = new TailLineCache();
      cache.AddData(new TailEventData("app.log", "line", 0, 0));
      cache.Dispose();

      Assert.DoesNotThrow(() => cache.GetLines(0, 1));
      Assert.DoesNotThrow(() => cache.Clear());
      Assert.DoesNotThrow(() => cache.AddData(new TailEventData("app.log", "more", 0, 0)));
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
