using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Be.Manuc66.Library.DevTools
{
  public interface IVSTools
  {
    FilePathWithLineInfo GetFilePathWithLineNumber(string originalString);
    void RefreshVSList(IList<IOpenWithInfo> vsList, ref IOpenWithInfo openInVS);
  }
}
