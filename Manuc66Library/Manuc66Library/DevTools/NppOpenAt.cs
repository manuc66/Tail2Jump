// -----------------------------------------------------------------------
// <copyright file="NppOpenAt.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using Be.Manuc66.Library.DevTools;

namespace Be.Manuc66.Library
{

  /// <summary>
  /// TODO: Update summary.
  /// </summary>
  public class NppOpenAt : ViewModelBase, IOpenWithInfo
  {
    private const int s_TypeSortOrder = 2;
    private static string s_nppLocation = null;

    public static bool IsNppAvailble()
    {
      bool result;
      if (s_nppLocation == null)
      {
        var possibleLocation = new[] {
        @"C:\Program Files (x86)\Notepad++\notepad++.exe" ,
        @"C:\Program Files)\Notepad++\notepad++.exe" ,
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\notepad++.exe",
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\notepad++.exe"
        };
        result = false;
        foreach (var item in possibleLocation)
        {
          if (File.Exists(item))
          {
            s_nppLocation = item;
            result = true;
            break;
          }
        }
      }
      else
      {
        result = true;
      }
      return result;
    }

    public int TypeSortOrder
    {
      get { return s_TypeSortOrder; }
    }
    public string DisplayName { get { return "Notepad++"; } }

    public bool OpenFileAtLine(FilePathWithLineInfo filePathWithLineInfo_)
    {
      if (filePathWithLineInfo_ == null) return false;
      bool result;
      if (IsNppAvailble())
      {
        Process.Start(s_nppLocation, "\"" + filePathWithLineInfo_.FilePath + "\" -n" + filePathWithLineInfo_.LineNumber);
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
      if (result == 0)
      {
        if (other is NppOpenAt)
        {
          result = 0;
        }
        else
        {
          result = -1;
        }
      }
      return result;
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
