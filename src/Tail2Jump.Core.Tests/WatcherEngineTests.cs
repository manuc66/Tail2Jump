using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Be.Manuc66.Library.FileIO;
using NUnit.Framework;
using Tail2Jump.Controllers;
using Tai2JumpInfra.MonitoringInstanceStates;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class WatcherEngineTests
  {
    private string _directory = null!;
    private string _file = null!;

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
      Assert.That(instance!.State, Is.InstanceOf<StartedMonitoringInstance>());
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
    public void RemoveWatcher_WithNull_DoesNotThrow()
    {
      using var engine = new WatcherEngine(new TailLineCache(), new TailLineCache());

      Assert.DoesNotThrow(() => engine.RemoveWatcher((System.Collections.Generic.ICollection<Tai2JumpInfra.IMonitoringInstance>)null!));
    }

    [Test]
    public void RemoveWatcher_StopsAndUnregistersTheWatcher()
    {
      using var engine = new WatcherEngine(new TailLineCache(), new TailLineCache());
      var instance = engine.CreateWatcher(_directory, "*.log");

      engine.RemoveWatcher(instance!);

      Assert.That(engine.ListWatcher, Is.Empty);
    }

    [Test]
    public void ApplyFilter_WhileALineIsBeingAdded_DoesNotDuplicateIt()
    {
      var blockingCache = new BlockingCache(new TailLineCache());
      using var engine = new WatcherEngine(blockingCache, new TailLineCache());
      engine.CreateWatcher(_directory, "*.log");

      File.AppendAllText(_file, "MATCH one" + Environment.NewLine);
      WaitForLines(engine, 1);

      blockingCache.BlockNextAdd();
      File.AppendAllText(_file, "MATCH two" + Environment.NewLine);

      Assert.That(blockingCache.WaitUntilAddBlocked(TimeSpan.FromSeconds(10)), Is.True,
        "The second line was never read");

      var filterTask = Task.Run(() => engine.ApplyFilter("MATCH"));

      // Without a lock the filter rebuild runs while the line is half-added.
      Thread.Sleep(300);
      blockingCache.ReleaseAdd();

      Assert.That(filterTask.Wait(TimeSpan.FromSeconds(10)), Is.True);
      WaitForLines(engine, 2);

      Assert.That(engine.FileAccessor.LineIndexed, Is.EqualTo(2));
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

    private class BlockingCache : ITailLineCache
    {
      private readonly ITailLineCache _inner;
      private readonly ManualResetEventSlim _addBlocked = new ManualResetEventSlim(false);
      private readonly ManualResetEventSlim _releaseAdd = new ManualResetEventSlim(false);
      private volatile bool _blockNextAdd;

      public BlockingCache(ITailLineCache inner)
      {
        _inner = inner;
      }

      public void BlockNextAdd()
      {
        _addBlocked.Reset();
        _releaseAdd.Reset();
        _blockNextAdd = true;
      }

      public bool WaitUntilAddBlocked(TimeSpan timeout) => _addBlocked.Wait(timeout);

      public void ReleaseAdd() => _releaseAdd.Set();

      public int LineIndexed => _inner.LineIndexed;

      public string[] GetLines(int start, int len) => _inner.GetLines(start, len);

      public void Clear() => _inner.Clear();

      public void AddData(TailEventData tailData, string? filter = null)
      {
        _inner.AddData(tailData, filter);

        if (!_blockNextAdd) return;
        _blockNextAdd = false;
        _addBlocked.Set();
        _releaseAdd.Wait(TimeSpan.FromSeconds(10));
      }

      public void AddData(ITailLineCache other, string? filter) => _inner.AddData(other, filter);

      public void Dispose() => _inner.Dispose();
    }
  }
}
