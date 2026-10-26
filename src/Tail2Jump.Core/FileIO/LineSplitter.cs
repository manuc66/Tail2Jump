using System;
using System.Collections.Generic;
using System.Linq;

namespace Be.Manuc66.Library.FileIO
{
  /// <summary>
  /// Splits a text buffer into complete lines, accepting both LF and CRLF
  /// regardless of the current platform.
  /// </summary>
  public static class LineSplitter
  {
    public static bool TrySplitCompleteLines(string buffer, out IReadOnlyList<string> lines, out int consumedChars)
    {
      lines = Array.Empty<string>();
      consumedChars = 0;

      if (string.IsNullOrEmpty(buffer)) return false;

      int lastNewLine = buffer.LastIndexOf('\n');
      if (lastNewLine < 0) return false;

      consumedChars = lastNewLine + 1;

      var parts = buffer.Substring(0, consumedChars).Split('\n').ToList();

      // The buffer ends with '\n', so the last part is always empty.
      if (parts.Count > 0 && parts[parts.Count - 1].Length == 0)
      {
        parts.RemoveAt(parts.Count - 1);
      }

      lines = parts
        .Select(part => part.EndsWith("\r", StringComparison.Ordinal)
          ? part.Substring(0, part.Length - 1)
          : part)
        .ToList();

      return true;
    }
  }
}
