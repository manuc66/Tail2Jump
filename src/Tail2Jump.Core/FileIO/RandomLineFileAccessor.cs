using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Be.Manuc66.Library.FileIO
{
  /// <summary>
  /// Indexes a file by line offsets and reads lines randomly. The file is
  /// memory-mapped read-only for both indexing and reading, so no buffer copy
  /// is involved and the memory usage stays flat for large files.
  /// </summary>
  public class RandomLineFileAccessor
  {
    private readonly List<long> _startLineIndex = new List<long>();
    private readonly string _filePath;
    private readonly ILogger _log;
    private bool _isFileIndexed;

    public RandomLineFileAccessor(string filePath, ILogger? log = null)
    {
      _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
      _log = log ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>Number of indexed lines.</summary>
    public int LineIndexed => _isFileIndexed ? Math.Max(0, _startLineIndex.Count - 1) : 0;

    public void IndexFile()
    {
      _startLineIndex.Clear();
      _startLineIndex.Add(0);

      if (new FileInfo(_filePath).Length == 0)
      {
        _isFileIndexed = true;
        return;
      }

      using var mappedFile = MemoryMappedFile.CreateFromFile(_filePath, FileMode.Open, null, 0,
        MemoryMappedFileAccess.Read);
      using var view = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

      int length = GetLength(view);

      unsafe
      {
        byte* pointer = null;
        view.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);

        try
        {
          var span = new ReadOnlySpan<byte>(pointer + (int)view.PointerOffset, length);

          int contentStart = 0;

          // Skip an optional UTF-8 byte order mark.
          if (span.Length >= 3 && span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF)
          {
            contentStart = 3;
            _startLineIndex[0] = 3;
          }

          for (int i = contentStart; i < span.Length; i++)
          {
            if (span[i] == (byte)'\n')
            {
              _startLineIndex.Add(i + 1);
            }
          }
        }
        finally
        {
          view.SafeMemoryMappedViewHandle.ReleasePointer();
        }
      }

      // Sentinel so the last line (with or without a trailing newline) has an end.
      if (_startLineIndex[^1] != length)
      {
        _startLineIndex.Add(length);
      }

      _isFileIndexed = true;
    }

    /// <summary>
    /// Returns up to <paramref name="len"/> lines starting at
    /// <paramref name="start"/> (zero-based).
    /// </summary>
    public string[] GetLines(int start, int len)
    {
      if (!_isFileIndexed || len < 0) return Array.Empty<string>();

      int total = LineIndexed;
      if (start < 0 || start >= total) return Array.Empty<string>();

      int count = Math.Min(len, total - start);
      if (count <= 0) return Array.Empty<string>();

      var result = new string[count];

      using var mappedFile = MemoryMappedFile.CreateFromFile(_filePath, FileMode.Open, null, 0,
        MemoryMappedFileAccess.Read);
      using var view = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

      int length = GetLength(view);

      unsafe
      {
        byte* pointer = null;
        view.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);

        try
        {
          var span = new ReadOnlySpan<byte>(pointer + (int)view.PointerOffset, length);

          for (int i = 0; i < count; i++)
          {
            int lineStart = checked((int)_startLineIndex[start + i]);
            int lineEnd = checked((int)_startLineIndex[start + i + 1]);

            if (lineEnd > lineStart && span[lineEnd - 1] == (byte)'\n')
            {
              lineEnd--;
            }

            if (lineEnd > lineStart && span[lineEnd - 1] == (byte)'\r')
            {
              lineEnd--;
            }

            result[i] = Encoding.UTF8.GetString(span[lineStart..lineEnd]);
          }
        }
        finally
        {
          view.SafeMemoryMappedViewHandle.ReleasePointer();
        }
      }

      return result;
    }

    private static int GetLength(MemoryMappedViewAccessor view)
    {
      if (view.Capacity > int.MaxValue)
      {
        throw new NotSupportedException("Files larger than 2 GB are not supported by this accessor.");
      }

      return (int)view.Capacity;
    }
  }
}
