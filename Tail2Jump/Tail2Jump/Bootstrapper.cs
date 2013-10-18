using System.Windows;
using Be.Manuc66.Library.FileIO;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Prism.UnityExtensions;
using Be.Manuc66.Library.DevTools;
using Be.Manuc66.Library;
using PartialView;
using Tail2Jump.Classes.Controller;
using Tail2Jump.Interfaces;

namespace Tail2Jump
{
  internal class Bootstrapper : UnityBootstrapper
  {
    protected override DependencyObject CreateShell()
    {
      return Container.Resolve<MainWindow>();
    }

    protected override void InitializeShell()
    {
      base.InitializeShell();

      Application.Current.MainWindow = (Window)Shell;
      Application.Current.MainWindow.Show();
    }

    protected override void ConfigureContainer()
    {
      base.ConfigureContainer();

      Container.RegisterType<ITailLineCache, TailLineCache>(new ContainerControlledLifetimeManager());
      Container.RegisterType<IVsAutomation, VsAutomation>();
      Container.RegisterType<IVSTools, VSTools>();
      Container.RegisterType<IHyperLinkBuilder, HyperLinkBuilder>();
      Container.RegisterType<IPartialView, PartialView.PartialView>(new ContainerControlledLifetimeManager());
      Container.RegisterType<IWatcherController, WatcherController>(new InjectionConstructor(new ResolvedParameter<ITailLineCache>(),
        new TailLineCache(),
        new ResolvedParameter<IPartialView>()));
      Container.RegisterType<ILogController, LogController>(new ContainerControlledLifetimeManager());
      Container.RegisterType<IMainWindow, MainWindow>();

    }

    protected override void ConfigureModuleCatalog()
    {
      base.ConfigureModuleCatalog();

      var moduleCatalog = (ModuleCatalog)ModuleCatalog;
      moduleCatalog.AddModule(typeof(PartialViewModule));
      moduleCatalog.AddModule(typeof(MonitoringModule));
      moduleCatalog.AddModule(typeof(SearchBoxModule));
    }
  }
}
