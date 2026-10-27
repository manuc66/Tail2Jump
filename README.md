# Tail2Jump

Real-time log tailing tool: watch several files, stream their new lines, filter and
search the output, and open the referenced source file at the right line.

![CI](https://github.com/manuc66/Tail2Jump/actions/workflows/ci.yml/badge.svg)

## Build and test

Requires the .NET 10 SDK.

```bash
dotnet build Tail2Jump.slnx
dotnet test  Tail2Jump.slnx
```

The build is strict: nullable reference types, the .NET analyzers and
warnings-as-errors are enabled (including Avalonia XAML warnings).

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
benchmarks/
  Tail2Jump.Benchmarks/  BenchmarkDotNet suites
```

The historical WPF implementation was removed; it is still available in the git
history and under the `legacy-wpf` tag.

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
- views unsubscribe from view-model events when detached, and the ownership of
  timers, delegates and collections is cleaned up on close

## Benchmarks

```bash
dotnet run -c Release --project benchmarks/Tail2Jump.Benchmarks
# or a single suite:
dotnet run -c Release --project benchmarks/Tail2Jump.Benchmarks -- --filter "*LineSplittingBenchmarks*"
```

`BenchmarkDotNet` reports time and allocations. On the reference machine (i7-4790):

| Path | Time | Allocated |
|---|---|---|
| UTF-8 split, legacy decode + `string.Split` | 3.66 ms / 10k lines | 2.77 MB |
| UTF-8 split, `ReadOnlySpan<byte>` | 1.45 ms / 10k lines | 1.69 MB |
| Line parsing, legacy `string.Split` | 259 ns | 560 B |
| Line parsing, memory slices | 206 ns | 256 B |
| Read a 50-line page, formatted (`GetLines`) | 1.4 us | 4.0 KB |
| Read a 50-line page, raw entries (`GetEntries`) | 133 ns | 824 B |
| Filter replay (50k lines, 10% matches) | 1.3 ms | 782 KB |

The log viewport is virtualized: a headless test asserts that a 10,000-line
list only realizes the visible rows.

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
