
using System.Diagnostics;
using log4net;

namespace Be.Manuc66.Library.Log
{

  public class Log4NetTraceListener : TraceListener
  {
    private readonly ILog _log;

    public Log4NetTraceListener()
    {
      _log = LogManager.GetLogger("System.Diagnostics.Redirection");
    }

    public Log4NetTraceListener(ILog log)
    {
      _log = log;
    }

    public override void Write(string message)
    {
      if (_log != null)
      {
        _log.Debug(message);
      }
    }

    public override void WriteLine(string message)
    {
      if (_log != null)
      {
        _log.Debug(message);
      }
    }
  }
}
