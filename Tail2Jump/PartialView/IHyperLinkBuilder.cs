using System.Windows.Documents;
namespace Tail2Jump
{
  public interface IHyperLinkBuilder
  {
    Hyperlink GetHyperLink(string linkText);
  }
}
