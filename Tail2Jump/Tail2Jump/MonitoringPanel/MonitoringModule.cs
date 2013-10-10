using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;

namespace Tail2Jump
{
  class MonitoringModule : IModule
  {
    private readonly IRegionViewRegistry _regionViewRegistry;
    private readonly IUnityContainer _container;

    public MonitoringModule(IUnityContainer container_, IRegionViewRegistry registry_)
    {
      _regionViewRegistry = registry_;
      _container = container_;
    }

    public void Initialize()
    {
      _container.RegisterType<IMonitoringPanelW8, MonitoringPanelW8>();
      _regionViewRegistry.RegisterViewWithRegion("MonitoringPanel", () => _container.Resolve<IMonitoringPanelW8>());
    }
  }
}
