using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Be.Manuc66.Library.FileIO;
using NUnit.Framework;
using Tail2Jump.Config.Objects;
using Tail2Jump.Controllers;
using Tail2Jump.Enums;
using Tail2Jump.Logging;
using Tail2Jump.ViewModels;

namespace Tail2Jump.App.Tests
{
  [TestFixture]
  public class MainWindowViewModelTests
  {
    private string _configPath = null!;
    private string _directory = null!;
    private string _file = null!;
    private WatcherEngine _engine = null!;
    private MainWindowViewModel _viewModel = null!;

    [SetUp]
    public void SetUp()
    {
      // Run everything inline: there is no UI thread in these tests.
      SynchronizationContext.SetSynchronizationContext(null);

      _configPath = Path.Combine(Path.GetTempPath(), "tail2jump-tests", Guid.NewGuid() + ".json");
      CFG.ConfigFilePath = _configPath;
      CFG.Conf = new AppConfig();

      _directory = Path.Combine(Path.GetTempPath(), "tail2jump-tests", Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_directory);
      _file = Path.Combine(_directory, "app.log");
      File.WriteAllText(_file, string.Empty);

      _engine = new WatcherEngine(new TailLineCache(), new TailLineCache());
      _viewModel = new MainWindowViewModel(_engine, new LogController());
    }

    [TearDown]
    public void TearDown()
    {
      _viewModel?.Dispose();
      try
      {
        Directory.Delete(_directory, true);
      }
      catch (IOException)
      {
      }
    }

    [Test]
    public void CreateWatcher_AddsTheWatcherAndPersistsTheCriterion()
    {
      var instance = _viewModel.CreateWatcher(_directory, "*.log", includeSubdirectories: false);

      Assert.That(instance, Is.Not.Null);
      Assert.That(_viewModel.Watchers, Has.Count.EqualTo(1));
      Assert.That(CFG.Conf.Search, Has.Count.EqualTo(1));
      Assert.That(CFG.Conf.Search[0].PathDirectory, Is.EqualTo(_directory));
      Assert.That(CFG.Conf.Search[0].FileExtension, Is.EqualTo("*.log"));
    }

    [Test]
    public void SetSelection_UpdatesTheStartAndStopCommands()
    {
      var instance = _viewModel.CreateWatcher(_directory, "*.log", false);
      _viewModel.SetSelection(new[] { instance! });

      Assert.That(_viewModel.CanStop, Is.True);
      Assert.That(_viewModel.CanStart, Is.False);

      _viewModel.StopCommand.Execute(null);

      Assert.That(_viewModel.CanStart, Is.True);
      Assert.That(_viewModel.CanStop, Is.False);
    }

    [Test]
    public void NewLines_AreAddedToTheViewModel()
    {
      _viewModel.CreateWatcher(_directory, "*.log", false);
      File.AppendAllText(_file, "first" + Environment.NewLine + "second" + Environment.NewLine);

      WaitFor(() => _viewModel.Lines.Count == 2);

      Assert.That(_viewModel.Lines.Select(line => line.RawLine),
        Is.EqualTo(new[] { "app.log|first", "app.log|second" }));
    }

    [Test]
    public void ManyNewLines_AreAddedInBatches()
    {
      _viewModel.CreateWatcher(_directory, "*.log", false);

      var builder = new System.Text.StringBuilder();
      for (int i = 0; i < 1500; i++)
      {
        builder.Append("line ").Append(i).Append('\n');
      }

      File.AppendAllText(_file, builder.ToString());

      WaitFor(() => _viewModel.Lines.Count == 1500);
      Assert.That(_viewModel.Lines, Has.Count.EqualTo(1500));
    }

    [Test]
    public void Filter_IsAppliedAfterTheDebounce()
    {
      _viewModel.CreateWatcher(_directory, "*.log", false);
      File.AppendAllText(_file, "INFO one" + Environment.NewLine + "ERROR two" + Environment.NewLine);
      WaitFor(() => _viewModel.Lines.Count == 2);

      _viewModel.ShowSearch = true;
      _viewModel.FilterActivated = true;
      _viewModel.SearchText = "ERROR";

      WaitFor(() => _viewModel.Lines.Count == 1);

      _viewModel.SearchText = string.Empty;
      WaitFor(() => _viewModel.Lines.Count == 2);
    }

