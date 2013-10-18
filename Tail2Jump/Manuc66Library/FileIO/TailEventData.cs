namespace Be.Manuc66.Library.FileIO
{
  public class TailEventData
  {
    public TailEventData(string fileName_, string data_, long fileSeekPos_, uint byteLenght_)
    {
      FileName = fileName_;
      NewLines = data_;
      FileSeekPos = fileSeekPos_;
      ByteLenght = byteLenght_;
    }

    public string FileName { get; private set; }
    public string NewLines { get; private set; }
    public long FileSeekPos { get; private set; }
    public uint ByteLenght { get; private set; }
  }
}