using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Be.Manuc66.Library.FileIO;
using NUnit.Framework;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class TailerTests
  {
    private string _directory;
    private string _file;

    [SetUp]
    public void SetUp()
    {
      _directory = Path.Combine(Path.GetTempPath(), "tail2jump-tests", Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_directory);
      _file = Path.Combine(_directory, "app.log");
      File.WriteAllText(_file, "existing line" + Environment.NewLine);
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
    public void Start_WithMissingDirectory_ReturnsFalse()
    {
      using var tailer = new Tailer
      {
        ParentDirectory = Path.Combine(_directory, "missing", "nope"),
        Filter = "*.log"
      };
      Assert.That(tailer.Start(), Is.False);
    }

    [Test]
    public void AppendingCompleteLines_RaisesTailEventData()
    {
      var received = new List<TailEventData>();
      using var signal = new ManualResetEventSlim(false);

      using var tailer = new Tailer { ParentDirectory = _directory, Filter = "*.log" };
      tailer.eMoreData += (_, data) =>
      {
        lock (received) received.Add(data);
        signal.Set();
      };

      Assert.That(tailer.Start(), Is.True);

      File.AppendAllText(_file, "new line" + Environment.NewLine);

      Assert.That(signal.Wait(TimeSpan.FromSeconds(10)), Is.True, "The tailer did not raise any event");

      lock (received)
      {
        Assert.That(received, Is.Not.Empty);
        Assert.That(received[0].FileName, Is.EqualTo("app.log"));
        Assert.That(received[0].NewLines, Does.Contain("new line"));
      }
    }

    [Test]
    public void AppendingAnIncompleteLine_DoesNotRaiseAnEvent()
    {
      int count = 0;
      using var tailer = new Tailer { ParentDirectory = _directory, Filter = "*.log" };
      tailer.eMoreData += (_, _) => Interlocked.Increment(ref count);

      Assert.That(tailer.Start(), Is.True);

      File.AppendAllText(_file, "incomplete");

      Thread.Sleep(700);
      Assert.That(count, Is.Zero);
    }

    [Test]
    public void Stop_IsIdempotent()
    {
      using var tailer = new Tailer { ParentDirectory = _directory, Filter = "*.log" };
      Assert.That(tailer.Start(), Is.True);

      Assert.That(tailer.Stop(), Is.True);
      Assert.That(tailer.Stop(), Is.True);
    }

    [Test]
    public void Start_DoesNotReplayExistingContent()
    {
      int count = 0;
      using var signal = new ManualResetEventSlim(false);
      using var tailer = new Tailer { ParentDirectory = _directory, Filter = "*.log" };
      tailer.eMoreData += (_, _) =>
      {
        Interlocked.Increment(ref count);
        signal.Set();
      };

      Assert.That(tailer.Start(), Is.True);
      Assert.That(signal.Wait(TimeSpan.FromMilliseconds(500)), Is.False);
      Assert.That(count, Is.Zero);
    }
  }
}
