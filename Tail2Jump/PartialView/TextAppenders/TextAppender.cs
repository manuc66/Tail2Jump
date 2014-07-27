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
        AddPart(paragraph, part);
      }
    }

    private void AddPart(Paragraph paragraph, string part)
    {
      var run = CreateRun(part);
      run.TextDecorations = null;
      paragraph.Inlines.Add(run);
    }

    protected virtual List<string> GetParts(string textToAdd)
    {
      return new List<string> { textToAdd };
    }

    protected virtual Run CreateRun(string part)
    {
      return new Run(part);
    }

  }
}