using System.Windows.Threading;

namespace Tail2Jump
{
  public interface IPartialView
  {
    double ScrollMaximum { get; set; }
    bool AutoScroll { get; set; }
    string TextSearch { get; set; }
    bool TextSearchActivated { get; set; }
    bool IgnoreCase { get; set; }
    Dispatcher Dispatcher { get; }
    void RefreshView(bool scrollDown = false);
  }
}