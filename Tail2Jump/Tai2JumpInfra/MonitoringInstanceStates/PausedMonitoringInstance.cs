namespace Tai2JumpInfra.MonitoringInstanceStates
{
  public class PausedMonitoringInstance : MonitoringInstanceState
  {
    public override bool CanStop { get { return true; } }
    public override bool CanStart { get { return true; } }
  }
}