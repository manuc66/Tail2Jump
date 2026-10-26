namespace Be.Manuc66.Library.FileIO
{
  public class TailEventData
  {
    public TailEventData(string? fileName, string? data, long fileSeekPos, uint byteLength)
    {
      FileName = fileName ?? string.Empty;
      NewLines = data ?? string.Empty;
      FileSeekPos = fileSeekPos;
      ByteLenght = byteLength;
    }

    public string FileName { get; private set; }
    public string NewLines { get; private set; }
    public long FileSeekPos { get; private set; }

    /// <summary>Kept with the original (misspelled) name for API compatibility.</summary>
    public uint ByteLenght { get; private set; }
  }
}
