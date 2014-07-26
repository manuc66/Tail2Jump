namespace Tai2JumpInfra.MonitoringInstanceStates
{
  public class StoppedMonitoringInstance : MonitoringInstanceState
  {
    public override bool CanStop { get { return false; } }
    public override bool CanStart { get { return true; } }
  }
}