using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media;

[assembly: AvaloniaTestApplication(typeof(Tail2Jump.App.Tests.HeadlessTestApp))]

namespace Tail2Jump.App.Tests
{
  /// <summary>
  /// Minimal application for headless tests: it only provides the brushes that
  /// the views reference as static resources.
  /// </summary>
  public class HeadlessApplication : Application
  {
    public override void Initialize()
    {
      Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());

      Resources["PanelBackgroundBrush"] = new SolidColorBrush(Color.Parse("#FF252526"));
      Resources["PanelBorderBrush"] = new SolidColorBrush(Color.Parse("#FF3F3F46"));
      Resources["LogBackgroundBrush"] = new SolidColorBrush(Color.Parse("#FF1C1C1C"));
      Resources["LogForegroundBrush"] = new SolidColorBrush(Color.Parse("#FFB8B7B7"));
      Resources["HyperLinkBrush"] = new SolidColorBrush(Color.Parse("#FF9ACD32"));
    }
  }

  public static class HeadlessTestApp
  {
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<HeadlessApplication>()
      .UseHeadless(new AvaloniaHeadlessPlatformOptions());
  }
}
