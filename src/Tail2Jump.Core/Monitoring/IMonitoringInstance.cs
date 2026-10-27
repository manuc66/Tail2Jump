using System.ComponentModel;
using Tail2Jump.Monitoring;

namespace Tail2Jump.Monitoring
{
  public interface IMonitoringInstance : INotifyPropertyChanged
  {
    bool IsOnScreen { get; set; }
    MonitoringInstanceState State { get; set; }
    bool IncludeSubdirectories { get; set; }
    bool Notified { get; set; }
    string? TitleName { get; set; }
    string? FullDirectoryPath { get; set; }
    int TailerId { get; set; }
    string? Filter { get; set; }
    string Status { get; }
    bool IsStarted { get; }
    void SetDisplayedName();
  }
}
