using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Be.Manuc66.Library.FileIO
{
  public class RandomLineFileAccessor
  {
    #region Variables
    bool _isFileIndexed;
    private readonly List<long> _startLineIndex = new List<long>();
    readonly string _filePath;

    private const int MaxBytes = 1024 * 32; //32 Ko block read at a time
    private static readonly byte[] NewLineByteDef;
    #endregion

    #region Properties
    public int LineIndexed
    {
      get
      {
        return _isFileIndexed ? _startLineIndex.Count : 0;
      }
    }
    #endregion


    static RandomLineFileAccessor()
    {
      var encoding = new UTF8Encoding();
      NewLineByteDef = encoding.GetBytes(Environment.NewLine);
    }

    public RandomLineFileAccessor(string filePath)
    {
      _filePath = filePath;
    }

    public void IndexFile()
    {
      var bytesRead = new byte[MaxBytes];
      long totalRead = 0;
      _startLineIndex.Add(0);
      using (FileStream fs = OpenFileShareMode(_filePath))
      {
        int numBytes;
        do
        {
          long windowReadStart = totalRead;
          numBytes = fs.Read(bytesRead, 0, MaxBytes);
          totalRead += numBytes;
          int jumpWenFound = NewLineByteDef.Length - 1;
          for (int k = 0; k < numBytes; ++k)
          {
            int i = 0;
            for (; i < NewLineByteDef.Length && (k + i) < bytesRead.Length && bytesRead[k + i] == NewLineByteDef[i]; i++) { }
            if (i != NewLineByteDef.Length) continue;
            long found = windowReadStart + k + NewLineByteDef.Length;
            k += jumpWenFound;
            _startLineIndex.Add(found);
          }
          if (numBytes <= 0 || bytesRead[numBytes - 1] != NewLineByteDef[0]) continue;
          // if last byte match, take this byte as the first byte of the next readed block
          totalRead--;
          fs.Seek(totalRead, SeekOrigin.Begin);
        }
        while (numBytes > 0); // while read success!
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
      return ShowLines(start + 1, Math.Min((start + len), (_startLineIndex.Count - 1)));
    }

    public string[] ShowLines(int incudingStart, int incudingEnd)
    {
      string[] result;
      if (_isFileIndexed && incudingStart > 0 && incudingEnd - incudingStart >= 0)
      {
        long totalbyte = (_startLineIndex[incudingEnd] - _startLineIndex[incudingStart - 1]);
        var byteRequested = new byte[totalbyte];
        long dataBegin = _startLineIndex[incudingStart - 1];
        int numBytes = 0;
        using (FileStream fs = OpenFileShareMode(_filePath))
        {
          if (dataBegin > 0) // not necessary but maybe reduce IO....
          {
            fs.Seek(dataBegin, SeekOrigin.Begin);
          }

          int byteReadedThisTime;
          do
          {
            byteReadedThisTime = fs.Read(byteRequested, numBytes, byteRequested.Length);
            numBytes += byteReadedThisTime;
          }
          while (byteReadedThisTime < 0 && numBytes < byteRequested.Length);
        }

        if (numBytes == byteRequested.Length)
        {
          result = new string[incudingEnd - incudingStart + 1];
          long offsetInFile = dataBegin;
          int offsetInArray = 0;
          for (int i = 0; i < result.Length; ++i)
          {
            var lineTotalBytes = (int)(_startLineIndex[incudingStart + i] - offsetInFile);
            result[i] = Encoding.UTF8.GetString(byteRequested, offsetInArray, lineTotalBytes);
            offsetInFile += lineTotalBytes;
            offsetInArray += lineTotalBytes;
          }
        }
        else
        {
          result = new string[0];
          Debugger.Break();
        }
      }
      else
      {
        result = new string[0];
        Debugger.Break();
      }
      return result;
    }
    private static FileStream OpenFileShareMode(string filePath)
    {
      return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    }
  }
}
