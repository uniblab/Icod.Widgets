# Icod.Widgets

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.Widgets/d56cb9a06b78b8fa975de23cf590e6d198e36fc3/icod_tui_toolchain.jpg)

[![PR Staging build](https://github.com/uniblab/Icod.Widgets/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.Widgets/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.Widgets/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.Widgets/actions/workflows/main.yaml)

`Icod.Widgets` is a managed, cross-platform .NET terminal UI control library
for applications and games. It provides reusable forms, lists, menus, dialogs,
filesystem pickers, and game UI controls above `Icod.DCurses`, without taking
ownership of the application's event loop or business logic.

## Status

Stable release: **1.1.0**. It adds the reusable single-document editing
foundation; tabs and document-workspace policy remain application concerns
planned for a later release.

See the [changelog](https://github.com/uniblab/Icod.Widgets/blob/main/CHANGELOG.md)
for release history and the [main roadmap](https://github.com/uniblab/Icod.Widgets/blob/main/ROADMAP.md)
for the delivered 1.1 editing foundation and planned 1.2 document workspace.

## Support the Project

`Icod.Widgets` and its ecosystem packages (`Icod.TermInfo`, `Icod.Terminal`, and
`Icod.DCurses`) are built and maintained by a solo developer. If these packages
save you or your team time, please consider supporting their continued
development and maintenance.

[![GitHub Sponsors](https://img.shields.io/badge/GitHub-Sponsor?logo=githubsponsors)](https://github.com/sponsors/uniblab)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support?logo=kofi)](https://ko-fi.com/TimothyBruce)
[![PayPal](https://img.shields.io/badge/PayPal-Support?logo=paypal)](https://paypal.me/uniblab)

## Architecture

The production dependency direction is
`Icod.Widgets → Icod.DCurses → Icod.Terminal → Icod.TermInfo`.
Widgets has one direct runtime package dependency: `Icod.DCurses`.

| Layer | Responsibility |
| --- | --- |
| Application | Event loop, commands, validation, file I/O, document lifetime, and game rules |
| Icod.Widgets | Control behavior, widget trees, focus, selection, themes, dialogs, and semantic actions |
| Icod.DCurses | Cells, windows, panels, Unicode layout, clipping, retained presentation, and interaction mechanisms |
| Icod.Terminal | Live terminal input/output, protocols, session lifecycle, and reversible terminal state |
| Icod.TermInfo | Immutable terminal capability data and expansion |

## Quick Start

Install the current stable package:

```text
dotnet add package Icod.Widgets --version 1.1.0
```

The 1.1.0 package declares `Icod.DCurses 2.4.0` as its sole direct runtime
dependency.

For a runnable first step, clone the repository and launch the gallery from its
root in an interactive terminal with the .NET 10 SDK:

```text
git clone https://github.com/uniblab/Icod.Widgets.git
cd Icod.Widgets
dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0
```

Use Ctrl+N/Ctrl+P to visit its six pages and Ctrl+Q to exit.

To exercise the 1.1 multiline editing foundation, run the editor sample:

```text
dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0
```

Use Ctrl+O to load an existing text file into memory, F6 to move focus between
the editor and Open button, and Ctrl+Q to exit. The sample never writes files.

Applications create a `WidgetHost`, attach a widget tree, and supply input and
time. This is **an excerpt from an application loop**, not a standalone program:
`session` is an open `CursesSession`, `inputEvent` is the current normalized input
event, and `elapsed` is a monotonic `TimeSpan` such as `Stopwatch.Elapsed`.
Host creation and attachment happen once; input processing and refresh repeat.

```csharp
using Icod.Widgets;

// Once, after opening the application's DCurses session:
using WidgetHost host = new(session, WidgetTheme.Default);
WidgetButton save = new("save", "Save", "file.save");
host.Attach(save);
host.Arrange(session.Screen.Bounds);

// Within the application's input/render loop:
WidgetDispatchResult result = host.Process(inputEvent, elapsed);
foreach (WidgetAction action in result.Actions) {
    // Execute application commands or update application state.
}
host.Render();
await session.RefreshAsync();
```

See the [shared sample loop](https://github.com/uniblab/Icod.Widgets/blob/v1.1.0/samples/Common/WidgetSampleApplication.cs)
for complete session setup, input, resizing, rendering, and cleanup.

## Feature Inventory

- Labels, buttons, single-line text input, checkboxes, radio groups, borders,
  scroll indicators, and stack/grid layout.
- Exact mixed-EOL text documents, immutable snapshots, checkpoint identity,
  bounded undo/redo, independent view state, and Unicode-aware multiline editing.
- Selection, indentation, insert/overwrite mode, read-only operation, captured
  pointer drag/autoscroll, both-axis scrollbars, explicit bounded clipboard
  requests, and focused terminal-caret intent.
- Single- and multi-select lists, keyboard search, and framed drop-down popups.
- Menu bars, framed popup menus, submenus, separators, disabled/checkable items,
  scoped mnemonics, and independent command shortcuts.
- Captioned, framed OK, Yes/No, OK/Cancel, and Previous/Next/Cancel dialogs with
  explicit results and modal focus restoration.
- Provider-backed folder selection and single/multiple existing-file selection.
- Inventory/action lists, bounded message history, status bars, help overlays,
  and text/number prompts.

Tab/Shift+Tab traverses controls; arrows navigate; Enter activates or commits;
Space toggles. Menu mnemonics navigate structure (`Alt+V`, then plain `F`), while
shortcuts invoke commands directly. Escape cancels the top dialog/menu;
**even an OK-only dialog returns Cancel on Escape**, not acknowledgement.

The [controls and interaction guide](https://github.com/uniblab/Icod.Widgets/blob/main/docs/Controls-and-Interaction.md)
contains menu and dialog examples, picker keys, configuration, errors, and limits.

The [editing guide](https://github.com/uniblab/Icod.Widgets/blob/main/docs/Editing-Guide.md)
covers document/view ownership, commands, clipboard completion, caret intent,
resource limits, and application-loop integration.

## Platforms and Targets

The library, samples, and tests target `net8.0`, `net9.0`, and `net10.0` with
C# 13 and AnyCPU library output. Build tooling supports CMD, shell, and
PowerShell 5.1. Interactive applications require a usable terminal and a DCurses
profile supporting cursor motion.

Windows, macOS, and Linux are build/CI targets. Live terminal results are
environment-specific: [W1113](https://github.com/uniblab/Icod.Widgets/blob/v1.1.0/docs/W1113-Stable-Qualification.md)
records the 1.1 qualification scope and explicitly unavailable live environments.
Automated tests are not a claim of live-terminal qualification.

## Design Boundaries and Guarantees

- Applications own terminal sessions, the input reader, monotonic time, command
  execution, validation, asynchronous work, and cleanup. Widgets starts no hidden
  event loop or competing input reader.
- Provider jobs return results for application-loop application. Stale, foreign,
  canceled, and closed-picker results cannot complete a selection.
- Pickers return paths; they do not open, create, delete, or write files. The
  editor sample performs bounded file reads in application code and edits only
  an in-memory document; it intentionally never writes user files.
- Modal scopes isolate background commands and restore eligible focus. Compact
  viewports clip rendering while retaining keyboard cancellation.
- Resource limits are explicit. The 1.0 host supports 1,024 widgets, 4,096 pending
  actions, and 16 modal/submenu levels; lists and directory snapshots allow
  100,000 items. Invalid ownership and capacity violations fail before attachment.
- DCurses immediate-raster presentation cannot compose overlapping retained
  panels; raster applications select text presentation while an overlay is visible.

## Samples and Documentation

The [sample guide](https://github.com/uniblab/Icod.Widgets/blob/v1.1.0/samples/README.md)
covers the gallery, editor open-file flow, and roguelike UI. All three support
`-- --smoke` for deterministic checks without a live terminal.

Recommended entry points:

- [Controls and interaction](https://github.com/uniblab/Icod.Widgets/blob/main/docs/Controls-and-Interaction.md) — behavior, keys, examples, errors, and limits.
- [Changelog](https://github.com/uniblab/Icod.Widgets/blob/main/CHANGELOG.md) and [1.1.0 release notes](https://github.com/uniblab/Icod.Widgets/blob/v1.1.0/docs/releases/1.1.0.md) — shipped history.
- [Main roadmap](https://github.com/uniblab/Icod.Widgets/blob/main/ROADMAP.md) — broader direction and library/application boundaries.
- [1.1.0 development roadmap](https://github.com/uniblab/Icod.Widgets/blob/main/Icod.Widgets-1.1.0-Development-Roadmap.md) — planned tranches, dependencies, and release gates.
- [1.1 public API baseline](https://github.com/uniblab/Icod.Widgets/blob/v1.1.0/docs/Public-API-Baseline-1.1.md) and [1.0 baseline](https://github.com/uniblab/Icod.Widgets/blob/v1.0.0/docs/Public-API-Baseline-1.0.md) — compatibility references.
- [Build and packaging guide](https://github.com/uniblab/Icod.Widgets/blob/main/packaging/README.md) — local builds and validation/publication workflows.

Release audits, versioned plans, API snapshots, and qualification worksheets
remain in the repository. They are intentionally not repeated in this README.

## Compatibility and Versioning

The stable 1.0 public API is the compatibility floor for the 1.x line. Version
1.1 evolves it additively, preserves existing control contracts, and retains
assembly identity `1.0.0.0`; package versions advance separately.

The README is a product and contributor entry point. Release chronology belongs
in the changelog; implementation and qualification status belongs in versioned
roadmaps and evidence records. Planned features are not installation promises.

## Authors

While this is all original work by Timothy Bruce, it is inspired by original work from Bill Joy, author of the original `termcap`; Ken Arnold, for his work on `termcap` and `curses`; Mary Ann (born Mark) Horton, author of `terminfo`; Pavel Curtis, author of `pcurses`; and Zeyd Ben-Halim, Eric S. Raymond, and Thomas Dickey, whose work developed and maintained `libtinfo` and `ncurses`.

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## Copyright

Copyright (c) 2026 Timothy J. Bruce

## License

The library is **LGPL-3.0-or-later**. Samples and tests are **GPL-3.0-or-later**,
following the established Icod convention.
See [licensing assignments](https://github.com/uniblab/Icod.Widgets/blob/v1.1.0/LICENSING.md),
[LGPL terms](https://github.com/uniblab/Icod.Widgets/blob/v1.1.0/LICENSE), and
[GPL terms](https://github.com/uniblab/Icod.Widgets/blob/v1.1.0/LICENSE.GPL).
