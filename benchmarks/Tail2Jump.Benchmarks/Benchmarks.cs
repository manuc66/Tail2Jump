using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Tail2Jump.FileIO;
using Tail2Jump.Rendering;

namespace Tail2Jump.Benchmarks
{
  [MemoryDiagnoser]
  [SimpleJob(warmupCount: 3, iterationCount: 5)]
  public class LineSplittingBenchmarks
  {
    private byte[] _buffer = Array.Empty<byte>();

    [GlobalSetup]
    public void Setup()
    {
      var builder = new StringBuilder(1024 * 1024);
      for (int i = 0; i < 10_000; i++)
      {
        builder.Append("2026-10-27 01:30:00 INFO line ").Append(i)
          .Append(" payload payload payload").Append('\n');
      }

      _buffer = Encoding.UTF8.GetBytes(builder.ToString());
    }

    [Benchmark(Baseline = true)]
    public int LegacyDecodeAndSplit()
    {
      string text = Encoding.UTF8.GetString(_buffer);
      return text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    [Benchmark]
    public int Utf8SpanSplit()
    {
      return Utf8LineSplitter.TrySplitCompleteLines(_buffer, out var lines, out _) ? lines.Count : 0;
    }

    [Benchmark]
    public int CountNewLinesOnly()
    {
      ReadOnlySpan<byte> span = _buffer;
      int count = 0;

      for (int i = 0; i < span.Length; i++)
      {
        if (span[i] == (byte)'\n') count++;
      }

      return count;
    }
  }

  [MemoryDiagnoser]
  [SimpleJob(warmupCount: 3, iterationCount: 5)]
  public class LineParsingBenchmarks
  {
    private const string Line =
      "2026-10-27 01:30:02 ERROR failure at C:\\src\\Program.cs:42 while processing payload";

    [Benchmark(Baseline = true)]
    public int LegacyStringSplit()
    {
      string[] words = Line.Split(' ', StringSplitOptions.None);
      int segments = 0;

      for (int i = 0; i < words.Length; i++)
      {
        string word = words[i];
        string next = i < words.Length - 1 ? words[i + 1] : string.Empty;

        if (word.EndsWith(":line", StringComparison.Ordinal) ||
            word.EndsWith(":ligne", StringComparison.Ordinal))
        {
          segments++;
          if (int.TryParse(next, out _)) i++;
          continue;
        }

        if (EndsWithLineNumber(word)) segments++;
      }

      return segments;
    }

    [Benchmark]
    public int MemorySliceParse()
    {
      return LogLineParser.Parse(Line).Count;
    }

    [Benchmark]
    public int MemorySliceParseWithSearch()
    {
      return LogLineParser.Parse(Line, "ERROR", ignoreCase: true).Count;
    }

    private static bool EndsWithLineNumber(string word)
    {
      int colon = word.LastIndexOf(':');
      if (colon <= 0 || colon >= word.Length - 1) return false;

      string pathPart = word.Substring(0, colon);
      if (!pathPart.Contains('.') || pathPart.Contains("://")) return false;

      return int.TryParse(word.AsSpan(colon + 1), out int line) && line > 0;
    }
  }

  [MemoryDiagnoser]
  [SimpleJob(warmupCount: 3, iterationCount: 5)]
  public class LineCacheBenchmarks
  {
    private readonly List<TailEventData> _events = new List<TailEventData>();
    private TailLineCache _cache = null!;
    private TailLineCache _filtered = null!;

    [GlobalSetup]
    public void Setup()
    {
      _cache = new TailLineCache(200_000);
      _filtered = new TailLineCache(200_000);

      for (int chunk = 0; chunk < 50; chunk++)
      {
        var lines = new string[1000];

        for (int i = 0; i < lines.Length; i++)
        {
          int index = chunk * 1000 + i;
          lines[i] = (index % 10 == 0 ? "ERROR " : "INFO ") + "line " + index;
        }

        var tailEvent = new TailEventData("app.log", lines, 0, 0);
        _events.Add(tailEvent);
        _cache.AddData(tailEvent);
      }
    }

    [Benchmark]
    public int AddLines()
    {
      var target = new TailLineCache(200_000);

      for (int i = 0; i < 10; i++)
      {
        target.AddData(_events[i]);
      }

      return target.LineIndexed;
    }

    [Benchmark]
    public int ReadPage()
    {
      return _cache.GetLines(25_000, 50).Length;
    }

    [Benchmark]
    public int ReadEntriesPage()
    {
      return _cache.GetEntries(25_000, 50).Length;
    }

    [Benchmark]
    public int FilterReplay()
    {
      _filtered.Clear();
      _filtered.AddData(_cache, "ERROR");
      return _filtered.LineIndexed;
    }
  }
}
