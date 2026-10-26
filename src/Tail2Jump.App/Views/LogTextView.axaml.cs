using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Tail2Jump.Rendering;
using Tail2Jump.ViewModels;

namespace Tail2Jump.App.Views
{
  public partial class LogTextView : UserControl
  {
    private MainWindowViewModel _viewModel;

    public LogTextView()
    {
      InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
      if (_viewModel != null)
      {
        _viewModel.LinesUpdated -= OnLinesUpdated;
      }

      _viewModel = DataContext as MainWindowViewModel;

      if (_viewModel != null)
      {
        _viewModel.LinesUpdated += OnLinesUpdated;
      }

      base.OnDataContextChanged(e);
    }

    private void OnLinesUpdated()
    {
      if (_viewModel == null || !_viewModel.AutoScroll) return;
      ScrollToEnd();
    }

    private void ScrollToEnd()
    {
      Dispatcher.UIThread.Post(() =>
      {
        var scrollViewer = LinesList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scrollViewer == null) return;

        scrollViewer.Offset = new Vector(scrollViewer.Offset.X,
          Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height));
      }, DispatcherPriority.Background);
    }

    private void OnSegmentPointerPressed(object sender, PointerPressedEventArgs e)
    {
      if (sender is not TextBlock { DataContext: LogLineSegment segment }) return;
      if (!segment.IsHyperLink) return;
      if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed) return;

      _viewModel?.OpenAtLine(segment.Text);
      e.Handled = true;
    }
  }
}
