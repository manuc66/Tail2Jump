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
using Be.Manuc66.Library.FileIO;
using Be.Manuc66.Library.Threading;
using log4net;
using Microsoft.Practices.Prism.PubSubEvents;
using Tai2JumpInfra.Events;

namespace PartialView
{
  /// <summary>
  /// Interaction logic for PartialView.xaml
  /// </summary>
  public partial class PartialView : IPartialView
  {
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


    public string TextSearch
    {
      get { return (string)GetValue(TextSearchProperty); }
      set { SetValue(TextSearchProperty, value); }
    }
    public static readonly DependencyProperty TextSearchProperty = DependencyProperty.Register(
      "TextSearch", typeof(string), typeof(PartialView), new PropertyMetadata(string.Empty, TextSearchPropertyChangedCallback));

    public bool TextSearchActivated
    {
      get { return (bool)GetValue(TextSearchActivatedProperty); }
      set { SetValue(TextSearchActivatedProperty, value); }
    }
    public static readonly DependencyProperty TextSearchActivatedProperty = DependencyProperty.Register(
      "TextSearchActivated", typeof(bool), typeof(PartialView), new PropertyMetadata(false));

    public bool IgnoreCase
    {
      get { return (bool)GetValue(IgnoreCaseProperty); }
      set { SetValue(IgnoreCaseProperty, value); }
    }
    public static readonly DependencyProperty IgnoreCaseProperty = DependencyProperty.Register(
      "IgnoreCase", typeof(bool), typeof(PartialView), new PropertyMetadata(true, TextSearchPropertyChangedCallback));


    private int _curentStart;
    private readonly CharSize _charSize;
    private ScrollBar _txtBoxHScroll;
    private double _viewPortSize;
    private double _scrollMaximum;
    private static readonly ILog Log = LogManager.GetLogger(typeof(PartialView));
    private readonly SolidColorBrush _highLightColor = Brushes.Yellow;
    private readonly TemporisedEvent _autoScroll;
    const string LINE_FR = ":ligne";
    const string LINE_EN = ":line";

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

      AddParagraph(TxtBoxTailContent);

      _charSize = GetCharSize();
      Loaded += PartialView_Loaded;


      _autoScroll = new TemporisedEvent(250);
      _autoScroll.eHandle += m_autoScroll_eHandle;
      _hyperLinkBuilder = hyperLinkBuilder;
    }

    private static void AddParagraph(RichTextBox txtBoxTailContent)
    {
      FlowDocument doc = txtBoxTailContent.Document;
      doc.Blocks.Add(new Paragraph());
    }

    private CharSize GetCharSize()
    {
      var formattedText = new FormattedText("M" + Environment.NewLine,
        CultureInfo.GetCultureInfo("en-us"),
        FlowDirection.LeftToRight,
        new Typeface(TxtBoxTailContent.FontFamily, TxtBoxTailContent.FontStyle, TxtBoxTailContent.FontWeight,
          TxtBoxTailContent.FontStretch),
        TxtBoxTailContent.FontSize,
        Brushes.Black);
      var charSize = new CharSize(formattedText.Width, formattedText.Height);
      return charSize;
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

        if (_txtBoxHScroll == null) return;

        hscroll.Maximum = 0.0;

        WatchMaximumScroll();

        WatchViewportSize();
      }
    }

    private void WatchViewportSize()
    {
      var dpdView = DependencyPropertyDescriptor.FromProperty(ScrollBar.ViewportSizeProperty, typeof(ScrollBar));
      if (dpdView == null) return;
      dpdView.AddValueChanged(_txtBoxHScroll, UpdateHorizViewportSize);
    }

    private void WatchMaximumScroll()
    {
      var dpdMax = DependencyPropertyDescriptor.FromProperty(RangeBase.MaximumProperty, typeof(ScrollBar));
      if (dpdMax == null) return;
      dpdMax.AddValueChanged(_txtBoxHScroll, UpdateHorizScrollMaximum);
    }

    private void UpdateHorizViewportSize(object sender, EventArgs args)
    {
      hscroll.ViewportSize = _txtBoxHScroll.ViewportSize;
    }

    private void UpdateHorizScrollMaximum(object sender, EventArgs args)
    {
      hscroll.Maximum = _txtBoxHScroll.Maximum;
    }

    void scrollview_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      hscroll.ViewportSize = e.ViewportWidth;
      if (_txtBoxHScroll == null) return;
      hscroll.Value += e.HorizontalChange;
    }
    private void hscroll_Scroll(object sender, ScrollEventArgs e)
    {
      if (_txtBoxHScroll == null) return;
      TxtBoxTailContent.ScrollToHorizontalOffset(e.NewValue);
      _txtBoxHScroll.Value = e.NewValue;
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
      var sz = (TxtBoxTailContent.ActualHeight / _charSize.Height) - 0.5;
      _viewPortSize = (int)sz;
    }

    private void RefreshVerticalScrollBarMaximum()
    {
      ScrollMaximum = _fileAcessor != null ? Math.Max(0, _fileAcessor.LineIndexed - _viewPortSize) : 0;
    }

    void m_autoScroll_eHandle(object sender, EventArgs data)
    {
      if (!AutoScroll) return;
      Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(RefreshViewAndScrollDown));
    }

    public void RefreshView()
    {
      RefreshView(ComputeCurrentStart());
    }

    public void RefreshViewAndScrollDown()
    {
      RefreshView(ComputeCurrentStartScrollDown());
    }

    private void RefreshView(int curentStart)
    {
      if (_fileAcessor == null)
        return;

      RefreshVerticalScrollBarMaximum();

      var viewSize = (int)_viewPortSize;
      RefreshScrollMaximum(viewSize);

      _curentStart = curentStart;
      scroller.Value = _curentStart;
      var toDisplay = _fileAcessor.GetLines(_curentStart, viewSize);

      FillTextView(toDisplay);
    }

    private void RefreshScrollMaximum(int viewSize)
    {
      scroller.Maximum = _fileAcessor.LineIndexed - viewSize;
    }

    private int ComputeCurrentStart()
    {
      int currentStart = Math.Min(_fileAcessor.LineIndexed - (int)_viewPortSize, _curentStart);
      if (AutoScroll && Mouse.LeftButton != MouseButtonState.Pressed)
      {
        _autoScroll.Set();
      }
      return Math.Max(currentStart, 0);
    }

    private int ComputeCurrentStartScrollDown()
    {
      int currentStart;
      if (true)
      {
        currentStart = Math.Max(_fileAcessor.LineIndexed - (int)_viewPortSize, 0);
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
        if ((word.EndsWith(LINE_FR) || word.EndsWith(LINE_EN)) && i < words.Length - 1)
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
      double sizeW = maxLineLenght * _charSize.Width + 10.0;
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

    private void TxtBoxTailContent_Initialized(object sender, EventArgs e)
    {
      ComputeViewPortSize();

    }
  }
}
