using System.Linq;
using NUnit.Framework;
using Tail2Jump.Rendering;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class LogLineParserTests
  {
    [Test]
    public void Parse_PlainLine_ReturnsASingleTextSegment()
    {
      var segments = LogLineParser.Parse("hello world");

      Assert.That(segments, Has.Count.EqualTo(1));
      Assert.That(segments[0].Kind, Is.EqualTo(LogLineSegmentKind.Text));
      Assert.That(segments[0].Text, Is.EqualTo("hello world"));
    }

    [Test]
    public void Parse_PathWithLineSuffix_ProducesAHyperLinkSegment()
    {
      var segments = LogLineParser.Parse("at Program.cs:42");

      Assert.That(segments.Any(s => s.Kind == LogLineSegmentKind.HyperLink), Is.True);
      Assert.That(segments.Last().Text.Trim(), Is.EqualTo("Program.cs:42"));
    }

    [Test]
    public void Parse_FrenchLineSuffix_ProducesAHyperLinkSegment()
    {
      var segments = LogLineParser.Parse("Erreur dans File.cs:ligne 12");

      Assert.That(segments.Any(s => s.Kind == LogLineSegmentKind.HyperLink), Is.True);
    }

    [Test]
    public void SplitFileName_SplitsOnTheFirstPipe()
    {
      var (fileName, content) = LogLineParser.SplitFileName("app.log|a|b");

      Assert.That(fileName, Is.EqualTo("app.log"));
      Assert.That(content, Is.EqualTo("a|b"));
    }
  }
}
