using System;
using System.Windows;
using NBug;
using NBug.Enums;

namespace Tail2Jump
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App
  {
    public App()
    {
      AppDomain.CurrentDomain.UnhandledException += Handler.UnhandledException;
      Current.DispatcherUnhandledException += Handler.DispatcherUnhandledException;
      Settings.StoragePath = StoragePath.IsolatedStorage;
      Settings.UIMode = UIMode.Full;
    }
    protected override void OnStartup(StartupEventArgs e)
    {
      var bootstrapper = new Bootstrapper();
      bootstrapper.Run();
    }
  }
}
