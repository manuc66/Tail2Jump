namespace Be.Manuc66.Library.DevTools
{
  public interface IVSTools
  {
    FilePathWithLineInfo GetFilePathWithLineNumber(string originalString);
    void RefreshVSList(ThreadSafeObservableCollection<IOpenWithInfo> vsList, ref IOpenWithInfo openInVS);
  }
}
