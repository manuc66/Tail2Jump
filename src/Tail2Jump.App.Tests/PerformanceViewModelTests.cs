using System;
using NUnit.Framework;
using Tail2Jump.Monitoring;
using Tail2Jump.ViewModels;

namespace Tail2Jump.App.Tests
{
  [TestFixture]
  public class PerformanceViewModelTests
  {
    [Test]
    public void Constructor_SeedsAFullWidthSeries()
    {
      using var viewModel = new PerformanceViewModel(new FakeMetrics());

      Assert.That(viewModel.Cpu.Values, Has.Count.EqualTo(59));
      Assert.That(viewModel.Memory.Values, Has.Count.EqualTo(59));
      Assert.That(viewModel.IsRunning, Is.False);
    }

    [Test]
    public void SampleOnce_AddsOneSampleToEachSeries()
    {
      using var viewModel = new PerformanceViewModel(new FakeMetrics());

      viewModel.SampleOnce();

      Assert.That(viewModel.Cpu.Values, Has.Count.EqualTo(60));
      Assert.That(viewModel.CpuDisplay, Is.EqualTo("42.0 %"));
      Assert.That(viewModel.MemoryDisplay, Is.EqualTo("5 MB"));
    }

    [Test]
    public void MemoryDetails_ReadsPhysicalMemoryOncePerCacheWindow()
    {
      var metrics = new CountingMetrics();
      using var viewModel = new PerformanceViewModel(metrics);

      _ = viewModel.MemoryDetails;
      _ = viewModel.MemoryDetails;
      _ = viewModel.MemoryDetails;

      Assert.That(metrics.TotalMemoryCalls, Is.EqualTo(1));
      Assert.That(metrics.AvailableMemoryCalls, Is.EqualTo(1));
    }

    [Test]
    public void SampleOnce_AfterDispose_DoesNothing()
    {
      var viewModel = new PerformanceViewModel(new FakeMetrics());
      viewModel.Dispose();
      int before = viewModel.Cpu.Values.Count;

      viewModel.SampleOnce();

      Assert.That(viewModel.Cpu.Values, Has.Count.EqualTo(before));
    }

    private class CountingMetrics : ISystemMetrics
    {
      public int TotalMemoryCalls { get; private set; }
      public int AvailableMemoryCalls { get; private set; }

      public double GetCpuUsagePercent() => 10;
      public long GetProcessMemoryBytes() => 1024;
      public long GetTotalMemoryBytes()
      {
        TotalMemoryCalls++;
        return 1024L * 1024 * 1024;
      }

      public long GetAvailableMemoryBytes()
      {
        AvailableMemoryCalls++;
        return 512L * 1024 * 1024;
      }
    }

    private class FakeMetrics : ISystemMetrics
    {
      public double GetCpuUsagePercent() => 42;
      public long GetProcessMemoryBytes() => 5 * 1024 * 1024;
      public long GetTotalMemoryBytes() => 16L * 1024 * 1024 * 1024;
      public long GetAvailableMemoryBytes() => 8L * 1024 * 1024 * 1024;
    }
  }
}
