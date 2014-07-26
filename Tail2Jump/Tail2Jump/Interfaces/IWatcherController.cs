using System.Collections.Generic;
using Be.Manuc66.Library.FileIO;
using Be.Manuc66.Library.WPF;
using Tai2JumpInfra;

namespace Tail2Jump.Interfaces
{
  public interface IWatcherController
  {
    ITailLineCache FileAcessor { get; }
    ThreadSafeObservableCollection<IMonitoringInstance> ListWatcher { get; }
    IMonitoringInstance CreateWatcher(string path = null);
    void RemoveWatcher(ICollection<IMonitoringInstance> toStart);
    void RemoveWatcher(IMonitoringInstance toRemove);
    bool Start(List<IMonitoringInstance> toStart);
    bool Start(IMonitoringInstance toStart);
    bool Stop(List<IMonitoringInstance> toStop);
    bool Stop(IMonitoringInstance toStop);
    void SetCurrentViewer(IList<IMonitoringInstance> toShow);
    void Clear();
    void ApplyFilter(string filter);
    void ResetFilter();
  }
}