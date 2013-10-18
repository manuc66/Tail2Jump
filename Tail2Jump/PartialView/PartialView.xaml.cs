using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Be.Manuc66.Library;
using Be.Manuc66.Library.FileIO;
using Be.Manuc66.Library.General;
using log4net;
using Microsoft.Practices.Prism.Events;
using Tai2JumpInfra.Events;

namespace PartialView
{
  /// <summary>
  /// Interaction logic for PartialView.xaml
  /// </summary>
  public partial class PartialView : IPartialView
  {
    #region Properties
    public double ScrollMaximum
    {
      get
      {
        return _scrollMaximum;
      }
      set
      {
        if (_scrollMaximum != value)
        {
          _scrollMaximum = value;
          OnPropertyChanged("ScrollMaximum");
        }
      }
    }
    public bool AutoScroll { get; set; }


    #region TextSearch
    public string TextSearch
    {
      get { return (string)GetValue(TextSearchProperty); }
      set { SetValue(TextSearchProperty, value); }
    }
    public static readonly DependencyProperty TextSearchProperty = DependencyProperty.Register(
      "TextSearch", typeof(string), typeof(PartialView), new PropertyMetadata(string.Empty, TextSearchPropertyChangedCallback));
    #endregion // TextSearch
    #region TextSearchActivated
    public bool TextSearchActivated
    {
      get { return (bool)GetValue(TextSearchActivatedProperty); }
      set { SetValue(TextSearchActivatedProperty, value); }
    }
    public static readonly DependencyProperty TextSearchActivatedProperty = DependencyProperty.Register(
      "TextSearchActivated", typeof(bool), typeof(PartialView), new PropertyMetadata(false));
    #endregion // TextSearch
    #region IgnoreCase
    public bool IgnoreCase
    {
      get { return (bool)GetValue(IgnoreCaseProperty); }
      set { SetValue(IgnoreCaseProperty, value); }
    }
    public static readonly DependencyProperty IgnoreCaseProperty = DependencyProperty.Register(
      "IgnoreCase", typeof(bool), typeof(PartialView), new PropertyMetadata(true, TextSearchPropertyChangedCallback));
    #endregion // IgnoreCase


    #endregion

    #region Variables
    private int _curentStart;
    readonly Paragraph _paragraph = new Paragraph();
    private readonly double _charWidth;
    private readonly double _charHeight;
    private ScrollBar _txtBoxHScroll;
    private double _viewPortSize;
    private double _scrollMaximum;
    private static readonly ILog Log = LogManager.GetLogger(typeof(PartialView));
    private readonly object _refreshViewLock = new object();
    private readonly SolidColorBrush _highLightColor = Brushes.Yellow;
    private readonly TemporisedEvent<IEmpty> _autoScroll;
    const string LineFr = ":ligne";
    const string LineEn = ":line";
    #endregion

    public event PropertyChangedEventHandler PropertyChanged;
    private readonly ITailLineCache _fileAcessor;
    private readonly IHyperLinkBuilder _hyperLinkBuilder;

    public PartialView(ITailLineCache fileAcessor, IEventAggregator eventAggregator, IHyperLinkBuilder hyperLinkBuilder)
    {
      if (fileAcessor == null) throw new ArgumentNullException("fileAcessor");
      InitializeComponent();
      _fileAcessor = fileAcessor;
      DataContext = this;

      eventAggregator.GetEvent<SearchInfoChanged>().Subscribe(data => { TextSearch = data; RefreshView(); });
      eventAggregator.GetEvent<SearchActivationStatusChanged>().Subscribe(activation => { TextSearchActivated = activation; });

      FlowDocument doc = TxtBoxTailContent.Document;
      doc.Blocks.Add(_paragraph);


      var formattedText = new FormattedText("M" + Environment.NewLine,
                  CultureInfo.GetCultureInfo("en-us"),
                  FlowDirection.LeftToRight,
                  new Typeface(TxtBoxTailContent.FontFamily, TxtBoxTailContent.FontStyle, TxtBoxTailContent.FontWeight, TxtBoxTailContent.FontStretch),
                  TxtBoxTailContent.FontSize,
                  Brushes.Black);
      _charWidth = formattedText.Width;
      _charHeight = formattedText.Height;
      Loaded += PartialView_Loaded;


      _autoScroll = new TemporisedEvent<IEmpty>(250);
      _autoScroll.eHandle += m_autoScroll_eHandle;
      _hyperLinkBuilder = hyperLinkBuilder;
    }

