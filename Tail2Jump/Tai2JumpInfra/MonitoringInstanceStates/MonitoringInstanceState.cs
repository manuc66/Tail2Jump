namespace Tai2JumpInfra.MonitoringInstanceStates
{
  public abstract class MonitoringInstanceState
  {
    public abstract bool CanStop { get; }
    public abstract bool CanStart { get; }
  }
}