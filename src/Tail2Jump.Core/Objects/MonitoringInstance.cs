using Be.Manuc66.Library.Mvvm;
using Tai2JumpInfra;
using Tai2JumpInfra.MonitoringInstanceStates;

namespace Tail2Jump.Objects
{
  public class MonitoringInstance : ObservableObject, IMonitoringInstance
  {
    private bool _isOnScreen;
    private MonitoringInstanceState _state = new StoppedMonitoringInstance();
    private bool _includeSubdirectories;
    private bool _notified;
    private string? _titleName;
    private string? _fullDirectoryPath;
    private int _tailerId;
    private string? _filter;

    public bool IsOnScreen
    {
      get { return _isOnScreen; }
      set { SetProperty(ref _isOnScreen, value); }
    }

    public MonitoringInstanceState State
    {
      get { return _state; }
      set
      {
        if (SetProperty(ref _state, value))
        {
          OnPropertyChanged(nameof(Status));
          OnPropertyChanged(nameof(IsStarted));
        }
      }
    }

    public bool IsStarted => _state is StartedMonitoringInstance;

    public string Status
    {
      get
      {
        if (_state is StartedMonitoringInstance) return "Started";
        if (_state is PausedMonitoringInstance) return "Paused";
        return "Stopped";
      }
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

    public string? TitleName
    {
      get { return _titleName; }
      set { SetProperty(ref _titleName, value); }
    }

    public string? FullDirectoryPath
    {
      get { return _fullDirectoryPath; }
      set { SetProperty(ref _fullDirectoryPath, value); }
    }

    public int TailerId
    {
      get { return _tailerId; }
      set { SetProperty(ref _tailerId, value); }
    }

    public string? Filter
    {
      get { return _filter; }
      set { SetProperty(ref _filter, value); }
    }

    public void SetDisplayedName()
    {
      TitleName = _fullDirectoryPath + "  " + _filter;
    }
  }
}
