using System;
using Be.Manuc66.Library;
using Tail2Jump.Interfaces;

namespace Tail2Jump
{
  public class LogController : ViewModelBase, ILogController
  {
    #region Properties
    public ThreadSafeObservableCollection<LogInstance> ListLogs 
    {
      get { return _listLogs; }
      set
      {
        if (_listLogs != value)
        {
          _listLogs = value;
          OnPropertyChanged("ListLogs");
        }
      }
    }
    #endregion // Properties
    
    #region Members 
    private ThreadSafeObservableCollection<LogInstance> _listLogs;
    #endregion // Members

    public LogController()
    {
      _listLogs = new ThreadSafeObservableCollection<LogInstance>();
    }

    public void AddLog(LogType type, string text, Exception exception = null)
    {
      var tempLog = new LogInstance(text,type);
      if (exception != null && type == LogType.Exception)
      {
        tempLog.Exception = exception;
      }

      if (_listLogs.Count > 100)
      {
        _listLogs.RemoveAt(100);
      }

      _listLogs.Insert(0,tempLog);
    }
  }
}
