using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using Tail2Jump.App.Views;
using Tail2Jump.ViewModels;

namespace Tail2Jump.App.Tests
{
  [TestFixture]
  public class LogTextViewVirtualizationTests
  {
    private class FakeLogViewModel
    {
      public ObservableCollection<LogLineViewModel> Lines { get; } = new ObservableCollection<LogLineViewModel>();
    }

    [AvaloniaTest]
    public void OnlyTheVisibleLinesAreRealized()
    {
      var viewModel = new FakeLogViewModel();
      for (int i = 0; i < 10_000; i++)
      {
        viewModel.Lines.Add(new LogLineViewModel("line " + i));
      }

      var view = new LogTextView { DataContext = viewModel };
      var window = new Window { Width = 800, Height = 600, Content = view };

      window.Show();
      Dispatcher.UIThread.RunJobs();

      int realized = view.GetVisualDescendants().OfType<ListBoxItem>().Count();
      TestContext.WriteLine($"realized rows: {realized} / {viewModel.Lines.Count}");

      Assert.That(realized, Is.GreaterThan(0));
      Assert.That(realized, Is.LessThan(200),
        "The log view is not virtualized: it realized " + realized + " rows out of 10000");

      window.Close();
    }
  }
}
