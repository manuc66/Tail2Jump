using System;
using Tail2Jump.FileIO;
using NUnit.Framework;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class TailEventDataTests
  {
    [Test]
    public void ConstructorFromText_PreSplitsTheLines()
    {
      var data = new TailEventData("app.log", "one\r\ntwo\nthree");

      Assert.That(data.Lines, Is.EqualTo(new[] { "one", "two", "three" }));
    }

    [Test]
    public void ConstructorFromLines_JoinsThemLazilyForNewLines()
    {
      var data = new TailEventData("app.log", new[] { "one", "two" });

      Assert.That(data.Lines, Is.EqualTo(new[] { "one", "two" }));
      Assert.That(data.NewLines, Is.EqualTo("one" + Environment.NewLine + "two"));
    }

    [Test]
    public void NewLines_IsTheSameInstanceOnSubsequentCalls()
    {
      var data = new TailEventData("app.log", new[] { "one", "two" });

      Assert.That(data.NewLines, Is.SameAs(data.NewLines));
    }
  }
}
