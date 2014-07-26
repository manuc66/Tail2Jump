namespace Tai2JumpInfra.MonitoringInstanceStates
{
  public class StartedMonitoringInstance : MonitoringInstanceState
  {
    public override bool CanStop { get { return true; } }
    public override bool CanStart { get { return false; } }
  }
}