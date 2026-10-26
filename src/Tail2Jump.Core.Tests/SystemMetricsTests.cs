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
