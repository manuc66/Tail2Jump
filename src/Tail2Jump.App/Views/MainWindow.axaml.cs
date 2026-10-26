using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Tail2Jump.ViewModels;
using Tai2JumpInfra;

namespace Tail2Jump.App.Views
{
  public partial class MainWindow : Window
  {
    private MainWindowViewModel _viewModel;

    public MainWindow()
    {
      InitializeComponent();
      AddHandler(DragDrop.DragOverEvent, OnDragOver);
      AddHandler(DragDrop.DropEvent, OnDrop);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
      _viewModel = DataContext as MainWindowViewModel;
      if (_viewModel != null)
      {
        _viewModel.NewWatcherDialog = ShowNewWatcherDialogAsync;
      }

      base.OnDataContextChanged(e);
    }

    private void OnWatchersSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (_viewModel == null) return;

      _viewModel.SetSelection(WatchersList.SelectedItems.OfType<IMonitoringInstance>());
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
      e.DragEffects = e.DataTransfer?.Contains(DataFormat.File) == true
        ? DragDropEffects.Copy
        : DragDropEffects.None;
      e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
      if (_viewModel == null) return;

      var paths = e.DataTransfer?.TryGetFiles()?
        .Select(file => file.TryGetLocalPath())
        .Where(path => path != null);
      if (paths == null) return;

      _viewModel.HandleDrop(paths);
      e.Handled = true;
    }

    private async Task<NewWatcherInput> ShowNewWatcherDialogAsync()
    {
      var dialog = new NewWatcherWindow();
      var result = await dialog.ShowDialog<NewWatcherInput>(this);
      return result;
    }
  }
}
