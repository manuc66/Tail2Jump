using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Tail2Jump.ViewModels;
using Tail2Jump.Monitoring;

namespace Tail2Jump.App.Views
{
  public partial class MainWindow : Window
  {
    private const int TitleFlashMaxTicks = 10;

    private MainWindowViewModel? _viewModel;
    private DispatcherTimer? _titleFlashTimer;
    private string? _baseTitle;
    private int _titleFlashTicks;

    public MainWindow()
    {
      InitializeComponent();
      AddHandler(DragDrop.DragOverEvent, OnDragOver);
      AddHandler(DragDrop.DropEvent, OnDrop);
      AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
      Activated += (_, _) => StopFlashing();
      Closed += (_, _) => DetachFromViewModel();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
      DetachFromViewModel();

      _viewModel = DataContext as MainWindowViewModel;
      if (_viewModel != null)
      {
        _viewModel.NewWatcherDialog = ShowNewWatcherDialogAsync;
        _viewModel.AdvancedWatcherDialog = ShowAdvancedWatcherDialogAsync;
        _viewModel.AttentionRequested += OnAttentionRequested;
      }

      base.OnDataContextChanged(e);
    }

    private void DetachFromViewModel()
    {
      StopFlashing();

      if (_viewModel == null) return;

      _viewModel.AttentionRequested -= OnAttentionRequested;
      _viewModel.NewWatcherDialog = null;
      _viewModel.AdvancedWatcherDialog = null;
      _viewModel = null;
    }

    private void OnAttentionRequested()
    {
      if (IsActive) return;

      if (OperatingSystem.IsWindows())
      {
        FlashNativeWindow(stop: false);
        return;
      }

      StartTitleFlash();
    }

    private void StartTitleFlash()
    {
      _baseTitle = _viewModel?.Title ?? Title ?? "Tail2Jump";
      _titleFlashTicks = 0;

      if (_titleFlashTimer == null)
      {
        _titleFlashTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _titleFlashTimer.Tick += OnTitleFlashTick;
      }

      _titleFlashTimer.Start();
    }

    private void OnTitleFlashTick(object? sender, EventArgs e)
    {
      Title = Title == _baseTitle ? "● " + _baseTitle : _baseTitle;

      if (++_titleFlashTicks <= TitleFlashMaxTicks) return;

      StopFlashing();
    }

    private void StopFlashing()
    {
      if (_titleFlashTimer != null)
      {
        _titleFlashTimer.Stop();
        _titleFlashTimer.Tick -= OnTitleFlashTick;
        _titleFlashTimer = null;
      }

      if (OperatingSystem.IsWindows())
      {
        FlashNativeWindow(stop: true);
      }

      if (!string.IsNullOrEmpty(_baseTitle))
      {
        Title = _baseTitle!;
      }
    }

    private void FlashNativeWindow(bool stop)
    {
      var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
      if (handle == IntPtr.Zero) return;

      var info = new FlashWindowInfo
      {
        Size = (uint)Marshal.SizeOf<FlashWindowInfo>(),
        Window = handle,
        Flags = stop ? FlashStop : FlashAll | FlashTimer,
        Count = uint.MaxValue,
        Timeout = 0
      };

      FlashWindowEx(ref info);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
      if (_viewModel == null) return;

      var focused = FocusManager?.GetFocusedElement();
      var textBoxHasFocus = focused is TextBox;

      if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
          e.KeyModifiers.HasFlag(KeyModifiers.Shift))
      {
        _viewModel.ShowConsole = !_viewModel.ShowConsole;
        e.Handled = true;
      }
      else if (e.Key == Key.P && e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
               e.KeyModifiers.HasFlag(KeyModifiers.Shift))
      {
        _viewModel.ShowPerformance = !_viewModel.ShowPerformance;
        e.Handled = true;
      }
      else if (!textBoxHasFocus && e.Key == Key.C && e.KeyModifiers == KeyModifiers.Control)
      {
        LogView.CopySelectionOrAll();
        e.Handled = true;
      }
      else if (!textBoxHasFocus && e.Key == Key.A && e.KeyModifiers == KeyModifiers.Control)
      {
        LogView.SelectAllLines();
        e.Handled = true;
      }
      else if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Control))
      {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
          _ = _viewModel.AddAdvancedWatchersAsync();
        }
        else
        {
          _ = _viewModel.AddWatcherAsync();
        }

        e.Handled = true;
      }
      else if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
      {
        _viewModel.ShowSearch = true;
        SearchBox.Focus();
        e.Handled = true;
      }
      else if (e.Key == Key.Escape && _viewModel.ShowSearch)
      {
        _viewModel.ShowSearch = false;
        e.Handled = true;
      }
    }

    private void OnWatchersSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
      if (_viewModel == null) return;

      _viewModel.SetSelection(WatchersList.SelectedItems?.OfType<IMonitoringInstance>() ?? System.Linq.Enumerable.Empty<IMonitoringInstance>());
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
      e.DragEffects = e.DataTransfer?.Contains(DataFormat.File) == true
        ? DragDropEffects.Copy
        : DragDropEffects.None;
      e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
      if (_viewModel == null) return;

      var paths = e.DataTransfer?.TryGetFiles()?
        .Select(file => file.TryGetLocalPath())
        .Where(path => path != null)
        .Select(path => path!)
        .ToList();

      if (paths == null) return;

      _viewModel.HandleDrop(paths);
      e.Handled = true;
    }

    private async Task<NewWatcherInput?> ShowNewWatcherDialogAsync()
    {
      var dialog = new NewWatcherWindow();
      var result = await dialog.ShowDialog<NewWatcherInput>(this);
      return result;
    }

    private async Task<IReadOnlyList<NewWatcherInput>?> ShowAdvancedWatcherDialogAsync()
    {
      var dialog = new AdvancedWatcherWindow();
      var result = await dialog.ShowDialog<IReadOnlyList<NewWatcherInput>>(this);
      return result;
    }

    private const uint FlashStop = 0;
    private const uint FlashAll = 3;
    private const uint FlashTimer = 4;

    [StructLayout(LayoutKind.Sequential)]
    private struct FlashWindowInfo
    {
      public uint Size;
      public IntPtr Window;
      public uint Flags;
      public uint Count;
      public uint Timeout;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FlashWindowInfo info);
  }
}
