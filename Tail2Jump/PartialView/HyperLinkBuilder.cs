using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Be.Manuc66.Library.DevTools;
using log4net;
using Microsoft.Practices.Prism.PubSubEvents;
using Tai2JumpInfra.Events;

namespace PartialView
{
  public class HyperLinkBuilder : IHyperLinkBuilder
  {
    private static readonly ILog Log = LogManager.GetLogger(typeof(HyperLinkBuilder));
    readonly IVSTools _vsTools;
    private IOpenWithInfo OpenInVS { get; set; }

    public HyperLinkBuilder(IEventAggregator eventAggregator, IVSTools vsTools)
    {
      eventAggregator.GetEvent<OpenInChanged>().Subscribe(OpenInChangedEventHandler);
      _vsTools = vsTools;
    }

    private void OpenInChangedEventHandler(IOpenWithInfo obj)
    {
      OpenInVS = obj;
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
    private void textLink_MouseEnter(object sender, EventArgs e)
    {
      var myHyperlink = sender as Hyperlink;
      if (myHyperlink != null)
        myHyperlink.TextDecorations = TextDecorations.Underline;
    }

    // Remove the underline on the MouseLeave event.
    private void textLink_MouseLeave(object sender, EventArgs e)
    {
      var myHyperlink = sender as Hyperlink;
      if (myHyperlink != null)
        myHyperlink.TextDecorations = null;
    }

    private void textLink_Unloaded(object sender, RoutedEventArgs e)
    {
      var hyperlink = sender as Hyperlink;
      if (hyperlink == null) return;
      hyperlink.Unloaded -= textLink_Unloaded;
      hyperlink.Click -= textLink_Click;
      hyperlink.MouseEnter -= textLink_MouseEnter;
      hyperlink.MouseLeave -= textLink_MouseLeave;
    }

    private void textLink_Click(object sender, RoutedEventArgs e)
    {
      var sender1 = sender as Hyperlink;
      if (sender1 == null)
      {
        throw new ArgumentException("Sender should be an Hyperlink!");
      }
      if (OpenInVS == null)
      {
        Log.Info("No VS to open in");
      }
      else
      {
        var hyperlink = sender1;
        var originalString = ((Run)hyperlink.Inlines.FirstInline).Text;
        NavigateTo(originalString);
        e.Handled = true;
      }
    }

    private void NavigateTo(string originalString)
    {
      var filePathWithLineInfo = _vsTools.GetFilePathWithLineNumber(originalString);
      OpenInVS.OpenFileAtLine(filePathWithLineInfo);
    }
  }
}