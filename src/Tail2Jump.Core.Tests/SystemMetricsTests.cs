using System;
using System.Threading;
using NUnit.Framework;
using Tail2Jump.Monitoring;

namespace Tail2Jump.Core.Tests
{
  [TestFixture]
  public class SystemMetricsTests
  {
    [Test]
    public void CpuUsage_AfterTwoReads_IsBetweenZeroAndHundred()
    {
      var metrics = new SystemMetrics();

      metrics.GetCpuUsagePercent();
      Thread.Sleep(200);
      var usage = metrics.GetCpuUsagePercent();

      Assert.That(usage, Is.InRange(0.0, 100.0));
    }

    [Test]
    public void ParseLinuxCpuLine_DoesNotCountGuestTwice()
    {
      // user nice system idle iowait irq softirq steal guest guest_nice
      var times = SystemMetrics.ParseLinuxCpuLine("cpu  100 0 50 850 0 0 0 0 20 0");

      Assert.That(times.Total, Is.EqualTo(1000));
      Assert.That(times.Idle, Is.EqualTo(850));
    }

    [Test]
    public void ComputeUsagePercent_ReturnsTheBusyShare()
    {
      var previous = new CpuTimes(1000, 800);
      var current = new CpuTimes(1100, 850);

      Assert.That(SystemMetrics.ComputeUsagePercent(previous, current), Is.EqualTo(50).Within(0.01));
    }

    [Test]
    public void ComputeUsagePercent_WithUnchangedCounters_ReturnsZero()
    {
      var sample = new CpuTimes(1000, 800);

      Assert.That(SystemMetrics.ComputeUsagePercent(sample, sample), Is.Zero);
    }

    [Test]
    public void ProcessMemory_IsPositive()
    {
      var metrics = new SystemMetrics();

      Assert.That(metrics.GetProcessMemoryBytes(), Is.GreaterThan(0));
    }

    [Test]
    public void PhysicalMemory_IsPositiveOnSupportedPlatforms()
    {
      if (!OperatingSystem.IsLinux() && !OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS())
      {
        Assert.Ignore("No system metrics on this platform");
      }

      var metrics = new SystemMetrics();

      Assert.That(metrics.GetTotalMemoryBytes(), Is.GreaterThan(0));
      Assert.That(metrics.GetAvailableMemoryBytes(), Is.GreaterThan(0));
      Assert.That(metrics.GetAvailableMemoryBytes(), Is.LessThanOrEqualTo(metrics.GetTotalMemoryBytes()));
    }
  }
}
