using System.ComponentModel;
using Be.Manuc66.Library.DevTools;
using EnvDTE;
using System;
using log4net;

namespace Be.Manuc66.Library
{
  public class VSInfo : ViewModelBase, INotifyPropertyChanged, IOpenWithInfo
  {
    private string m_SolutionPath;
    private _DTE m_Vs;
    private bool m_InDebug;
    private string m_displayName;
    private const int s_TypeSortOrder = 1;
    private readonly ILog m_log = LogManager.GetLogger(typeof(VSInfo));

    public VSInfo(string _SolutionPath, _DTE _Vs, string displayName, bool inDebug)
    {
      m_displayName = displayName;
      SolutionPath = _SolutionPath;
      Vs = _Vs;
      InDebug = inDebug;
    }
    public string SolutionPath
    {
      get { return m_SolutionPath; }
      set
      {
        if (m_SolutionPath == value) return;
        m_SolutionPath = value;
        OnPropertyChanged("SolutionPath");
      }
    }
    public _DTE Vs
    {
      get { return m_Vs; }
      set
      {
        if (m_Vs == value) return;
        m_Vs = value; OnPropertyChanged("Vs");
      }
    }
    public bool InDebug
    {
      get { return m_InDebug; }
      set
      {
        if (m_InDebug == value) return;
        m_InDebug = value; OnPropertyChanged("InDebug");
      }
    }
    public int TypeSortOrder { get { return s_TypeSortOrder; } }
    public string DisplayName { get { return SolutionPath; } }

    public void Set(VSInfo other)
    {
      if (other == null)
      {
        throw new ArgumentNullException("other");
      }
      if (other.SolutionPath != VsAutomation.UnknownSolution || string.IsNullOrWhiteSpace(SolutionPath))
      {
        SolutionPath = other.SolutionPath;
      }
      Vs = other.Vs;
      InDebug = other.InDebug;
      m_displayName = other.m_displayName;
    }
    public int CompareTo(IOpenWithInfo other)
    {
      if (other == null) return -1;
      int result = TypeSortOrder.CompareTo(other.TypeSortOrder);
      if (result == 0)
      {
        if (other is VSInfo && m_displayName != null)
        {
          result = String.Compare(m_displayName, ((VSInfo)other).m_displayName, StringComparison.Ordinal);
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
      return SolutionPath == null ? 0 : m_displayName.GetHashCode();
    }
    public bool OpenFileAtLine(FilePathWithLineInfo filePathWithLineInfo)
    {
      if (filePathWithLineInfo == null || filePathWithLineInfo.LineNumber <= 0) throw new ArgumentException();

      //Open up incident file and select given line
      return BringToFront() && OpenFile(filePathWithLineInfo) && GotoLine(filePathWithLineInfo);
    }

    private bool BringToFront()
    {
      bool sucess;
      try
      {
        //Make it visible, and stay under user control, not close when this app close
        Vs.MainWindow.Visible = true;
        Vs.UserControl = true;
        sucess = true;
      }
      catch (Exception)
      {
        m_log.Info(string.Format("The visual studio loaded on \"{0}\" is not responding ", SolutionPath));
        sucess = false;
      }
      return sucess;
    }

    private bool GotoLine(FilePathWithLineInfo filePathWithLineInfo)
    {
      var selection = Vs.ActiveDocument.Selection as TextSelection;

      if (selection == null) return true;
      try
      {
        selection.GotoLine(filePathWithLineInfo.LineNumber, true);
        return true;
      }
      catch (ArgumentException)
      {
        m_log.Info(string.Format("Line {0} not found in file {1}", filePathWithLineInfo.LineNumber,
          filePathWithLineInfo.FilePath));

        return false;
      }
    }

    private bool OpenFile(FilePathWithLineInfo filePathWithLineInfo)
    {
      bool isFileOk;
      try
      {
        Vs.ItemOperations.OpenFile(filePathWithLineInfo.FilePath);
        isFileOk = true;
      }
      catch (ArgumentException)
      {
        m_log.Info(string.Format("File not found {0}", filePathWithLineInfo.FilePath));
        isFileOk = false;
      }
      return isFileOk;
    }
  }
}
