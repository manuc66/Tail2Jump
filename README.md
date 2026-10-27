# Tail2Jump

Real-time log tailing tool: watch several files, stream their new lines, filter and
search the output, and open the referenced source file at the right line.

![CI](https://github.com/manuc66/Tail2Jump/actions/workflows/ci.yml/badge.svg)

## Two implementations

| Directory | Technology | Platforms |
|---|---|---|
| `Tail2Jump/` (legacy) | WPF / .NET Framework 4.5.1 | Windows only |
| `src/` (port) | .NET 10 / Avalonia | Windows, Linux, macOS |

The cross-platform port is **in progress**. The business logic (file tailing, line
cache, watcher engine, configuration) is already ported and covered by tests; the
Avalonia UI is being rebuilt feature by feature.

## Build and test

Requires the .NET 10 SDK.

```bash
dotnet build Tail2Jump.Avalonia.sln
dotnet test  Tail2Jump.Avalonia.sln
```

The build is strict: nullable reference types, the .NET analyzers and
warnings-as-errors are enabled (including Avalonia XAML warnings).

The legacy Windows solution (`Tail2Jump.sln`) can still be built with Visual Studio
on Windows.

## Run the Avalonia application

```bash
dotnet run --project src/Tail2Jump.App

# Open a file or a directory straight away:
dotnet run --project src/Tail2Jump.App -- /var/log
```

## Project layout

```
src/
  Tail2Jump.Core/        UI-free business logic
  Tail2Jump.Core.Tests/  NUnit tests for the business logic
  Tail2Jump.App/         Avalonia desktop application
  Tail2Jump.App.Tests/   NUnit tests for the view models
```

## Performance and memory

- log lines are parsed lazily: only lines realized by the list box pay for
  building their segments, and the result is cached per line
- the display keeps the last 50,000 lines and the line cache the last 200,000,
  so a long-running tail cannot grow without bound
- filtering is debounced (250 ms) instead of replaying the whole cache on every
  keystroke, and replays are chunked
- the tailer decodes UTF-8 lines from `ReadOnlySpan<byte>` and reads in bounded
  chunks (`ArrayPool` for large reads, `stackalloc` for small ones), without
  intermediate full-buffer strings
- the line cache stores `(file, content)` entries and only builds the displayed
  `file|content` string for the requested page
- `RandomLineFileAccessor` indexes and reads through a read-only memory-mapped
  file (with UTF-8 BOM handling)
- views unsubscribe from view-model events when detached, and the ownership of
  timers, delegates and collections is cleaned up on close

## Configuration

Configuration is stored as JSON in `$XDG_CONFIG_HOME/Tail2Jump/SaveConfig.json`
(or `~/.config/Tail2Jump/SaveConfig.json`) and holds the watcher history and filter
history. The path can be overridden with the `TAIL2JUMP_CONFIG` environment variable.

## Status

Done:
- cross-platform file tailer (FileSystemWatcher based)
- thread-safe line cache and watcher engine
- JSON configuration
- Avalonia shell: watcher list, log view, filtering and search highlighting,
  colored console with exceptions, editor detection, drag and drop, new-watcher and
  multi-location dialogs, `file:line` links
- log view: line selection and copy (`Ctrl+C` / `Ctrl+A`), smart auto-scroll that
  pauses when you scroll up
- window attention: title flash on Linux/macOS, native `FlashWindowEx` on Windows
- portable performance panel: CPU and process memory charts (`/proc` on Linux,
  `GetSystemTimes` on Windows, `sysctl` on macOS)
- shortcuts: `Ctrl+F` (search), `Ctrl+N` (new watcher), `Ctrl+Shift+N` (multi),
  `Ctrl+Shift+C` (console)

Still to port:
- per-watcher notifications and paused state
- stress-test panel (CPU burner / temperature)
- UI tests
