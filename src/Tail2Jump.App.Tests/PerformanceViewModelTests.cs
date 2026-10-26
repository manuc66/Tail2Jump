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
    public void SampleOnce_AfterDispose_DoesNothing()
    {
      var viewModel = new PerformanceViewModel(new FakeMetrics());
      viewModel.Dispose();
      int before = viewModel.Cpu.Values.Count;

      viewModel.SampleOnce();

      Assert.That(viewModel.Cpu.Values, Has.Count.EqualTo(before));
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
