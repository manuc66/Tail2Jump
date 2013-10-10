using System.Collections.Generic;
using Microsoft.Practices.Prism.Events;

namespace Tai2JumpInfra.Events
{
  public class MonitoringSelectionChanged : CompositePresentationEvent<IEnumerable<IMonitoringInstance>>
  {
  }
}
