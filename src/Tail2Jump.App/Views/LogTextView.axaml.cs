using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Tail2Jump.Rendering;
using Tail2Jump.ViewModels;

namespace Tail2Jump.App.Views
{
  public partial class LogTextView : UserControl
  {
    private const double BottomThreshold = 8;

    private MainWindowViewModel? _viewModel;
    private ScrollViewer? _scrollViewer;
    private bool _stickToBottom = true;
    private bool _scrollingProgrammatically;

    public LogTextView()
    {
      InitializeComponent();

      LinesList.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
      base.OnAttachedToVisualTree(e);
      Dispatcher.UIThread.Post(AttachScrollViewer, DispatcherPriority.Loaded);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
      if (_scrollViewer != null)
      {
        _scrollViewer.ScrollChanged -= OnScrollChanged;
        _scrollViewer = null;
      }

      base.OnDetachedFromVisualTree(e);
    }

    private void AttachScrollViewer()
    {
      _scrollViewer = LinesList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
      if (_scrollViewer != null)
      {
        _scrollViewer.ScrollChanged += OnScrollChanged;
      }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
      if (_viewModel != null)
      {
        _viewModel.LinesUpdated -= OnLinesUpdated;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
      }

      _viewModel = DataContext as MainWindowViewModel;

      if (_viewModel != null)
      {
        _viewModel.LinesUpdated += OnLinesUpdated;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
      }

      base.OnDataContextChanged(e);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName != nameof(MainWindowViewModel.AutoScroll) || _viewModel == null) return;
      if (!_viewModel.AutoScroll) return;

      _stickToBottom = true;
      ScrollToEnd();
    }

    private void OnLinesUpdated()
    {
      if (_viewModel == null || !_viewModel.AutoScroll || !_stickToBottom) return;
      ScrollToEnd();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
      if (_scrollViewer == null || _scrollingProgrammatically) return;

      double distanceToBottom = _scrollViewer.Extent.Height -
                                (_scrollViewer.Offset.Y + _scrollViewer.Viewport.Height);
      _stickToBottom = distanceToBottom <= BottomThreshold;
    }

    private void ScrollToEnd()
    {
      Dispatcher.UIThread.Post(() =>
      {
        if (_scrollViewer == null) return;

        _scrollingProgrammatically = true;
        try
        {
          _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X,
            Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height));
        }
        finally
        {
          _scrollingProgrammatically = false;
        }
      }, DispatcherPriority.Background);
    }

    private void OnSegmentPointerPressed(object? sender, PointerPressedEventArgs e)
    {
      if (sender is not TextBlock { DataContext: LogLineSegment segment }) return;
      if (!segment.IsHyperLink) return;
      if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed) return;

      _viewModel?.OpenAtLine(segment.Text);
      e.Handled = true;
    }

    public void CopySelectionOrAll() => CopyLines(all: false);

    public void SelectAllLines() => LinesList.Selection.SelectAll();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
      if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;

      if (e.Key == Key.C)
      {
        CopyLines(all: false);
        e.Handled = true;
      }
      else if (e.Key == Key.A)
      {
        LinesList.Selection.SelectAll();
        e.Handled = true;
      }
    }

    private void OnCopyClick(object? sender, RoutedEventArgs e) => CopyLines(all: false);

    private void OnCopyAllClick(object? sender, RoutedEventArgs e) => CopyLines(all: true);

    private void OnSelectAllClick(object? sender, RoutedEventArgs e) => LinesList.Selection.SelectAll();

    private async void CopyLines(bool all)
    {
      if (_viewModel == null) return;

      var selected = LinesList.SelectedItems?.OfType<LogLineViewModel>().ToList() ?? new List<LogLineViewModel>();
      var lines = all || selected.Count == 0 ? _viewModel.Lines.ToList() : selected;
      if (lines.Count == 0) return;

      var text = string.Join(Environment.NewLine, lines.Select(line => line.RawLine));

      var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
      if (clipboard != null)
      {
        await clipboard.SetTextAsync(text);
      }
    }
  }
}
