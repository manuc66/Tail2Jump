using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Be.Manuc66.Library.WPF;

namespace Be.Manuc66.Library.DevTools
{
  public class NppOpenAt : ViewModelBase, IOpenWithInfo
  {
    private const int s_TypeSortOrder = 2;
    private static string s_nppLocation;

    public static bool IsNppAvailble()
    {
      bool result;
      if (s_nppLocation == null)
      {
        var possibleLocation = PossibleNppLocations();
        s_nppLocation = possibleLocation.Where(File.Exists).FirstOrDefault();
        result = s_nppLocation != null;
      }
      else
      {
        result = true;
      }
      return result;
    }

    private static IEnumerable<string> PossibleNppLocations()
    {
      var possibleLocation = new[]
      {
        @"C:\Program Files (x86)\Notepad++\notepad++.exe",
        @"C:\Program Files)\Notepad++\notepad++.exe",
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\notepad++.exe",
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\notepad++.exe"
      };
      return possibleLocation;
    }

    public int TypeSortOrder
    {
      get { return s_TypeSortOrder; }
    }
    public string DisplayName { get { return "Notepad++"; } }

    public bool OpenFileAtLine(FilePathWithLineInfo filePathWithLineInfo)
    {
      if (filePathWithLineInfo == null) return false;
      bool result;
      if (IsNppAvailble())
      {
        Process.Start(s_nppLocation, string.Format("\"{0}\" -n{1}", filePathWithLineInfo.FilePath, filePathWithLineInfo.LineNumber));
        result = true;
      }
      else
      {
        result = false;
      }
      return result;
    }


    public int CompareTo(IOpenWithInfo other)
    {
      if (other == null) return -1;
      int result = TypeSortOrder.CompareTo(other.TypeSortOrder);
      if (result != 0) return result;
      return other is NppOpenAt ? 0 : -1;
    }
    public bool Equals(IOpenWithInfo other)
    {
      return other != null && TypeSortOrder == other.TypeSortOrder && CompareTo(other) == 0;
    }
    public override bool Equals(object obj)
    {
      return ReferenceEquals(this, obj);
    }
    public override int GetHashCode()
    {
      return GetType().GetHashCode();
    }
  }
}
