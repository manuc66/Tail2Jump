using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tail2Jump.Rendering
{
  public enum LogLineSegmentKind
  {
    Text,
    HyperLink
  }

  public class LogLineSegment
  {
    public LogLineSegment(LogLineSegmentKind kind, string text)
    {
      Kind = kind;
      Text = text;
    }

    public LogLineSegmentKind Kind { get; }
    public string Text { get; }

    /// <summary>Text with non-breaking spaces, so the UI keeps the indentation.</summary>
    public string DisplayText => Text?.Replace(' ', '\u00A0');

    public bool IsHyperLink => Kind == LogLineSegmentKind.HyperLink;
  }

  /// <summary>
  /// Splits a log line into plain text and hyperlink segments.
  /// A segment is a hyperlink when it looks like "File.cs:12" or
  /// "File.cs:line 12" (and the French ":ligne" variant).
  /// Ported from the WPF PartialView.
  /// </summary>
  public static class LogLineParser
  {
    private const string LineFr = ":ligne";
    private const string LineEn = ":line";
    private const string Space = " ";

    public static IReadOnlyList<LogLineSegment> Parse(string line)
    {
      var segments = new List<LogLineSegment>();
      if (line == null) return segments;

      var textToAdd = new StringBuilder();
      string[] words = line.Split(new[] { Space }, System.StringSplitOptions.None);

      for (int i = 0; i < words.Length; i++)
      {
        string word = words[i];
        bool isLineKeyword = word.EndsWith(LineFr, System.StringComparison.Ordinal) ||
                             word.EndsWith(LineEn, System.StringComparison.Ordinal);

        if (isLineKeyword)
        {
          // Include the line number when it is the next token: "File.cs:line 12".
          string linkText = word;
          if (i + 1 < words.Length && int.TryParse(words[i + 1], out _))
          {
            linkText += Space + words[++i];
          }

          FlushText(segments, textToAdd);
          segments.Add(new LogLineSegment(LogLineSegmentKind.HyperLink, linkText));
          continue;
        }

        if (EndsWithLineNumber(word))
        {
          FlushText(segments, textToAdd);
          segments.Add(new LogLineSegment(LogLineSegmentKind.HyperLink, word));
          continue;
        }

        textToAdd.Append(word);
        if (i < words.Length - 1)
        {
          textToAdd.Append(Space);
        }
      }

      FlushText(segments, textToAdd);
      return segments;
    }

    private static void FlushText(ICollection<LogLineSegment> segments, StringBuilder textToAdd)
    {
      if (textToAdd.Length == 0) return;

      segments.Add(new LogLineSegment(LogLineSegmentKind.Text, textToAdd.ToString()));
      textToAdd.Clear();
    }

    private static bool EndsWithLineNumber(string word)
    {
      int colon = word.LastIndexOf(':');
      if (colon <= 0 || colon >= word.Length - 1) return false;

      var pathPart = word.Substring(0, colon);
      // Avoid treating timestamps such as "23:50:01" as file locations.
      if (!pathPart.Contains('.') || pathPart.All(char.IsDigit) || pathPart.Contains(':')) return false;

      var lineText = word.Substring(colon + 1);
      return int.TryParse(lineText, out int lineNumber) && lineNumber > 0;
    }

    /// <summary>
    /// Splits a "file|content" cache line into its file name and content.
    /// </summary>
    public static (string FileName, string Content) SplitFileName(string line)
    {
      if (line == null) return (string.Empty, string.Empty);

      int index = line.IndexOf('|');
      return index > 0
        ? (line.Substring(0, index), line.Substring(index + 1))
        : (string.Empty, line);
    }
  }
}
