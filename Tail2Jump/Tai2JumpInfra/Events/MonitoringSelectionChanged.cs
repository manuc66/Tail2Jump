using System.Collections.Generic;
using Microsoft.Practices.Prism.PubSubEvents;

namespace Tai2JumpInfra.Events
{
  public class MonitoringSelectionChanged : PubSubEvent<IEnumerable<IMonitoringInstance>>
  {
  }
}
