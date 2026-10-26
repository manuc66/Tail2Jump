using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Be.Manuc66.Library.Mvvm;
using Tail2Jump.Config.Objects;
using Tail2Jump.ViewModels;

namespace Tail2Jump.App.Views
{
  public class WatcherCriterion : ObservableObject
  {
    private string _path = string.Empty;
    private string _filter = "*.log";
    private bool _includeSubdirectories;

    public string Path
    {
      get { return _path; }
      set { SetProperty(ref _path, value); }
    }

    public string Filter
    {
      get { return _filter; }
      set { SetProperty(ref _filter, value); }
    }

    public bool IncludeSubdirectories
    {
      get { return _includeSubdirectories; }
      set { SetProperty(ref _includeSubdirectories, value); }
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(Path);
  }

  public partial class AdvancedWatcherWindow : Window
  {
    public AdvancedWatcherWindow()
    {
      InitializeComponent();
      DataContext = this;

      foreach (var criterion in CFG.Conf.Search.Where(c => !string.IsNullOrWhiteSpace(c.PathDirectory)))
      {
        Criteria.Add(new WatcherCriterion
        {
          Path = criterion.PathDirectory,
          Filter = string.IsNullOrWhiteSpace(criterion.FileExtension) ? "*" : criterion.FileExtension,
          IncludeSubdirectories = criterion.Recursive
        });
      }

      if (Criteria.Count == 0)
      {
        Criteria.Add(new WatcherCriterion
        {
          Path = CFG.Conf.DirectoryHistory.FirstOrDefault() ?? string.Empty,
          Filter = CFG.Conf.FilterHistory.FirstOrDefault() ?? "*.log"
        });
      }

      SelectedCriterion = Criteria.FirstOrDefault();
    }

    public ObservableCollection<WatcherCriterion> Criteria { get; } = new ObservableCollection<WatcherCriterion>();

    public WatcherCriterion SelectedCriterion { get; set; }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
      var last = Criteria.LastOrDefault();
      var criterion = new WatcherCriterion
      {
        Filter = last?.Filter ?? "*.log",
        IncludeSubdirectories = last?.IncludeSubdirectories ?? false
      };

      Criteria.Add(criterion);
      CriteriaList.SelectedItem = criterion;
    }

    private void OnRemoveClick(object sender, RoutedEventArgs e)
    {
      if (CriteriaList.SelectedItem is WatcherCriterion criterion)
      {
        Criteria.Remove(criterion);
      }
    }

    private async void OnBrowseClick(object sender, RoutedEventArgs e)
    {
      if ((sender as Button)?.DataContext is not WatcherCriterion criterion) return;

      var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
      {
        Title = "Select a folder to watch",
        AllowMultiple = false
      });

      var path = folders.FirstOrDefault()?.TryGetLocalPath();
      if (!string.IsNullOrEmpty(path))
      {
        criterion.Path = path;
      }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
      Close(null);
    }

    private void OnStartClick(object sender, RoutedEventArgs e)
    {
      var inputs = Criteria
        .Where(c => c.IsValid)
        .Select(c => new NewWatcherInput
        {
          Path = c.Path.Trim(),
          Filter = string.IsNullOrWhiteSpace(c.Filter) ? "*" : c.Filter.Trim(),
          IncludeSubdirectories = c.IncludeSubdirectories
        })
        .ToList();

      Close(inputs);
    }
  }
}
