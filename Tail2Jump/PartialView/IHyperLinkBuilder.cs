using System.Windows.Documents;

namespace PartialView
{
  public interface IHyperLinkBuilder
  {
    Hyperlink GetHyperLink(string linkText);
  }
}
