using System;
using Be.Manuc66.Library;

namespace Tail2Jump.Interfaces
{
  public interface ILogController
  {
    ThreadSafeObservableCollection<LogInstance> ListLogs { get; set; }
    void AddLog(LogType type, string text, Exception exception = null);
  }
}