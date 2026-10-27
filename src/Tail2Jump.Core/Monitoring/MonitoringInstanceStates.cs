namespace Tail2Jump.Monitoring
{
  public abstract class MonitoringInstanceState
  {
    public abstract bool CanStop { get; }
    public abstract bool CanStart { get; }
  }

  public class PausedMonitoringInstance : MonitoringInstanceState
  {
    public override bool CanStop { get { return true; } }
    public override bool CanStart { get { return true; } }
  }

  public class StartedMonitoringInstance : MonitoringInstanceState
  {
    public override bool CanStop { get { return true; } }
    public override bool CanStart { get { return false; } }
  }

  public class StoppedMonitoringInstance : MonitoringInstanceState
  {
    public override bool CanStop { get { return false; } }
    public override bool CanStart { get { return true; } }
  }
}
