using System;

namespace Be.Manuc66.Library.FileIO
{
  public interface ITailLineCache : IDisposable
  {
    string[] GetLines(int start, int len);
    void Clear();
    void AddData(TailEventData tailData, string? filter = null);
    void AddData(ITailLineCache other, string? filter);
    int LineIndexed { get; }
  }
}
