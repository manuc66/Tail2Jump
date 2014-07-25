using System;
using Be.Manuc66.Library;
using Be.Manuc66.Library.WPF;
using Tail2Jump.Enums;
using Tail2Jump.Objects;

namespace Tail2Jump.Interfaces
{
  public interface ILogController
  {
    ThreadSafeObservableCollection<LogInstance> ListLogs { get; set; }
    void AddLog(LogType type, string text, Exception exception = null);
  }
}