using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;

namespace PartialView
{
  public class PartialViewModule : IModule
  {
    private readonly IRegionViewRegistry _regionViewRegistry;
    private readonly IUnityContainer _container;

    public PartialViewModule(IUnityContainer container, IRegionViewRegistry registry)
    {
      _regionViewRegistry = registry;
      _container = container;
    }

    public void Initialize()
    {
      _regionViewRegistry.RegisterViewWithRegion("PartialViewRegion", () => _container.Resolve<IPartialView>());
    }
  }
}
