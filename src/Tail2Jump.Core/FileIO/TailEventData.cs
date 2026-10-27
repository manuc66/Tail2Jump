using System;
using System.Collections.Generic;

namespace Tail2Jump.FileIO
{
  public class TailEventData
  {
    private static readonly string[] NewLineSeparators = { "\r\n", "\n", "\r" };

    private string? _newLines;

    public TailEventData(string? fileName, string? data)
      : this(fileName, SplitLines(data))
    {
    }

    public TailEventData(string? fileName, IReadOnlyList<string>? lines)
    {
      FileName = fileName ?? string.Empty;
      Lines = lines ?? Array.Empty<string>();
    }

    public string FileName { get; }

    /// <summary>Complete lines, without their line terminator.</summary>
    public IReadOnlyList<string> Lines { get; }

    /// <summary>
    /// All lines joined with <see cref="Environment.NewLine"/>. Computed lazily:
    /// the hot path uses <see cref="Lines"/> directly.
    /// </summary>
    public string NewLines => _newLines ??= string.Join(Environment.NewLine, Lines);

    private static IReadOnlyList<string> SplitLines(string? data)
    {
      if (string.IsNullOrEmpty(data)) return Array.Empty<string>();

      return data.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries);
    }
  }
}
