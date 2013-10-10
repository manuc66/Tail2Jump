using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Be.Manuc66.Library.DevTools;
using Microsoft.Practices.Prism.Events;
using log4net;
using Tai2JumpInfra.Events;

namespace Tail2Jump
{
  public class HyperLinkBuilder : IHyperLinkBuilder
  {
    private static readonly ILog _log = LogManager.GetLogger(typeof(HyperLinkBuilder));
    readonly IVSTools m_vsTools;
    private IOpenWithInfo OpenInVS { get; set; }

    public HyperLinkBuilder(IEventAggregator eventAggregator_, IVSTools vsTools_)
    {
      eventAggregator_.GetEvent<OpenInChanged>().Subscribe(OpenInChangedEventHandler);
      m_vsTools = vsTools_;
    }

    private void OpenInChangedEventHandler(IOpenWithInfo obj_)
    {
      OpenInVS = obj_;
    }

    public Hyperlink GetHyperLink(string linkText)
    {
      var textLink = new Hyperlink(new Run(linkText)) { TextDecorations = null, Foreground = Brushes.YellowGreen };

      textLink.Click += textLink_Click;
      textLink.Unloaded += textLink_Unloaded;
      textLink.MouseEnter += textLink_MouseEnter;
      textLink.MouseLeave += textLink_MouseLeave;
      return textLink;
    }

    // Display the underline on only the MouseEnter event.
    private void textLink_MouseEnter(object sender_, EventArgs e_)
    {
      var myHyperlink = sender_ as Hyperlink;
      if (myHyperlink != null)
        myHyperlink.TextDecorations = TextDecorations.Underline;
    }

    // Remove the underline on the MouseLeave event.
    private void textLink_MouseLeave(object sender_, EventArgs e_)
    {
      var myHyperlink = sender_ as Hyperlink;
      if (myHyperlink != null)
        myHyperlink.TextDecorations = null;
    }

    private void textLink_Unloaded(object sender_, RoutedEventArgs e_)
    {
      var hyperlink = sender_ as Hyperlink;
      if (hyperlink == null) return;
      hyperlink.Unloaded -= textLink_Unloaded;
      hyperlink.Click -= textLink_Click;
      hyperlink.MouseEnter -= textLink_MouseEnter;
      hyperlink.MouseLeave -= textLink_MouseLeave;
    }

    private void textLink_Click(object sender_, RoutedEventArgs e_)
    {
      var sender1 = sender_ as Hyperlink;
      if (sender1 == null)
      {
        throw new ArgumentException("Sender should be an Hyperlink!");
      }
      if (OpenInVS == null)
      {
        _log.Info("No VS to open in");
      }
      else
      {
        var hyperlink = sender1;
        var originalString = ((Run)hyperlink.Inlines.FirstInline).Text;
        NavigateTo(originalString);
        e_.Handled = true;
      }
    }

    private void NavigateTo(string originalString_)
    {
      var filePathWithLineInfo = m_vsTools.GetFilePathWithLineNumber(originalString_);
      OpenInVS.OpenFileAtLine(filePathWithLineInfo);
    }
  }
}