using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Be.Manuc66.Library.DevTools
{
  public class VSTools : IVSTools
  {
    private readonly IVsAutomation _vsAutomation;

    public VSTools(IVsAutomation vsAutomation)
    {
      _vsAutomation = vsAutomation;
    }

    public void RefreshVSList(IList<IOpenWithInfo> vsList, ref IOpenWithInfo openInVS)
    {
      if (vsList == null)
      {
        throw new ArgumentNullException("vsList");
      }
      List<VSInfo> tmpVsList = _vsAutomation.GetAllVs();
      for (int i = 0; i < vsList.Count; ++i)
      {
        if (!(vsList[i] is VSInfo) || tmpVsList.Contains(vsList[i])) continue;
        if (ReferenceEquals(openInVS, vsList[i]))
        {
          openInVS = null;
        }
        vsList.RemoveAt(i);
      }
      foreach (VSInfo vsInfo in tmpVsList)
      {
        int index = vsList.IndexOf(vsInfo);
        if (index < 0)
        {
          vsList.Add(vsInfo);
        }
        else
        {
          ((VSInfo)vsList[index]).Set(vsInfo);
        }
      }

      if (vsList.Count <= 0 || !(vsList[vsList.Count - 1] is NppOpenAt))
      {
        vsList.Add(new NppOpenAt());
      }

      if (openInVS != null) return;

      bool debugFound = false;
      foreach (var vsInfo in vsList.Where(vsInfo => vsInfo is VSInfo && ((VSInfo)vsInfo).InDebug))
      {
        openInVS = vsInfo;
        debugFound = true;
        break;
      }
      if (!debugFound)
      {
        openInVS = vsList.FirstOrDefault();
      }
    }

    public FilePathWithLineInfo GetFilePathWithLineNumber(string originalString)
    {
      if (string.IsNullOrEmpty(originalString))
      {
        return null;
      }

      var fileInfo = GetGoToLineInfo(originalString);
      if (fileInfo == null) 
        return null;

      string filePath = fileInfo.FilePath;
      int? lineNumber = fileInfo.GetLineNumber();

      if (!lineNumber.HasValue || !File.Exists(filePath))
      {
        return null;
      }

      return new FilePathWithLineInfo {FilePath = filePath, LineNumber = lineNumber.Value};
    }

    private static FileGotoLineInfo GetGoToLineInfo(string originalString)
    {
      int lastIndexDoubleDots = originalString.LastIndexOf(':');
      if (lastIndexDoubleDots <= 0 || lastIndexDoubleDots >= originalString.Length - 1)
      {
        return null;
      }
      var fileInfo = new FileGotoLineInfo(originalString.Substring(0, lastIndexDoubleDots),
        originalString.Substring(lastIndexDoubleDots + 1));
      return fileInfo;
    }
  }
}