    protected void OnPropertyChanged(string name)
    {
      PropertyChangedEventHandler handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(name));
      }
    }
    static void TextSearchPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var @this = d as PartialView;
      if (@this == null)
      {
        return;
      }
      @this.RefreshView();
    }
    void PartialView_Loaded(object sender, RoutedEventArgs e)
    {
      var scrollview = TxtBoxTailContent.Template.FindName("PART_ContentHost", TxtBoxTailContent) as ScrollViewer;
      if (scrollview == null)
      {
        Debugger.Break();
      }
      else
      {
        _txtBoxHScroll = scrollview.Template.FindName("PART_HorizontalScrollBar", scrollview) as ScrollBar;

        hscroll.Maximum = 0.0;
        DependencyPropertyDescriptor dpdMax = DependencyPropertyDescriptor.FromProperty(RangeBase.MaximumProperty, typeof(ScrollBar));
        if (dpdMax != null && _txtBoxHScroll != null)
        {
          dpdMax.AddValueChanged(_txtBoxHScroll, (sender_, args) => hscroll.Maximum = _txtBoxHScroll.Maximum);
        }
        DependencyPropertyDescriptor dpdView = DependencyPropertyDescriptor.FromProperty(ScrollBar.ViewportSizeProperty, typeof(ScrollBar));
        if (dpdView != null && _txtBoxHScroll != null)
        {
          dpdView.AddValueChanged(_txtBoxHScroll, (sender_, args) => hscroll.ViewportSize = _txtBoxHScroll.ViewportSize);
        }
      }
    }

    void scrollview_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      hscroll.ViewportSize = e.ViewportWidth;
      if (_txtBoxHScroll != null)
      {
        hscroll.Value += e.HorizontalChange;
      }
    }
    private void hscroll_Scroll(object sender, ScrollEventArgs e)
    {
      if (_txtBoxHScroll != null)
      {
        TxtBoxTailContent.ScrollToHorizontalOffset(e.NewValue);
        _txtBoxHScroll.Value = e.NewValue;
      }
    }
    private void scroller_Scroll(object sender, ScrollEventArgs e)
    {
      _curentStart = (int)e.NewValue;
      RefreshView();
    }
    private void RichTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      if (e == null) return;
      scroller.Value -= e.Delta / 120.0;
      _curentStart = (int)scroller.Value;
      e.Handled = true;
      RefreshView();
    }
    private void txtBox_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      ComputeViewPortSize();
      RefreshVerticalScrollBarMaximum();

      RefreshView();
    }

    private void ComputeViewPortSize()
    {
      var sz = (TxtBoxTailContent.ActualHeight / _charHeight) - 0.5;
      _viewPortSize = (int)sz;
    }

    private void RefreshVerticalScrollBarMaximum()
    {
      ScrollMaximum = _fileAcessor != null ? Math.Max(0, _fileAcessor.LineIndexed - _viewPortSize) : 0;
    }

    void m_autoScroll_eHandle(TemporisedEvent<IEmpty> sender, IEmpty data)
    {
      if (AutoScroll)
      {
        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => RefreshView(true)));
      }
    }
    public void RefreshView(bool scrollDown = false)
    {
      if (_fileAcessor == null)
        return;

      RefreshVerticalScrollBarMaximum();

      lock (_refreshViewLock)
      {
        var viewSize = (int)_viewPortSize;
        if (viewSize == 0)
        {
        }
        else
        {

        }
        RefreshScrollMaximum(viewSize);

        RefreshCurrentStart(scrollDown);
        var toDisplay = _fileAcessor.GetLines(_curentStart, viewSize);

        FillTextView(toDisplay);
      }
    }

    private void RefreshScrollMaximum(int viewSize)
    {
      scroller.Maximum = _fileAcessor.LineIndexed - viewSize;
    }

    private void RefreshCurrentStart(bool scrollDown)
    {
      int currentStart = ComputeCurrentStart(scrollDown);
      _curentStart = currentStart;
      scroller.Value = _curentStart;
    }

    private int ComputeCurrentStart(bool scrollDown)
    {
      int currentStart;
      if (scrollDown && AutoScroll)
      {
        currentStart = Math.Max(_fileAcessor.LineIndexed - (int)_viewPortSize, 0);
      }
      else
      {
        currentStart = Math.Min(_fileAcessor.LineIndexed - (int)_viewPortSize, _curentStart);
        if (AutoScroll && Mouse.LeftButton != MouseButtonState.Pressed)
        {
          _autoScroll.Set(Empty.Instance);
        }
      }
      if (currentStart < 0)
      {
        currentStart = 0;
      }
      return currentStart;
    }

    private void FillTextView(IList<string> toDisplay)
    {
      var doc = ResetDocument();
      var paragraph = CreateParagraph(doc);

      FillParagraph(toDisplay, paragraph);

      RefreshDocumentWidth(doc, toDisplay);
    }

    private void FillParagraph(IEnumerable<string> toDisplay, Paragraph paragraph)
    {
      foreach (var line in toDisplay)
      {
        if (line == null)
        {
          Log.Debug("bug here!");
          continue;
        }
        WriteLine(line, paragraph);
      }
    }

    private static Paragraph CreateParagraph(FlowDocument doc)
    {
      var paragraph = new Paragraph { KeepTogether = true, KeepWithNext = true };
      doc.Blocks.Add(paragraph);
      return paragraph;
    }

    private void WriteLine(string line, Paragraph paragraph)
    {
      var textToAdd = new StringBuilder();
      string[] words = line.Split(new[] { " " }, StringSplitOptions.None);
      for (int i = 0; i < words.Length; i++)
      {
        string word = words[i];
        if ((word.EndsWith(LineFr) || word.EndsWith(LineEn)) && i < words.Length - 1)
        {
          AddText(paragraph, textToAdd);
          string linkText = word + " " + words[i + 1];
          var textLink = _hyperLinkBuilder.GetHyperLink(linkText);
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

      textToAdd.Append(Environment.NewLine);
      AddText(paragraph, textToAdd);
    }

    private FlowDocument ResetDocument()
    {
      TxtBoxTailContent.Document.Blocks.Clear();
      var doc = TxtBoxTailContent.Document = new FlowDocument();
      return doc;
    }

    private void RefreshDocumentWidth(FlowDocument doc, IEnumerable<string> toDisplay)
    {
      int maxLineLenght = toDisplay.Aggregate(0, (currentMaxLenght, line) => Math.Max(currentMaxLenght, line.Length));
      double sizeW = maxLineLenght * _charWidth + 10.0;
      if ((double.IsNaN(doc.PageWidth) || doc.PageWidth < sizeW) && !double.IsInfinity(sizeW) && !double.IsNaN(sizeW))
      {
        doc.PageWidth = sizeW;
      }
    }

    private void AddText(Paragraph paragraph, StringBuilder textToAdd)
    {
      if (textToAdd.Length != 0)
      {
        var parts = new List<string>();
        var all = textToAdd.ToString();
        if (string.IsNullOrEmpty(TextSearch) || !TextSearchActivated)
        {
          parts.Add(all);
        }
        else
        {
          while (!string.IsNullOrEmpty(all))
          {
            var index = IgnoreCase ? all.ToLower().IndexOf(TextSearch.ToLower(), StringComparison.Ordinal) : all.IndexOf(TextSearch, StringComparison.Ordinal);
            if (index >= 0)
            {
              parts.Add(all.Substring(0, index));
              parts.Add(all.Substring(index, TextSearch.Length));
              all = all.Substring(index + TextSearch.Length);
            }
            else
            {
              parts.Add(all);
              all = string.Empty;
            }
          }
        }
        foreach (var part in parts)
        {
          var run = new Run(part);
          if (!string.IsNullOrEmpty(TextSearch) && ((!IgnoreCase && part.Equals(TextSearch)) || part.ToLower().Equals(TextSearch.ToLower())))
          {
            run.Background = _highLightColor;
          }
          run.TextDecorations = null;
          paragraph.Inlines.Add(run);
          textToAdd.Length = 0;
        }
      }
    }

    private void TxtBoxTailContent_OnLayoutUpdated(object sender, EventArgs e)
    {
    }

    private void TxtBoxTailContent_Initialized(object sender, EventArgs e)
    {
      ComputeViewPortSize();

    }
  }
}
