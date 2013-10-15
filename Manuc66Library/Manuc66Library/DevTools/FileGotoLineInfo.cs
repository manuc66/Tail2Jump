namespace Be.Manuc66.Library
{
  public class FileGotoLineInfo
  {
    private readonly string _filePath;
    private readonly string _lineInfo;

    public string FilePath
    {
      get { return _filePath; }
    }

    public FileGotoLineInfo(string filePath, string lineInfo)
    {
      _filePath = filePath;
      _lineInfo = lineInfo;
    }

    public int? GetLineNumber()
    {
      string lineInfo = _lineInfo;
      int lastSpaceIndex = lineInfo.LastIndexOf(' ');

      if (lastSpaceIndex <= 0)
      {
        return null;
      }

      string strLineNum = lineInfo.Substring(lastSpaceIndex + 1);
      int lineNumber;
      if (!int.TryParse(strLineNum, out lineNumber))
        return null;

      return lineNumber;
    }
  }
}