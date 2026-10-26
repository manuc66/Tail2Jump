using NUnit.Framework;
using Be.Manuc66.Library.FileIO;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class LineSplitterTests
  {
    [Test]
    public void TrySplitCompleteLines_WithLf_ReturnsCompleteLines()
    {
      bool result = LineSplitter.TrySplitCompleteLines("one\ntwo\n", out var lines, out int consumed);

      Assert.That(result, Is.True);
      Assert.That(lines, Is.EqualTo(new[] { "one", "two" }));
      Assert.That(consumed, Is.EqualTo(8));
    }

    [Test]
    public void TrySplitCompleteLines_WithCrLf_StripsTheCarriageReturn()
    {
      bool result = LineSplitter.TrySplitCompleteLines("one\r\ntwo\r\n", out var lines, out int consumed);

      Assert.That(result, Is.True);
      Assert.That(lines, Is.EqualTo(new[] { "one", "two" }));
      Assert.That(consumed, Is.EqualTo(10));
    }

    [Test]
    public void TrySplitCompleteLines_WithPartialLine_OnlyReturnsTheCompletePart()
    {
      bool result = LineSplitter.TrySplitCompleteLines("one\ntwo", out var lines, out int consumed);

      Assert.That(result, Is.True);
      Assert.That(lines, Is.EqualTo(new[] { "one" }));
      Assert.That(consumed, Is.EqualTo(4));
    }

    [Test]
    public void TrySplitCompleteLines_WithoutNewLine_ReturnsFalse()
    {
      Assert.That(LineSplitter.TrySplitCompleteLines("partial", out _, out int consumed), Is.False);
      Assert.That(consumed, Is.Zero);
    }

    [Test]
    public void TrySplitCompleteLines_WithEmptyLine_PreservesIt()
    {
      Assert.That(LineSplitter.TrySplitCompleteLines("\n", out var lines, out _), Is.True);
      Assert.That(lines, Is.EqualTo(new[] { string.Empty }));
    }
  }
}
