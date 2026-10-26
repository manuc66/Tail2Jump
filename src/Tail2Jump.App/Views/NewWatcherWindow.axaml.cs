using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Tail2Jump.Config.Objects;
using Tail2Jump.ViewModels;

namespace Tail2Jump.App.Views
{
  public partial class NewWatcherWindow : Window
  {
    public NewWatcherWindow()
    {
      InitializeComponent();

      PathBox.Text = CFG.Conf.DirectoryHistory.FirstOrDefault() ?? string.Empty;
      FilterBox.Text = CFG.Conf.FilterHistory.FirstOrDefault() ?? "*.log";
    }

    private async void OnBrowseClick(object sender, RoutedEventArgs e)
    {
      var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
      {
        Title = "Select a folder to watch",
        AllowMultiple = false
      });

      var folder = folders.FirstOrDefault();
      var path = folder?.TryGetLocalPath();
      if (!string.IsNullOrEmpty(path))
      {
        PathBox.Text = path;
      }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
      Close(null);
    }

    private void OnStartClick(object sender, RoutedEventArgs e)
    {
      var path = PathBox.Text?.Trim();
      if (string.IsNullOrEmpty(path))
      {
        PathBox.Focus();
        return;
      }

      var filter = FilterBox.Text?.Trim();
      if (string.IsNullOrEmpty(filter))
      {
        filter = "*";
      }

      Close(new NewWatcherInput
      {
        Path = path,
        Filter = filter,
        IncludeSubdirectories = RecursiveBox.IsChecked == true
      });
    }
  }
}
