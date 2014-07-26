using System.Windows.Controls;
using Microsoft.Practices.Prism.Mvvm;
using Tai2JumpInfra;

namespace Tail2Jump.Objects
{
  public class MonitoringInstance : BindableBase, IMonitoringInstance
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
      set { SetProperty(ref _isOnScreen, value); }
    }
    public MonitoringInstanceStatus Status
    {
      get { return _status; }
      set { SetProperty(ref _status, value); }
    }
    public bool IncludeSubdirectories
    {
      get { return _includeSubdirectories; }
      set { SetProperty(ref _includeSubdirectories, value); }
    }
    public bool Notified
    {
      get { return _notified; }
      set { SetProperty(ref _notified, value); }
    }
    public string TitleName
    {
      get { return _titleName; }
      set { SetProperty(ref _titleName, value); }
    }
    public string FullDirectoryPath
    {
      get { return _fullDirectoryPath; }
      set { SetProperty(ref _fullDirectoryPath, value); }
    }
    public Image Picture
    {
      get { return _picture; }
      set { SetProperty(ref _picture, value); }
    }
    public int TailerId
    {
      get { return _tailerId; }
      set { SetProperty(ref _tailerId, value); }
    }
    public string Filter
    {
      get { return _filter; }
      set { SetProperty(ref _filter, value); }
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
