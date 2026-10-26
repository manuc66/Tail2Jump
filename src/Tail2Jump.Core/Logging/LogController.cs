using System;
using Be.Manuc66.Library.Collections;
using Be.Manuc66.Library.Mvvm;
using Tail2Jump.Enums;
using Tail2Jump.Objects;

namespace Tail2Jump.Logging
{
  public interface ILogController
  {
    ThreadSafeObservableCollection<LogInstance> ListLogs { get; }
    void AddLog(LogType type, string text, Exception? exception = null);
  }

  public class LogController : ObservableObject, ILogController
  {
    private ThreadSafeObservableCollection<LogInstance> _listLogs;

    public ThreadSafeObservableCollection<LogInstance> ListLogs
    {
      get { return _listLogs; }
      set { SetProperty(ref _listLogs, value); }
    }

    public LogController()
    {
      _listLogs = new ThreadSafeObservableCollection<LogInstance>();
    }

    public void AddLog(LogType type, string text, Exception? exception = null)
    {
      var tempLog = new LogInstance(text, type);
      if (exception != null && type == LogType.Exception)
      {
        tempLog.Exception = exception;
      }

      if (_listLogs.Count > 100)
      {
        _listLogs.RemoveAt(100);
      }

      _listLogs.Insert(0, tempLog);
    }
  }
}
