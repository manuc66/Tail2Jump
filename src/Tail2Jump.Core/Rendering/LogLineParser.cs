using System;
using System.Collections.Generic;

namespace Tail2Jump.Rendering
{
  public enum LogLineSegmentKind
  {
    Text,
    HyperLink
  }

  /// <summary>
  /// A slice of a log line. The text is kept as a memory slice so parsing a
  /// line does not allocate one string per word; strings are only materialized
  /// for the segments actually bound by the UI (and then cached).
  /// </summary>
  public class LogLineSegment
  {
    private readonly ReadOnlyMemory<char> _content;
    private string? _text;
    private string? _displayText;

    public LogLineSegment(LogLineSegmentKind kind, ReadOnlyMemory<char> content, bool isHighlighted = false)
    {
      Kind = kind;
      _content = content;
      IsHighlighted = isHighlighted;
    }

    public LogLineSegmentKind Kind { get; }

    public ReadOnlyMemory<char> Content => _content;

    public bool IsHyperLink => Kind == LogLineSegmentKind.HyperLink;

    public bool IsHighlighted { get; }

    /// <summary>Raw text of the segment (allocated on first access).</summary>
    public string Text => _text ??= _content.ToString();

    /// <summary>
    /// Text with non-breaking spaces, so the UI keeps the indentation.
    /// Allocated on first access and cached.
    /// </summary>
    public string DisplayText => _displayText ??= CreateDisplayText(_content);

    private static string CreateDisplayText(ReadOnlyMemory<char> content)
    {
      return string.Create(content.Length, content, static (destination, source) =>
      {
        ReadOnlySpan<char> span = source.Span;

        for (int i = 0; i < span.Length; i++)
        {
          destination[i] = span[i] == ' ' ? '\u00A0' : span[i];
        }
      });
    }
  }

  /// <summary>
  /// Splits a log line into plain text and hyperlink segments. A segment is a
  /// hyperlink when it looks like "File.cs:12", "File.cs:line 12" or
  /// "File.cs:ligne 12".
  /// </summary>
  public static class LogLineParser
  {
    private const string LineFr = ":ligne";
    private const string LineEn = ":line";
    private const char Space = ' ';
    private const char Colon = ':';

    public static IReadOnlyList<LogLineSegment> Parse(string? line)
    {
      return ParseCore(line, null, StringComparison.Ordinal);
    }

    public static IReadOnlyList<LogLineSegment> Parse(string? line, string? searchText, bool ignoreCase = true)
    {
      if (string.IsNullOrEmpty(searchText)) return Parse(line);

      var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
      return ParseCore(line, searchText, comparison);
    }

    private static IReadOnlyList<LogLineSegment> ParseCore(string? line, string? searchText,
      StringComparison comparison)
    {
      var segments = new List<LogLineSegment>();
      if (string.IsNullOrEmpty(line)) return segments;

      int index = 0;
      int textStart = 0;

      while (index < line.Length)
      {
        int tokenEnd = line.IndexOf(Space, index);
        if (tokenEnd < 0) tokenEnd = line.Length;

        ReadOnlySpan<char> token = line.AsSpan(index, tokenEnd - index);

        bool isLink = IsFileLocationToken(token);
        int linkEnd = tokenEnd;

        if (!isLink && (token.EndsWith(LineFr, StringComparison.Ordinal) ||
                        token.EndsWith(LineEn, StringComparison.Ordinal)))
        {
          // "File.cs:line 12": include the line number when it is the next token.
          int nextStart = tokenEnd + 1;
          if (nextStart < line.Length)
          {
            int nextEnd = line.IndexOf(Space, nextStart);
            if (nextEnd < 0) nextEnd = line.Length;

            if (int.TryParse(line.AsSpan(nextStart, nextEnd - nextStart), out _))
            {
              linkEnd = nextEnd;
              isLink = true;
            }
          }
        }

        if (isLink)
        {
          AddText(segments, line.AsMemory(textStart, index - textStart), searchText, comparison);
          segments.Add(new LogLineSegment(LogLineSegmentKind.HyperLink,
            line.AsMemory(index, linkEnd - index)));

          textStart = linkEnd;
          index = linkEnd;
          continue;
        }

        index = tokenEnd + 1;
      }

      AddText(segments, line.AsMemory(textStart), searchText, comparison);
      return segments;
    }

    private static void AddText(ICollection<LogLineSegment> segments, ReadOnlyMemory<char> text,
      string? searchText, StringComparison comparison)
    {
      if (text.Length == 0) return;

      if (searchText == null)
      {
        segments.Add(new LogLineSegment(LogLineSegmentKind.Text, text));
        return;
      }

      ReadOnlySpan<char> span = text.Span;
      ReadOnlySpan<char> pattern = searchText.AsSpan();
      int index = 0;

      while (index < span.Length)
      {
        int found = span[index..].IndexOf(pattern, comparison);
        if (found < 0)
        {
          segments.Add(new LogLineSegment(LogLineSegmentKind.Text, text[index..]));
          break;
        }

        found += index;

        if (found > index)
        {
          segments.Add(new LogLineSegment(LogLineSegmentKind.Text, text[index..found]));
        }

        segments.Add(new LogLineSegment(LogLineSegmentKind.Text, text.Slice(found, pattern.Length), true));
        index = found + pattern.Length;
      }
    }

    private static bool IsFileLocationToken(ReadOnlySpan<char> token)
    {
      int colon = token.LastIndexOf(Colon);
      if (colon <= 0 || colon >= token.Length - 1) return false;

      ReadOnlySpan<char> pathPart = token[..colon];

      if (pathPart.IndexOf('.') < 0) return false;
      if (pathPart.IndexOf("://".AsSpan()) >= 0) return false;
      if (IsAllDigits(pathPart)) return false;

      return int.TryParse(token[(colon + 1)..], out int lineNumber) && lineNumber > 0;
    }

    private static bool IsAllDigits(ReadOnlySpan<char> value)
    {
      if (value.Length == 0) return false;

      foreach (char character in value)
      {
        if (!char.IsDigit(character)) return false;
      }

      return true;
    }

  }
}
