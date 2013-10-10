using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Be.Manuc66.Library.DevTools;
using log4net;

namespace Be.Manuc66.Library
{
  public class VSTools : IVSTools
  {
    private ILog m_log = LogManager.GetLogger(typeof(VSTools));
    private readonly IVsAutomation m_vsAutomation;

    public VSTools(IVsAutomation vsAutomation_)
    {
      m_vsAutomation = vsAutomation_;
    }

    public void RefreshVSList(ThreadSafeObservableCollection<IOpenWithInfo> vsList, ref IOpenWithInfo openInVS)
    {
      if (vsList == null)
      {
        throw new ArgumentNullException("vsList");
      }
      List<VSInfo> tmpVsList = m_vsAutomation.GetAllVs();
      for (int i = 0; i < vsList.Count; ++i)
      {
        if (!(vsList[i] is VSInfo) || tmpVsList.Contains(vsList[i])) continue;
        if (openInVS == vsList[i])
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
      FilePathWithLineInfo fileWithLineInfo;

      if (originalString == null)
      {
        originalString = string.Empty;
      }

      int lastIndexDoubleDots = originalString.LastIndexOf(':');
      if (lastIndexDoubleDots > 0 && lastIndexDoubleDots < originalString.Length - 1)
      {
        var fileInfo = new[] { originalString.Substring(0, lastIndexDoubleDots), originalString.Substring(lastIndexDoubleDots + 1) };

        string filePath = fileInfo[0];
        string lineInfo = fileInfo[1];
        int lastSpaceIndex = lineInfo.LastIndexOf(' ');
        if (File.Exists(filePath) && lastSpaceIndex > 0 && lastSpaceIndex < originalString.Length - 1)
        {
          string strLineNum = lineInfo.Substring(lastSpaceIndex + 1);
          int lineNumber;
          fileWithLineInfo = int.TryParse(strLineNum, out lineNumber) ? new FilePathWithLineInfo { FilePath = filePath, LineNumber = lineNumber } : null;
        }
        else
        {
          fileWithLineInfo = null;
        }
      }
      else
      {
        fileWithLineInfo = null;
      }
      return fileWithLineInfo;
    }
  }
  public class FilePathWithLineInfo
  {
    public string FilePath { get; set; }
    public int LineNumber { get; set; }
  }
}
