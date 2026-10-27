using System.ComponentModel;
using Tail2Jump.Monitoring;

namespace Tail2Jump.Monitoring
{
  public interface IMonitoringInstance : INotifyPropertyChanged
  {
    MonitoringInstanceState State { get; set; }
    bool IncludeSubdirectories { get; set; }
    string? TitleName { get; set; }
    string? FullDirectoryPath { get; set; }
    int TailerId { get; set; }
    string? Filter { get; set; }
    string Status { get; }
    void SetDisplayedName();
  }
}
