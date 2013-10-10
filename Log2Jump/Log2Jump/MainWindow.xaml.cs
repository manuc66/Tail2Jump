using System;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Be.Manuc66.Library;
using Be.Manuc66.Library.DevTools;
using Be.Manuc66.Library.FileIO;
using log4net;

namespace WpfTest
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    //public ICommand MouseDownCommand { get; private set; }
    //public 

    public double ScrollMaximum
    {
      get
      {
        return m_ScrollMaximum;
      }
      set
      {
        if (m_ScrollMaximum != value)
        {
          m_ScrollMaximum = value;
          OnPropertyChanged("ScrollMaximum");
        }
      }
    }
    public double ViewPortSize
    {
      get
      {
        return m_ViewPortSize;
      }
      set
      {
        if (m_ViewPortSize != value)
        {
          m_ViewPortSize = value;
          OnPropertyChanged("ViewPortSize");
        }
      }
    }
    public IOpenWithInfo OpenInVS
    {
      get { return m_OpenInVS; }
      set
      {
        if (m_OpenInVS != value)
        {
          m_OpenInVS = value;
          OnPropertyChanged("OpenInVS");
        }
      }
    }
    public ThreadSafeObservableCollection<IOpenWithInfo> VSList
    {
      get
      {
        return m_VSList;
      }
      set
      {
        if (m_VSList != value)
        {
          m_VSList = value;
          OnPropertyChanged("VSList");
        }
      }
    }

    private double m_ViewPortSize;
    private double m_ScrollMaximum;
    private RandomLineFileAccessor m_fileAcessor;
    private double m_charSize;
    private int m_curentStart = 0;
    private ScrollBar m_txtBoxHScroll;
    private IOpenWithInfo m_OpenInVS;
    private ILog m_log = log4net.LogManager.GetLogger(typeof(MainWindow));
    private object m_VSListLock = new object();
    private ThreadSafeObservableCollection<IOpenWithInfo> m_VSList;

    public MainWindow(VSTools vsTools)
    {

      InitializeComponent();

      //MouseDownCommand = new SimpleCommand
      //{
      //  ExecuteDelegate = x => DoMouseDown(),
      //  CanExecuteDelegate = x => true
      //};
      m_vsTools = vsTools;

      DataContext = this;
      // m_fileAcessor = new RandomLineFileAccessor(@"C:\Workspace\Demo\Logic\PlayerService\PlayerService\Player.cs");
      m_fileAcessor = new RandomLineFileAccessor(@"C:\Logs\Demo\service3.log");

      m_fileAcessor.IndexFile();
      ScrollMaximum = m_fileAcessor.LineIndexed;

      FormattedText formattedText = new FormattedText("M",
                  CultureInfo.GetCultureInfo("en-us"),
                  FlowDirection.LeftToRight,
                  new Typeface(txtBox.FontFamily, txtBox.FontStyle, txtBox.FontWeight, txtBox.FontStretch),
                  txtBox.FontSize,
                  Brushes.Black);
      m_charSize = formattedText.Width;

      m_VSList = new ThreadSafeObservableCollection<IOpenWithInfo>();
      this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
      Activated += new EventHandler(MainWindow_Activated);

    }

    void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {


      var scrollview = txtBox.Template.FindName("PART_ContentHost", txtBox) as ScrollViewer;
      m_txtBoxHScroll = scrollview.Template.FindName("PART_HorizontalScrollBar", scrollview) as ScrollBar;

      if (m_txtBoxHScroll != null)
      {
        hscroll.Maximum = m_txtBoxHScroll.Maximum;
        hscroll.ViewportSize = m_txtBoxHScroll.ViewportSize;
      }
    }


    // Create the OnPropertyChanged method to raise the event
    protected void OnPropertyChanged(string name)
    {
      PropertyChangedEventHandler handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(name));
      }
    }
    public event PropertyChangedEventHandler PropertyChanged;
    private readonly VSTools m_vsTools;

    private void RichTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      scroller.Value -= e.Delta / 120;
      m_curentStart = (int)scroller.Value;
      e.Handled = true;
      RefreshView();
    }

    private void txtBox_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      double normal = e.NewSize.Height / 19;

      FormattedText formattedText = new FormattedText("M" + Environment.NewLine,
                  CultureInfo.GetCultureInfo("en-us"),
                  FlowDirection.LeftToRight,
                  new Typeface(txtBox.FontFamily, txtBox.FontStyle, txtBox.FontWeight, txtBox.FontStretch),
                  txtBox.FontSize,
                  Brushes.Black);
      double calculated = formattedText.Height;

      ViewPortSize = (int)(e.NewSize.Height / calculated);
      ScrollMaximum = m_fileAcessor.LineIndexed - ViewPortSize - 1;

      RefreshView();
      refreHs();
    }

    private void scroller_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
    {
      m_curentStart = (int)e.NewValue;
      RefreshView();
    }

    private void RefreshView()
    {
      string[] toDisplay = m_fileAcessor.GetLines(m_curentStart, (int)ViewPortSize);
      txtBox.Document.Blocks.Clear();
      var doc = txtBox.Document = new FlowDocument();
      var paragraph = new Paragraph();
      paragraph.KeepTogether = true;
      paragraph.KeepWithNext = true;
      StringBuilder textToAdd = new StringBuilder();
      txtBox.Document.Blocks.Add(paragraph);
      foreach (var line in toDisplay)
      {
        string[] words = line.Split(new string[] { " " }, StringSplitOptions.None);
        for (int i = 0; i < words.Length; i++)
        {
          string word = words[i];
          if ((word.EndsWith(":ligne") || word.EndsWith(":line")) && i < words.Length - 1)
          {
            AddText(paragraph, textToAdd);
            string linkText = word + " " + words[i + 1];
            Hyperlink textLink = new Hyperlink(new Run(linkText));

            textLink.TextDecorations = null;
            textLink.NavigateUri = new Uri(linkText);
            textLink.MouseRightButtonDown += new MouseButtonEventHandler(textLink_MouseRightButtonDown);
            textLink.RequestNavigate += new RequestNavigateEventHandler(DoOpenInVS);
            textLink.Unloaded += new RoutedEventHandler(textLink_Unloaded);
            textLink.MouseEnter += new MouseEventHandler(textLink_MouseEnter);
            textLink.MouseLeave += new MouseEventHandler(textLink_MouseLeave);
            paragraph.Inlines.Add(textLink);
            ++i;
          }
          else
          {
            textToAdd.Append(word);
          }
          if (i < words.Length - 1)
          {
            textToAdd.Append(" ");
          }
        }

        AddText(paragraph, textToAdd);
        //  textToAdd.Append(Environment.NewLine);
        //paragraph.Inlines.Add(new Run(line));
        double sizeW = line.Length * m_charSize + 10.0;
        if ((double.IsNaN(doc.PageWidth) || doc.PageWidth < sizeW) && !double.IsInfinity(sizeW) && !double.IsNaN(sizeW))
        {
          doc.PageWidth = sizeW;
        }
      }
      refreHs();
    }

    private static void AddText(Paragraph paragraph, StringBuilder textToAdd)
    {
      if (textToAdd.Length != 0)
      {
        Run run = new Run(textToAdd.ToString());
        run.TextDecorations = null;
        paragraph.Inlines.Add(run);
        textToAdd.Length = 0;
      }
    }

    private void refreHs()
    {
      if (m_txtBoxHScroll != null)
      {
        hscroll.Maximum = m_txtBoxHScroll.Maximum;
        hscroll.ViewportSize = m_txtBoxHScroll.ViewportSize;
      }
    }

    private void hscroll_Scroll(object sender, ScrollEventArgs e)
    {
      if (m_txtBoxHScroll != null)
      {
        txtBox.ScrollToHorizontalOffset(e.NewValue);
        // m_txtBoxHScroll.Value = e.NewValue;
      }
    }
    // Display the underline on only the MouseEnter event.
    private void textLink_MouseEnter(object sender, EventArgs e)
    {
      Hyperlink myHyperlink = sender as Hyperlink;
      if (myHyperlink != null)
        myHyperlink.TextDecorations = TextDecorations.Underline;
    }

    // Remove the underline on the MouseLeave event.
    private void textLink_MouseLeave(object sender, EventArgs e)
    {
      Hyperlink myHyperlink = sender as Hyperlink;
      if (myHyperlink != null)
        myHyperlink.TextDecorations = null;
    }

    void textLink_Unloaded(object _sender, RoutedEventArgs e)
    {
      try
      {
        Hyperlink sender = _sender as Hyperlink;
        if (sender != null)
        {
          sender.Unloaded -= new RoutedEventHandler(textLink_Unloaded);
          sender.MouseRightButtonDown -= new MouseButtonEventHandler(textLink_MouseRightButtonDown);
          sender.RequestNavigate -= new RequestNavigateEventHandler(DoOpenInVS);
          sender.MouseEnter += new MouseEventHandler(textLink_MouseEnter);
          sender.MouseLeave += new MouseEventHandler(textLink_MouseLeave);
        }
      }
      catch (Exception Error)
      {
        m_log.Error(Error);
      }

    }

    void textLink_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      //var vsinfo = new VSTools().GetVsInfos();
      //ContextMenu RightClickMenu = new ContextMenu();
      //foreach (var vs in vsinfo.VSInDebug)
      //{
      //  MenuItem mi = new MenuItem();
      //  mi.Header = vs.SolutionPath;
      //  RightClickMenu.Items.Add(mi);
      //}
      //RightClickMenu.Items.Add(new Separator());
      //foreach (var vs in vsinfo.AllVS)
      //{
      //  MenuItem mi = new MenuItem();
      //  mi.Header = vs.SolutionPath;
      //  RightClickMenu.Items.Add(mi);
      //}
      //RightClickMenu.PlacementTarget = sender as UIElement;
      //RightClickMenu.IsOpen = true;
      //e.Handled = true;
    }
    private void DoOpenInVS(object sender, RequestNavigateEventArgs e)
    {
      try
      {
        if (sender is Hyperlink)
        {
          if (OpenInVS == null)
          {
            m_log.Info("No VS to open in");
          }
          else
          {
            Hyperlink hyperlink = (Hyperlink)sender;
            string originalString = hyperlink.NavigateUri.OriginalString;
            FilePathWithLineInfo filePathWithLineInfo = m_vsTools.GetFilePathWithLineNumber(hyperlink.NavigateUri.OriginalString);
            OpenInVS.OpenFileAtLine(filePathWithLineInfo);
          }
        }
      }
      catch (Exception Error)
      {
        m_log.Error(Error);
      }
    }

    private void MainWindow_Activated(object sender, EventArgs e)
    {
      try
      {
        ThreadPool.QueueUserWorkItem(RefreshVSList);
      }
      catch (Exception Error)
      {
        m_log.Error(Error);
      }
    }

    private void RefreshVSList(object state)
    {
      lock (m_VSListLock)
      {
        m_vsTools.RefreshVSList(VSList, ref m_OpenInVS);
      }
    }
  }
}
