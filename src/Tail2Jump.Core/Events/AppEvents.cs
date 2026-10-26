using System.Collections.Generic;
using Be.Manuc66.Library.Events;

namespace Tai2JumpInfra.Events
{
  public enum WatcherAction
  {
    Start,
    Stop
  }

  public class WatcherActionAsk : PubSubEvent<WatcherAction>
  {
  }

  public class MonitoringSelectionChanged : PubSubEvent<IEnumerable<IMonitoringInstance>>
  {
  }

  public class OpenInChanged : PubSubEvent<Be.Manuc66.Library.DevTools.IOpenWithInfo>
  {
  }

  public class SearchActivationStatusChanged : PubSubEvent<bool>
  {
  }

  public class SearchInfoChanged : PubSubEvent<string>
  {
  }
}
