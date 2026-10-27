using Tail2Jump.Mvvm;
using Tail2Jump.Monitoring;

namespace Tail2Jump.Objects
{
  public class MonitoringInstance : ObservableObject, IMonitoringInstance
  {
    private MonitoringInstanceState _state = new StoppedMonitoringInstance();
    private bool _includeSubdirectories;
    private string? _titleName;
    private string? _fullDirectoryPath;
    private int _tailerId;
    private string? _filter;

    public MonitoringInstanceState State
    {
      get { return _state; }
      set
      {
        if (SetProperty(ref _state, value))
        {
          OnPropertyChanged(nameof(Status));
        }
      }
    }

    public string Status => _state is StartedMonitoringInstance ? "Started" : "Stopped";

    public bool IncludeSubdirectories
    {
      get { return _includeSubdirectories; }
      set { SetProperty(ref _includeSubdirectories, value); }
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
