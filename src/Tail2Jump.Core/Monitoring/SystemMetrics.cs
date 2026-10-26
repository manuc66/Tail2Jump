using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Tail2Jump.Monitoring
{
  /// <summary>
  /// Portable system metrics (CPU usage and memory).
  /// </summary>
  public interface ISystemMetrics
  {
    /// <summary>System CPU usage in percent since the previous call (0 when unknown).</summary>
    double GetCpuUsagePercent();

    /// <summary>Memory currently used by the current process, in bytes.</summary>
    long GetProcessMemoryBytes();

    /// <summary>Total physical memory in bytes (0 when unknown).</summary>
    long GetTotalMemoryBytes();

    /// <summary>Available physical memory in bytes (0 when unknown).</summary>
    long GetAvailableMemoryBytes();
  }

  public readonly struct CpuTimes
  {
    public CpuTimes(long total, long idle)
    {
      Total = total;
      Idle = idle;
    }

    public long Total { get; }
    public long Idle { get; }
  }

  public class SystemMetrics : ISystemMetrics
  {
    private readonly object _lock = new object();
    private long _lastTotal = -1;
    private long _lastIdle;

    public double GetCpuUsagePercent()
    {
      CpuTimes current;
      try
      {
        current = ReadCpuTimes();
      }
      catch (Exception)
      {
        return 0;
      }

      if (current.Total <= 0) return 0;

      lock (_lock)
      {
        if (_lastTotal < 0)
        {
          _lastTotal = current.Total;
          _lastIdle = current.Idle;
          return 0;
        }

        var previous = new CpuTimes(_lastTotal, _lastIdle);
        _lastTotal = current.Total;
        _lastIdle = current.Idle;

        return ComputeUsagePercent(previous, current);
      }
    }

    public long GetProcessMemoryBytes()
    {
      return GC.GetTotalMemory(false);
    }

    public long GetTotalMemoryBytes()
    {
      try
      {
        if (OperatingSystem.IsLinux()) return ReadLinuxMemory().Total;
        if (OperatingSystem.IsWindows()) return (long)ReadWindowsMemory().TotalPhysical;
        if (OperatingSystem.IsMacOS()) return ReadMacTotalMemory();
      }
      catch (Exception)
      {
        // Metrics are best-effort only.
      }

      return 0;
    }

    public long GetAvailableMemoryBytes()
    {
      try
      {
        if (OperatingSystem.IsLinux()) return ReadLinuxMemory().Available;
        if (OperatingSystem.IsWindows()) return (long)ReadWindowsMemory().AvailablePhysical;
        if (OperatingSystem.IsMacOS()) return ReadMacAvailableMemory();
      }
      catch (Exception)
      {
        // Metrics are best-effort only.
      }

      return 0;
    }

    private static CpuTimes ReadCpuTimes()
    {
      if (OperatingSystem.IsLinux()) return ReadLinuxCpuTimes();
      if (OperatingSystem.IsWindows()) return ReadWindowsCpuTimes();
      if (OperatingSystem.IsMacOS()) return ReadMacCpuTimes();
      return new CpuTimes(0, 0);
    }

    #region Linux

    /// <summary>
    /// Computes the CPU usage between two samples. Returns 0 when the counters
    /// did not move (or went backwards, e.g. after a suspend).
    /// </summary>
    public static double ComputeUsagePercent(CpuTimes previous, CpuTimes current)
    {
      long totalDelta = current.Total - previous.Total;
      long idleDelta = current.Idle - previous.Idle;

      if (totalDelta <= 0) return 0;

      double usage = (totalDelta - idleDelta) * 100.0 / totalDelta;
      return Math.Clamp(usage, 0, 100);
    }

    /// <summary>
    /// Parses the first line of /proc/stat.
    /// Format: cpu user nice system idle iowait irq softirq steal guest guest_nice
    /// guest and guest_nice are already accounted for in user/nice, so they are
    /// excluded from the total.
    /// </summary>
    public static CpuTimes ParseLinuxCpuLine(string line)
    {
      if (string.IsNullOrWhiteSpace(line)) return new CpuTimes(0, 0);

      var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Skip(1)
        .Select(value => long.TryParse(value, out long parsed) ? parsed : 0)
        .ToArray();

      if (parts.Length < 4) return new CpuTimes(0, 0);

      long idle = parts[3] + (parts.Length > 4 ? parts[4] : 0);
      long total = parts.Take(8).Sum();
      return new CpuTimes(total, idle);
    }

    private static CpuTimes ReadLinuxCpuTimes()
    {
      var line = File.ReadLines("/proc/stat").FirstOrDefault();
      return ParseLinuxCpuLine(line);
    }

    private static (long Total, long Available) ReadLinuxMemory()
    {
      long totalKb = 0;
      long availableKb = 0;

      foreach (var line in File.ReadLines("/proc/meminfo"))
      {
        if (line.StartsWith("MemTotal:", StringComparison.Ordinal))
        {
          totalKb = ParseMeminfoValue(line);
        }
        else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
        {
          availableKb = ParseMeminfoValue(line);
        }

        if (totalKb > 0 && availableKb > 0) break;
      }

      return (totalKb * 1024, availableKb * 1024);
    }

    private static long ParseMeminfoValue(string line)
    {
      var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      return parts.Length > 1 && long.TryParse(parts[1], out long value) ? value : 0;
    }

    #endregion

    #region Windows

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
      public uint Low;
      public uint High;

      public ulong ToUInt64() => ((ulong)High << 32) | Low;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
      public uint Length;
      public uint MemoryLoad;
      public ulong TotalPhysical;
      public ulong AvailablePhysical;
      public ulong TotalPageFile;
      public ulong AvailablePageFile;
      public ulong TotalVirtual;
      public ulong AvailableVirtual;
      public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    private static CpuTimes ReadWindowsCpuTimes()
    {
      if (!GetSystemTimes(out var idle, out var kernel, out var user)) return new CpuTimes(0, 0);

      // On Windows, kernel time already includes idle time.
      ulong total = kernel.ToUInt64() + user.ToUInt64();
      return new CpuTimes((long)total, (long)idle.ToUInt64());
    }

    private static MemoryStatusEx ReadWindowsMemory()
    {
      var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
      return GlobalMemoryStatusEx(ref status) ? status : default;
    }

    #endregion

    #region macOS

    private static CpuTimes ReadMacCpuTimes()
    {
      // kern.cp_time returns: user nice sys intr idle (ticks).
      var output = RunProcess("sysctl", "-n kern.cp_time");
      if (string.IsNullOrWhiteSpace(output)) return new CpuTimes(0, 0);

      var values = output.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(value => long.TryParse(value, out long parsed) ? parsed : 0)
        .ToArray();

      if (values.Length < 5) return new CpuTimes(0, 0);

      return new CpuTimes(values.Sum(), values[4]);
    }

    private static long ReadMacTotalMemory()
    {
      var output = RunProcess("sysctl", "-n hw.memsize");
      return long.TryParse(output?.Trim(), out long value) ? value : 0;
    }

    private static long ReadMacAvailableMemory()
    {
      var output = RunProcess("vm_stat", string.Empty);
      if (string.IsNullOrWhiteSpace(output)) return 0;

      long pageSize = 4096;
      var pageSizeIndex = output.IndexOf("page size of", StringComparison.OrdinalIgnoreCase);
      if (pageSizeIndex >= 0)
      {
        var end = output.IndexOf(" bytes", pageSizeIndex, StringComparison.OrdinalIgnoreCase);
        if (end > pageSizeIndex)
        {
          var sizeText = output.Substring(pageSizeIndex + "page size of".Length, end - pageSizeIndex)
            .Trim();
          if (long.TryParse(sizeText, out long parsedSize)) pageSize = parsedSize;
        }
      }

      long freePages = ReadVmStatPages(output, "Pages free");
      long inactivePages = ReadVmStatPages(output, "Pages inactive");
      return (freePages + inactivePages) * pageSize;
    }

    private static long ReadVmStatPages(string output, string key)
    {
      foreach (var line in output.Split('\n'))
      {
        if (!line.StartsWith(key, StringComparison.Ordinal)) continue;

        var parts = line.Split(':', 2);
        if (parts.Length < 2) return 0;

        var value = parts[1].Trim().TrimEnd('.');
        return long.TryParse(value, out long pages) ? pages : 0;
      }

      return 0;
    }

    private static string RunProcess(string fileName, string arguments)
    {
      try
      {
        using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
          RedirectStandardOutput = true,
          UseShellExecute = false,
          CreateNoWindow = true
        });

        if (process == null) return string.Empty;

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(2000);
        return output;
      }
      catch (Exception)
      {
        return string.Empty;
      }
    }

    #endregion
  }
}
