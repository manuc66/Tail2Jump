using System;

namespace Tail2Jump.FileIO
{
  public interface ITailLineCache : IDisposable
  {
    string[] GetLines(int start, int len);

    /// <summary>
    /// Returns the raw (file name, content) entries for a page. Unlike
    /// <see cref="GetLines"/>, no "file|content" string is allocated.
    /// </summary>
    TailLineEntry[] GetEntries(int start, int len);

    void Clear();
    void AddData(TailEventData tailData, string? filter = null);
    void AddData(ITailLineCache other, string? filter);
    int LineIndexed { get; }

    /// <summary>Absolute index of the oldest line still kept in the cache.</summary>
    int FirstLineIndex { get; }
  }

  /// <summary>
  /// A cached log line: the file name is shared between all the lines of the
  /// same file, and the content is the raw line.
  /// </summary>
  public readonly struct TailLineEntry
  {
    public TailLineEntry(string fileName, string content)
    {
      FileName = fileName;
      Content = content;
    }

    public string FileName { get; }
    public string Content { get; }
  }
}
