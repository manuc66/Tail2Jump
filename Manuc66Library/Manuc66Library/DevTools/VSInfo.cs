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
    private ILog m_log = log4net.LogManager.GetLogger(typeof(VSInfo));

    public VSInfo(string _SolutionPath, _DTE _Vs, string displayName, bool inDebug)
    {
      m_displayName = displayName;
      this.SolutionPath = _SolutionPath;
      this.Vs = _Vs;
      InDebug = inDebug;
    }
    public string SolutionPath
    {
      get { return m_SolutionPath; }
      set
      {
        if (m_SolutionPath != value) { m_SolutionPath = value; OnPropertyChanged("SolutionPath"); }
      }
    }
    public _DTE Vs
    {
      get { return m_Vs; }
      set
      {
        if (m_Vs != value) { m_Vs = value; OnPropertyChanged("Vs"); }
      }
    }
    public bool InDebug
    {
      get { return m_InDebug; }
      set
      {
        if (m_InDebug != value) { m_InDebug = value; OnPropertyChanged("InDebug"); }
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
        this.SolutionPath = other.SolutionPath;
      }
      this.Vs = other.Vs;
      this.InDebug = other.InDebug;
      this.m_displayName = other.m_displayName;
    }
    public int CompareTo(IOpenWithInfo other)
    {
      if (other == null) return -1;
      int result = TypeSortOrder.CompareTo(other.TypeSortOrder);
      if (result == 0)
      {
        if (other is VSInfo && m_displayName != null)
        {
          result = m_displayName.CompareTo(((VSInfo)other).m_displayName);
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
      return base.Equals(obj as IOpenWithInfo);
    }
    public override int GetHashCode()
    {
      return SolutionPath == null ? 0 : m_displayName.GetHashCode();
    }
    public bool OpenFileAtLine(FilePathWithLineInfo _filePathWithLineInfo)
    {
      bool sucess;
      if (_filePathWithLineInfo != null && _filePathWithLineInfo.LineNumber > 0)
      {

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
        if (sucess)
        {
          //Open up incident file and select given line
          bool isFileOk;
          try
          {
            EnvDTE.Window win = Vs.ItemOperations.OpenFile(_filePathWithLineInfo.FilePath);
            isFileOk = true;
          }
          catch (ArgumentException)
          {
            m_log.Info(string.Format("File not found {0}", _filePathWithLineInfo.FilePath));
            isFileOk = false;
            sucess = false;
          }
          var selection = Vs.ActiveDocument.Selection as EnvDTE.TextSelection;
          if (selection != null && isFileOk)
          {
            try
            {
              selection.GotoLine(_filePathWithLineInfo.LineNumber, true);
            }
            catch (ArgumentException)
            {
              m_log.Info(string.Format("Line {0} not found in file {1}", _filePathWithLineInfo.LineNumber, _filePathWithLineInfo.FilePath));
              sucess = false;
            }
          }
        }
      }
      else
      {
        throw new ArgumentException();
      }
      return sucess;
    }
  }
}
