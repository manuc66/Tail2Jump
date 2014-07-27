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
      var all = textToAdd;
      while (!string.IsNullOrEmpty(all))
      {
        var index = _textMatcher.GetFirstMatchStartIndex(all);
        int length = _textMatcher.PatternLenght;

        if (index >= 0)
        {
          parts.Add(all.Substring(0, index));
          parts.Add(all.Substring(index, length));
          all = all.Substring(index + length);
        }
        else
        {
          parts.Add(all);
          all = string.Empty;
        }
      }
      return parts;
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