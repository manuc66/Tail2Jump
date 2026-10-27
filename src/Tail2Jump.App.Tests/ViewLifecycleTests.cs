using System.Collections.ObjectModel;
using Avalonia.Controls;
using Tail2Jump.FileIO;
using NUnit.Framework;
using Tail2Jump.App.Charts;
using Tail2Jump.App.Views;
using Tail2Jump.Controllers;
using Tail2Jump.Logging;
using Tail2Jump.ViewModels;

namespace Tail2Jump.App.Tests
{
  [TestFixture]
  public class ViewLifecycleTests
  {
    [Avalonia.Headless.NUnit.AvaloniaTest]
    public void SparklineChart_StopsObservingTheCollectionWhenDetached()
    {
      var values = new ObservableCollection<double> { 1, 2, 3 };
      var chart = new SparklineChart { Values = values };
      var window = new Window { Content = chart };

      window.Show();
      Assert.That(chart.IsObservingValues, Is.True);

      window.Close();
      Assert.That(chart.IsObservingValues, Is.False);
    }

    [Avalonia.Headless.NUnit.AvaloniaTest]
    public void LogTextView_StopsListeningToTheViewModelWhenDetached()
    {
      using var engine = new WatcherEngine(new TailLineCache(), new TailLineCache());
      using var viewModel = new MainWindowViewModel(engine, new LogController());
      var view = new LogTextView { DataContext = viewModel };
      var window = new Window { Content = view };

      window.Show();
      Assert.That(view.IsAttachedToViewModel, Is.True);

      window.Close();
      Assert.That(view.IsAttachedToViewModel, Is.False);
    }
  }
}
