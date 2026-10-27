using System;
using System.Collections.Generic;

namespace Tail2Jump.FileIO
{
  public class TailEventData
  {
    private static readonly string[] NewLineSeparators = { "\r\n", "\n", "\r" };

    private string? _newLines;

    public TailEventData(string? fileName, string? data, long fileSeekPos, uint byteLength)
      : this(fileName, SplitLines(data), fileSeekPos, byteLength)
    {
    }

    public TailEventData(string? fileName, IReadOnlyList<string>? lines, long fileSeekPos, uint byteLength)
    {
      FileName = fileName ?? string.Empty;
      Lines = lines ?? Array.Empty<string>();
      FileSeekPos = fileSeekPos;
      ByteLenght = byteLength;
    }

    public string FileName { get; }

    /// <summary>Complete lines, without their line terminator.</summary>
    public IReadOnlyList<string> Lines { get; }

    /// <summary>
    /// All lines joined with <see cref="Environment.NewLine"/>. Computed lazily:
    /// the hot path uses <see cref="Lines"/> directly.
    /// </summary>
    public string NewLines => _newLines ??= string.Join(Environment.NewLine, Lines);

    public long FileSeekPos { get; }

    /// <summary>Kept with the original (misspelled) name for API compatibility.</summary>
    public uint ByteLenght { get; }

    private static IReadOnlyList<string> SplitLines(string? data)
    {
      if (string.IsNullOrEmpty(data)) return Array.Empty<string>();

      return data.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries);
    }
  }
}
