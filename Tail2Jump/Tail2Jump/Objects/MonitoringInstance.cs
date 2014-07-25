using System.Windows.Controls;
using Be.Manuc66.Library;
using Tai2JumpInfra;

namespace Tail2Jump
{
  public class MonitoringInstance : ViewModelBase, IMonitoringInstance
  {
    #region Members
    private bool _isOnScreen;
    private MonitoringInstanceStatus _status;
    private bool _includeSubdirectories;
    private bool _notified;
    private string _titleName;
    private string _fullDirectoryPath;
    private Image _picture;
    private int _tailerId;
    private string _filter;
    #endregion // Members

    #region Properties
    public bool IsOnScreen
    {
      get { return _isOnScreen; }
      set
      {
        if (_isOnScreen != value)
        {
          _isOnScreen = value;
          OnPropertyChanged("IsOnScreen");
        }
      }
    }
    public MonitoringInstanceStatus Status
    {
      get { return _status; }
      set
      {
        if (_status != value)
        {
          _status = value;
          OnPropertyChanged("Status");
        }
      }
    }
    public bool IncludeSubdirectories
    {
      get { return _includeSubdirectories; }
      set
      {
        if (_includeSubdirectories != value)
        {
          _includeSubdirectories = value;
          OnPropertyChanged("IncludeSubdirectories");
        }
      }
    }
    public bool Notified
    {
      get { return _notified; }
      set
      {
        if (_notified != value)
        {
          _notified = value;
          OnPropertyChanged("Notified");
        }
      }
    }
    public string TitleName
    {
      get { return _titleName; }
      set
      {
        if (_titleName != value)
        {
          _titleName = value;
          OnPropertyChanged("TitleName");
        }
      }
    }
    public string FullDirectoryPath
    {
      get { return _fullDirectoryPath; }
      set
      {
        if (_fullDirectoryPath != value)
        {
          _fullDirectoryPath = value;
          OnPropertyChanged("FullDirectoryPath");
        }
      }
    }
    public Image Picture
    {
      get { return _picture; }
      set
      {
        if (_picture != value)
        {
          _picture = value;
          OnPropertyChanged("Picture");
        }
      }
    }
    public int TailerId
    {
      get { return _tailerId; }
      set
      {
        if (_tailerId != value)
        {
          _tailerId = value;
          OnPropertyChanged("TailerId");
        }
      }
    }
    public string Filter
    {
      get { return _filter; }
      set
      {
        if (_filter != value)
        {
          _filter = value;
          OnPropertyChanged("Filter");
        }
      }
    }
    #endregion // properties


    #region Methods
    public void SetDisplayedName()
    {
      TitleName = _fullDirectoryPath + "  " + _filter;
    }
    #endregion // Methods

  }
}
