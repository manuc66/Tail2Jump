using System.Collections.Generic;
using System.Windows.Documents;

namespace PartialView.TextAppenders
{
  internal class TextAppender
  {
    public virtual void AddText(Paragraph paragraph, string textToAdd)
    {
      if (string.IsNullOrEmpty(textToAdd)) return;
      var parts = GetParts(textToAdd);
      AddToParagraph(paragraph, parts);
    }


    protected virtual void AddToParagraph(Paragraph paragraph, List<string> parts)
    {
      foreach (var part in parts)
      {
        var run = CreateRun(part);
        run.TextDecorations = null;
        paragraph.Inlines.Add(run);
      }
    }

    protected virtual List<string> GetParts(string textToAdd)
    {
      var parts = new List<string>();
      var all = textToAdd;

      parts.Add(all);

      return parts;
    }

    protected virtual Run CreateRun(string part)
    {
      return new Run(part);
    }

  }
}