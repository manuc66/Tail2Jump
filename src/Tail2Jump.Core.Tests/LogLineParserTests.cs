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
    public void Parse_WindowsDriveLetterPath_ProducesAHyperLinkSegment()
    {
      var segments = LogLineParser.Parse(@"failure at C:\src\Program.cs:42");

      Assert.That(segments.Any(segment => segment.IsHyperLink), Is.True);
    }

    [Test]
    public void Parse_UrlWithPort_IsNotTreatedAsAFileLocation()
    {
      var segments = LogLineParser.Parse("see https://example.com:443");

      Assert.That(segments.Any(segment => segment.IsHyperLink), Is.False);
    }

    [Test]
    public void Parse_FrenchLineSuffix_ProducesAHyperLinkSegment()
    {
      var segments = LogLineParser.Parse("Erreur dans File.cs:ligne 12");

      Assert.That(segments.Any(s => s.Kind == LogLineSegmentKind.HyperLink), Is.True);
    }

    [Test]
    public void Segments_CoverTheWholeOriginalLine()
    {
      const string line = "at C:\\src\\Program.cs:42 and https://example.com:443";

      var segments = LogLineParser.Parse(line);

      Assert.That(string.Concat(segments.Select(segment => segment.Text)), Is.EqualTo(line));
    }

    [Test]
    public void Text_IsMaterializedOnceAndCached()
    {
      var segments = LogLineParser.Parse("hello Program.cs:42");
      var segment = segments[0];

      Assert.That(segment.Text, Is.SameAs(segment.Text));
      Assert.That(segment.DisplayText, Is.SameAs(segment.DisplayText));
    }

    [Test]
    public void DisplayText_ReplacesSpacesWithNonBreakingSpaces()
    {
      var segments = LogLineParser.Parse("two words");

      Assert.That(segments[0].Text, Is.EqualTo("two words"));
      Assert.That(segments[0].DisplayText, Is.EqualTo("two\u00A0words"));
    }

    [Test]
    public void SplitFileName_SplitsOnTheFirstPipe()
    {
      var (fileName, content) = LogLineParser.SplitFileName("app.log|a|b");

      Assert.That(fileName, Is.EqualTo("app.log"));
      Assert.That(content, Is.EqualTo("a|b"));
    }

    [Test]
    public void Parse_WithSearch_MarksEveryMatch()
    {
      var segments = LogLineParser.Parse("Error here, error there", "error");

      var highlighted = segments.Where(s => s.IsHighlighted).ToList();
      Assert.That(highlighted, Has.Count.EqualTo(2));
      Assert.That(string.Concat(segments.Select(s => s.Text)), Is.EqualTo("Error here, error there"));
    }

    [Test]
    public void Parse_WithSearch_IsCaseSensitiveWhenRequested()
    {
      var segments = LogLineParser.Parse("Error error", "error", ignoreCase: false);

      Assert.That(segments.Count(s => s.IsHighlighted), Is.EqualTo(1));
    }

    [Test]
    public void Parse_WithSearchOnHyperLink_KeepsTheLinkClickable()
    {
      var segments = LogLineParser.Parse("failure at Program.cs:42", "Program");

      var link = segments.Single(s => s.IsHyperLink);
      Assert.That(link.Text.Trim(), Is.EqualTo("Program.cs:42"));
      Assert.That(link.IsHighlighted, Is.False);
    }

    [Test]
    public void Parse_WithEmptySearch_DoesNotHighlightAnything()
    {
      var segments = LogLineParser.Parse("nothing to see", string.Empty);

      Assert.That(segments.Any(s => s.IsHighlighted), Is.False);
    }
  }
}