    [Test]
    public void Dispose_UnsubscribesFromTheEngineCollection()
    {
      bool notified = false;
      _viewModel.PropertyChanged += (_, e) =>
      {
        if (e.PropertyName == nameof(MainWindowViewModel.WatcherCount)) notified = true;
      };

      _viewModel.Dispose();
      _engine.ListWatcher.Add(new Tail2Jump.Objects.MonitoringInstance());

      Assert.That(notified, Is.False);
    }

    [Test]
    public void DisplayedLines_AreCappedToTheConfiguredMaximum()
    {
      _viewModel.Dispose();
      _engine = new WatcherEngine(new TailLineCache(), new TailLineCache());
      _viewModel = new MainWindowViewModel(_engine, new LogController(), maxDisplayedLines: 5);
      _viewModel.CreateWatcher(_directory, "*.log", false);

      var builder = new System.Text.StringBuilder();
      for (int i = 0; i < 10; i++)
      {
        builder.Append("line ").Append(i).Append('\n');
      }

      File.AppendAllText(_file, builder.ToString());

      var stopwatch = Stopwatch.StartNew();
      while (stopwatch.Elapsed < TimeSpan.FromSeconds(5) && _viewModel.Lines.Count != 5)
      {
        Thread.Sleep(50);
      }

      Assert.That(_viewModel.Lines, Has.Count.EqualTo(5),
        $"watchers={_viewModel.Watchers.Count}, lines={_viewModel.Lines.Count}");
      Assert.That(_viewModel.Lines[^1].RawLine, Is.EqualTo("app.log|line 9"));
    }

    [Test]
    public void Search_HighlightsNewLines()
    {
      _viewModel.ShowSearch = true;
      _viewModel.SearchText = "ERROR";
      _viewModel.CreateWatcher(_directory, "*.log", false);

      File.AppendAllText(_file, "an ERROR happened" + Environment.NewLine);

      WaitFor(() => _viewModel.Lines.Count == 1);
      Assert.That(_viewModel.Lines[0].Segments.Any(segment => segment.IsHighlighted), Is.True);
    }

    [Test]
    public void Filter_WithEmptyText_KeepsEveryLine()
    {
      _viewModel.CreateWatcher(_directory, "*.log", false);
      File.AppendAllText(_file, "INFO one" + Environment.NewLine + "ERROR two" + Environment.NewLine);
      WaitFor(() => _viewModel.Lines.Count == 2);

      _viewModel.ShowSearch = true;
      _viewModel.FilterActivated = true;
      WaitFor(() => _viewModel.Lines.Count == 2);

      _viewModel.FilterActivated = false;
      Assert.That(_viewModel.Lines, Has.Count.EqualTo(2));
    }

    [Test]
    public void Clear_EmptiesTheDisplayedLines()
    {
      _viewModel.CreateWatcher(_directory, "*.log", false);
      File.AppendAllText(_file, "first" + Environment.NewLine);
      WaitFor(() => _viewModel.Lines.Count == 1);

      _viewModel.ClearCommand.Execute(null);

      Assert.That(_viewModel.Lines, Is.Empty);
    }

    [Test]
    public void ShowPerformance_StartsAndStopsTheSampler()
    {
      Assert.That(_viewModel.Performance.IsRunning, Is.False);

      _viewModel.ShowPerformance = true;
      Assert.That(_viewModel.Performance.IsRunning, Is.True);

      _viewModel.ShowPerformance = false;
      Assert.That(_viewModel.Performance.IsRunning, Is.False);
    }

    [Test]
    public void OpenAtLine_WithAnUnknownFile_LogsAnError()
    {
      int errorsBefore = _viewModel.Logs.Count(log => log.Type == LogType.Error);

      _viewModel.OpenAtLine("/does/not/exist.cs:42");

      Assert.That(_viewModel.Logs.Count(log => log.Type == LogType.Error), Is.GreaterThan(errorsBefore));
    }

    private static void WaitFor(Func<bool> condition, int timeoutMilliseconds = 10000)
    {
      var stopwatch = Stopwatch.StartNew();

      while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds)
      {
        if (condition()) return;
        Thread.Sleep(50);
      }

      Assert.Fail("Condition not met before the timeout");
    }
  }
}
