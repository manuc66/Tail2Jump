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
    private ILog m_log = log4net.LogManager.GetLogger(typeof(VSTools));
    private readonly IVsAutomation m_vsAutomation;

    public VSTools(IVsAutomation vsAutomation_)
    {
      m_vsAutomation = vsAutomation_;
    }

    public void RefreshVSList(ThreadSafeObservableCollection<IOpenWithInfo> VSList, ref IOpenWithInfo OpenInVS)
    {
      if (VSList == null)
      {
        throw new ArgumentNullException("VSList");
      }
      List<VSInfo> tmpVSList = m_vsAutomation.GetAllVs();
      for (int i = 0; i < VSList.Count; ++i)
      {
        if (VSList[i] is VSInfo && !tmpVSList.Contains(VSList[i]))
        {
          if (OpenInVS == VSList[i])
          {
            OpenInVS = null;
          }
          VSList.RemoveAt(i);
        }
      }
      for (int i = 0; i < tmpVSList.Count; ++i)
      {
        int index = VSList.IndexOf(tmpVSList[i]);
        if (index < 0)
        {
          VSList.Add(tmpVSList[i]);
        }
        else
        {
          ((VSInfo)VSList[index]).Set(tmpVSList[i]);
        }
      }

      if (VSList.Count <= 0 || !(VSList[VSList.Count - 1] is NppOpenAt))
      {
        VSList.Add(new NppOpenAt());
      }

      if (OpenInVS == null)
      {
        bool debugFound = false;
        for (int i = 0; i < VSList.Count; ++i)
        {
          if (VSList[i] is VSInfo && ((VSInfo)VSList[i]).InDebug)
          {
            OpenInVS = VSList[i];
            debugFound = true;
            break;
          }
        }
        if (!debugFound)
        {
          OpenInVS = VSList.FirstOrDefault();
        }
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
        string[] fileInfo = new string[] { originalString.Substring(0, lastIndexDoubleDots), originalString.Substring(lastIndexDoubleDots + 1) };

        string filePath = fileInfo[0];
        string lineInfo = fileInfo[1];
        int lastSpaceIndex = lineInfo.LastIndexOf(' ');
        if (File.Exists(filePath) && lastSpaceIndex > 0 && lastSpaceIndex < originalString.Length - 1)
        {
          string strLineNum = lineInfo.Substring(lastSpaceIndex + 1);
          int lineNumber;
          if (int.TryParse(strLineNum, out lineNumber))
          {
            fileWithLineInfo = new FilePathWithLineInfo();
            fileWithLineInfo.FilePath = filePath;
            fileWithLineInfo.LineNumber = lineNumber;
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
