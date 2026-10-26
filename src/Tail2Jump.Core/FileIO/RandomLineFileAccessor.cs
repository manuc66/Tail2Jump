using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Be.Manuc66.Library.FileIO
{
  /// <summary>
  /// Indexes a file by new-line offsets so lines can be read randomly.
  /// </summary>
  public class RandomLineFileAccessor
  {
    private const int MaxBytes = 1024 * 32; // 32 KB per read

    private readonly List<long> _startLineIndex = new List<long>();
    private readonly string _filePath;
    private readonly ILogger _log;
    private bool _isFileIndexed;

    private static readonly byte[] NewLineByteDef;

    static RandomLineFileAccessor()
    {
      NewLineByteDef = new UTF8Encoding().GetBytes(Environment.NewLine);
    }

    public RandomLineFileAccessor(string filePath, ILogger? log = null)
    {
      _filePath = filePath;
      _log = log ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    public int LineIndexed
    {
      get { return _isFileIndexed ? _startLineIndex.Count : 0; }
    }

    public void IndexFile()
    {
      _startLineIndex.Clear();
      _startLineIndex.Add(0);

      var bytesRead = new byte[MaxBytes];
      long totalRead = 0;

      using (FileStream fs = OpenFileShareMode(_filePath))
      {
        int numBytes;
        do
        {
          long windowReadStart = totalRead;
          numBytes = fs.Read(bytesRead, 0, MaxBytes);
          totalRead += numBytes;

          int jumpWhenFound = NewLineByteDef.Length - 1;
          for (int k = 0; k < numBytes; ++k)
          {
            int i = 0;
            for (; i < NewLineByteDef.Length && (k + i) < numBytes && bytesRead[k + i] == NewLineByteDef[i]; i++) { }

            if (i != NewLineByteDef.Length) continue;

            _startLineIndex.Add(windowReadStart + k + NewLineByteDef.Length);
            k += jumpWhenFound;
          }

          if (numBytes <= 0 || bytesRead[numBytes - 1] != NewLineByteDef[0]) continue;

          // Last byte matches the first byte of the new-line sequence:
          // read it again as the start of the next block.
          totalRead--;
          fs.Seek(totalRead, SeekOrigin.Begin);
        }
        while (numBytes > 0);
      }

      _isFileIndexed = true;
    }

    public string[] GetLines(int start, int len)
    {
      if (len == 0)
      {
        if (start == _startLineIndex.Count)
        {
          start--;
        }
        len++;
      }
      return ShowLines(start + 1, Math.Min(start + len, _startLineIndex.Count - 1));
    }

    public string[] ShowLines(int includingStart, int includingEnd)
    {
      if (!_isFileIndexed || includingStart <= 0 || includingEnd - includingStart < 0)
      {
        return new string[0];
      }

      long totalBytes = _startLineIndex[includingEnd] - _startLineIndex[includingStart - 1];
      var byteRequested = new byte[totalBytes];
      long dataBegin = _startLineIndex[includingStart - 1];
      int numBytes = 0;

      using (FileStream fs = OpenFileShareMode(_filePath))
      {
        if (dataBegin > 0)
        {
          fs.Seek(dataBegin, SeekOrigin.Begin);
        }

        int byteReadThisTime;
        do
        {
          byteReadThisTime = fs.Read(byteRequested, numBytes, byteRequested.Length - numBytes);
          numBytes += byteReadThisTime;
        }
        while (byteReadThisTime > 0 && numBytes < byteRequested.Length);
      }

      if (numBytes != byteRequested.Length)
      {
        _log.LogError("Failed to read {Expected} bytes from {FilePath} (got {Actual})",
          byteRequested.Length, _filePath, numBytes);
        return new string[0];
      }

      var result = new string[includingEnd - includingStart + 1];
      long offsetInFile = dataBegin;
      int offsetInArray = 0;
      for (int i = 0; i < result.Length; ++i)
      {
        var lineTotalBytes = (int)(_startLineIndex[includingStart + i] - offsetInFile);
        result[i] = Encoding.UTF8.GetString(byteRequested, offsetInArray, lineTotalBytes);
        offsetInFile += lineTotalBytes;
        offsetInArray += lineTotalBytes;
      }

      return result;
    }

    private static FileStream OpenFileShareMode(string filePath)
    {
      return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    }
  }
}
