using System;
using EnvDTE;
using log4net;

namespace Be.Manuc66.Library.DevTools
{
  public class VSInfo : ViewModelBase, IOpenWithInfo
  {
    private string _solutionPath;
    private _DTE _vs;
    private bool _inDebug;
    private string _displayName;
    private const int s_TypeSortOrder = 1;
    private readonly ILog _log = LogManager.GetLogger(typeof(VSInfo));

    public VSInfo(string solutionPath, _DTE vs, string displayName, bool inDebug)
    {
      _displayName = displayName;
      SolutionPath = solutionPath;
      Vs = vs;
      InDebug = inDebug;
    }
    public string SolutionPath
    {
      get { return _solutionPath; }
      set
      {
        if (_solutionPath == value) return;
        _solutionPath = value;
        OnPropertyChanged("SolutionPath");
      }
    }
    public _DTE Vs
    {
      get { return _vs; }
      set
      {
        if (_vs == value) return;
        _vs = value; OnPropertyChanged("Vs");
      }
    }
    public bool InDebug
    {
      get { return _inDebug; }
      set
      {
        if (_inDebug == value) return;
        _inDebug = value; OnPropertyChanged("InDebug");
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
      _displayName = other._displayName;
    }
    public int CompareTo(IOpenWithInfo other)
    {
      if (other == null) return -1;
      int result = TypeSortOrder.CompareTo(other.TypeSortOrder);
      if (result != 0) return result;
      if (other is VSInfo && _displayName != null)
      {
        result = String.Compare(_displayName, ((VSInfo)other)._displayName, StringComparison.Ordinal);
      }
      else
      {
        result = -1;
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
      return SolutionPath == null ? 0 : _displayName.GetHashCode();
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
        _log.Info(string.Format("The visual studio loaded on \"{0}\" is not responding ", SolutionPath));
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
        _log.Info(string.Format("Line {0} not found in file {1}", filePathWithLineInfo.LineNumber,
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
        _log.Info(string.Format("File not found {0}", filePathWithLineInfo.FilePath));
        isFileOk = false;
      }
      return isFileOk;
    }
  }
}
