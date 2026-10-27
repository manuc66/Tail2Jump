using System;
using System.Collections.Generic;
using System.Text;

namespace Be.Manuc66.Library.FileIO
{
  /// <summary>
  /// Splits a UTF-8 buffer into complete lines without decoding the whole
  /// buffer first. Only complete lines (terminated by '\n') are returned; the
  /// trailing partial line stays in the file for the next read.
  /// </summary>
  public static class Utf8LineSplitter
  {
    public static bool TrySplitCompleteLines(ReadOnlySpan<byte> buffer, out IReadOnlyList<string> lines,
      out int consumedBytes)
    {
      lines = Array.Empty<string>();
      consumedBytes = 0;

      int lastNewLine = buffer.LastIndexOf((byte)'\n');
      if (lastNewLine < 0) return false;

      consumedBytes = lastNewLine + 1;

      var result = new List<string>();
      int lineStart = 0;

      for (int index = 0; index <= lastNewLine; index++)
      {
        if (buffer[index] != (byte)'\n') continue;

        int lineEnd = index;
        if (lineEnd > lineStart && buffer[lineEnd - 1] == (byte)'\r')
        {
          lineEnd--;
        }

        result.Add(Encoding.UTF8.GetString(buffer[lineStart..lineEnd]));
        lineStart = index + 1;
      }

      lines = result;
      return true;
    }
  }
}
