using System;
using System.Linq;
using System.Text;
using Be.Manuc66.Library.FileIO;
using NUnit.Framework;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class Utf8LineSplitterTests
  {
    [Test]
    public void SplitsLfLinesAndReportsConsumedBytes()
    {
      var buffer = Encoding.UTF8.GetBytes("one\ntwo\n");

      bool result = Utf8LineSplitter.TrySplitCompleteLines(buffer, out var lines, out int consumed);

      Assert.That(result, Is.True);
      Assert.That(lines, Is.EqualTo(new[] { "one", "two" }));
      Assert.That(consumed, Is.EqualTo(buffer.Length));
    }

    [Test]
    public void SplitsCrLfAndStripsTheCarriageReturn()
    {
      var buffer = Encoding.UTF8.GetBytes("one\r\ntwo\r\n");

      Utf8LineSplitter.TrySplitCompleteLines(buffer, out var lines, out int consumed);

      Assert.That(lines, Is.EqualTo(new[] { "one", "two" }));
      Assert.That(consumed, Is.EqualTo(buffer.Length));
    }

    [Test]
    public void LeavesThePartialLineForTheNextRead()
    {
      var buffer = Encoding.UTF8.GetBytes("one\ntwo");

      Utf8LineSplitter.TrySplitCompleteLines(buffer, out var lines, out int consumed);

      Assert.That(lines, Is.EqualTo(new[] { "one" }));
      Assert.That(consumed, Is.EqualTo(4));
    }

    [Test]
    public void DecodesMultiByteCharacters()
    {
      var buffer = Encoding.UTF8.GetBytes("héllo\n");

      Utf8LineSplitter.TrySplitCompleteLines(buffer, out var lines, out int consumed);

      Assert.That(lines, Is.EqualTo(new[] { "héllo" }));
      Assert.That(consumed, Is.EqualTo(buffer.Length));
    }

    [Test]
    public void ReturnsFalseWithoutAnyNewLine()
    {
      var buffer = Encoding.UTF8.GetBytes("partial");

      Assert.That(Utf8LineSplitter.TrySplitCompleteLines(buffer, out _, out int consumed), Is.False);
      Assert.That(consumed, Is.Zero);
    }

    [Test]
    public void PreservesEmptyLines()
    {
      var buffer = Encoding.UTF8.GetBytes("\n\ntext\n");

      Utf8LineSplitter.TrySplitCompleteLines(buffer, out var lines, out _);

      Assert.That(lines, Is.EqualTo(new[] { string.Empty, string.Empty, "text" }));
    }
  }
}
