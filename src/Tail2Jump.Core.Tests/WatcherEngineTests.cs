using System;
using System.Diagnostics;
using System.IO;
using Be.Manuc66.Library.FileIO;
using NUnit.Framework;
using Tail2Jump.Controllers;
using Tai2JumpInfra.MonitoringInstanceStates;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class WatcherEngineTests
  {
    private string _directory;
    private string _file;

    [SetUp]
    public void SetUp()
    {
      _directory = Path.Combine(Path.GetTempPath(), "tail2jump-tests", Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_directory);
      _file = Path.Combine(_directory, "app.log");
      File.WriteAllText(_file, string.Empty);
    }

    [TearDown]
    public void TearDown()
    {
      try
      {
        Directory.Delete(_directory, true);
      }
      catch (IOException)
      {
      }
    }

    [Test]
    public void CreateWatcher_StartsTheWatcher()
    {
      using var engine = new WatcherEngine(new TailLineCache(), new TailLineCache());

      var instance = engine.CreateWatcher(_directory, "*.log");

      Assert.That(instance, Is.Not.Null);
      Assert.That(instance.State, Is.InstanceOf<StartedMonitoringInstance>());
      Assert.That(engine.ListWatcher, Has.Count.EqualTo(1));
    }

    [Test]
    public void ApplyFilter_WithEmptyText_ShowsEveryLineAgain()
    {
      using var engine = new WatcherEngine(new TailLineCache(), new TailLineCache());
      engine.CreateWatcher(_directory, "*.log");

      File.AppendAllText(_file, "INFO first" + Environment.NewLine + "ERROR second" + Environment.NewLine);

      WaitForLines(engine, 2);
      Assert.That(engine.FileAccessor.LineIndexed, Is.EqualTo(2));

      engine.ApplyFilter("ERROR");
      Assert.That(engine.FileAccessor.LineIndexed, Is.EqualTo(1));

      engine.ApplyFilter(string.Empty);
      Assert.That(engine.FileAccessor.LineIndexed, Is.EqualTo(2));
    }

    [Test]
    public void RemoveWatcher_StopsAndUnregistersTheWatcher()
    {
      using var engine = new WatcherEngine(new TailLineCache(), new TailLineCache());
      var instance = engine.CreateWatcher(_directory, "*.log");

      engine.RemoveWatcher(instance);

      Assert.That(engine.ListWatcher, Is.Empty);
    }

    private static void WaitForLines(WatcherEngine engine, int expected)
    {
      var stopwatch = Stopwatch.StartNew();

      while (stopwatch.Elapsed < TimeSpan.FromSeconds(10))
      {
        if (engine.FileAccessor.LineIndexed >= expected) return;
        System.Threading.Thread.Sleep(50);
      }

      Assert.Fail($"Expected {expected} lines, got {engine.FileAccessor.LineIndexed}");
    }
  }
}
