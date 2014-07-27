using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Media;

namespace PartialView.TextAppenders
{
  internal class FilteredTextAppender : TextAppender
  {
    private readonly Brush _highLightColor;
    private readonly TextMatcher _textMatcher;

    public FilteredTextAppender(TextMatcher textMatcher, Brush highLightColor)
    {
      _highLightColor = highLightColor;
      _textMatcher = textMatcher;
    }

    protected override List<string> GetParts(string textToAdd)
    {
      var parts = new List<string>();
      var remaining = textToAdd;
      while (!string.IsNullOrEmpty(remaining))
      {
        var index = _textMatcher.GetFirstMatchStartIndex(remaining);
        int length = _textMatcher.PatternLenght;

        if (index < 0)
        {
          parts.Add(remaining);
          break;
        }

        parts.AddRange(new[]{
          Before(remaining, index),
          Between(remaining, index, index + length)}
          );

        remaining = AllAfter(remaining, index, length);
      }
      return parts;
    }

    private static string Between(string remaining, int start, int end)
    {
      return remaining.Substring(start, end - start);
    }

    private static string AllAfter(string remaining, int index, int length)
    {
      return remaining.Substring(index + length);
    }

    private static string Before(string remaining, int index)
    {
      return remaining.Substring(0, index);
    }

    protected override Run CreateRun(string part)
    {
      var run = base.CreateRun(part);
      if (_textMatcher.Match(part))
      {
        run.Background = _highLightColor;
      }
      return run;
    }
  }
}