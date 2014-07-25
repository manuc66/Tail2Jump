using System.ComponentModel;
using System.Windows.Controls;

namespace Tai2JumpInfra
{
  // A développer
  public interface IMonitoringInstance
  {
    bool IsOnScreen { get; set; }
    MonitoringInstanceStatus Status { get; set; }
    bool IncludeSubdirectories { get; set; }
    bool Notified { get; set; }
    string TitleName { get; set; }
    string FullDirectoryPath { get; set; }
    Image Picture { get; set; }
    int TailerId { get; set; }
    string Filter { get; set; }
    void SetDisplayedName();
    event PropertyChangedEventHandler PropertyChanged;
  }
}