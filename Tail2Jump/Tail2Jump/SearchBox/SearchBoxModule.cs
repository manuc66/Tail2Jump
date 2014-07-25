using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;

namespace Tail2Jump.SearchBox
{
  internal class SearchBoxModule : IModule
  {
    private readonly IRegionViewRegistry _regionViewRegistry;
    private readonly IUnityContainer _container;

    public SearchBoxModule(IUnityContainer container_, IRegionViewRegistry registry_)
    {
      _regionViewRegistry = registry_;
      _container = container_;
    }

    public void Initialize()
    {
      _container.RegisterType<IFilterSearchGrid, FilterSearchGrid>();
      _regionViewRegistry.RegisterViewWithRegion("SearchBoxRegion", () => _container.Resolve<IFilterSearchGrid>());
    }
  }
}
