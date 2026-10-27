using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Tail2Jump.FileIO;
using Microsoft.Extensions.DependencyInjection;
using Tail2Jump.Controllers;
using Tail2Jump.Logging;
using Tail2Jump.ViewModels;
using Tail2Jump.App.Views;

namespace Tail2Jump.App
{
  public partial class App : Application
  {
    private ServiceProvider? _services;

    public override void Initialize()
    {
      AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
      var services = new ServiceCollection();

      services.AddSingleton<ILogController, LogController>();
      services.AddSingleton(_ => new WatcherEngine(new TailLineCache(), new TailLineCache()));
      services.AddSingleton<MainWindowViewModel>();

      _services = services.BuildServiceProvider();

      if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      {
        var viewModel = _services.GetRequiredService<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        desktop.MainWindow = window;
        desktop.ShutdownRequested += (_, _) =>
        {
          viewModel.Dispose();
          _services?.Dispose();
        };

        foreach (var argument in desktop.Args ?? Array.Empty<string>())
        {
          if (System.IO.File.Exists(argument) || System.IO.Directory.Exists(argument))
          {
            viewModel.CreateWatcher(argument, "*", false);
          }
        }
      }

      base.OnFrameworkInitializationCompleted();
    }
  }
}
