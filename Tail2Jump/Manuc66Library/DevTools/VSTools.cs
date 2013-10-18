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

      RemoveMissingVs(vsList, tmpVsList);

      UpdateExisting(vsList, tmpVsList);

      AddNewVs(vsList, tmpVsList);

      AddNppIfMissing(vsList);

      if (!vsList.Contains(openInVS))
      {
        openInVS = null;
      }

      if (openInVS == null)
      {
        openInVS = vsList.FirstOrDefault(IsInDebug) ?? vsList.FirstOrDefault();
      }
    }

    private static void RemoveMissingVs(IList<IOpenWithInfo> vsList, IEnumerable<VSInfo> tmpVsList)
    {
      var toRemove = vsList.Where(openWithInfo => openWithInfo is VSInfo && !tmpVsList.Contains(openWithInfo)).ToList();
      toRemove.ForEach(x => vsList.Remove(x));
    }

    private static void AddNppIfMissing(IList<IOpenWithInfo> vsList)
    {
      if (!vsList.Any(x => x is NppOpenAt))
      {
        vsList.Add(new NppOpenAt());
      }
    }

    private static void UpdateExisting(IList<IOpenWithInfo> vsList, IEnumerable<VSInfo> tmpVsList)
    {
      foreach (VSInfo vsInfo in tmpVsList)
      {
        var found = TryGet(vsList, vsInfo);

        if (found != null)
        {
          ((VSInfo) found).Set(vsInfo);
        }
      }
    }

    private static void AddNewVs(IList<IOpenWithInfo> vsList, IEnumerable<VSInfo> tmpVsList)
    {
      foreach (VSInfo vsInfo in tmpVsList)
      {
        var found = TryGet(vsList, vsInfo);

        if (found == null)
        {
          vsList.Add(vsInfo);
        }
      }
    }

    private static IOpenWithInfo TryGet(IList<IOpenWithInfo> vsList, VSInfo vsInfo)
    {
      int index = vsList.IndexOf(vsInfo);
      IOpenWithInfo found = index < 0 ? null : vsList[index];
      return found;
    }

    private static bool IsInDebug(IOpenWithInfo vsInfo)
    {
      return vsInfo is VSInfo && ((VSInfo)vsInfo).InDebug;
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

      return new FilePathWithLineInfo { FilePath = filePath, LineNumber = lineNumber.Value };
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
