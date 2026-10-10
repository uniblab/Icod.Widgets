# Icod.Widgets

## NAME

Icod.Widgets — reusable .NET terminal UI controls for applications and games.

## STATUS

Planning for the first stable release, **1.0.0**. This planning PR defines scope,
architecture, licensing, and implementation gates; it does not provide a working
DLL, runnable samples, or a published NuGet package.

## DESCRIPTION

Icod.Widgets will provide menus, dialogs, lists, drop-downs, folder and file
pickers, basic form controls, and reusable roguelike presentation controls above
Icod.DCurses. Applications will retain ownership of their event loop, commands,
validation, game rules, file operations, and terminal lifecycle.

The planned production graph is `Icod.Widgets -> Icod.DCurses -> Icod.Terminal ->
Icod.TermInfo`. The sole direct runtime package dependency of Widgets will be
Icod.DCurses. Planned targets are `net8.0`, `net9.0`, and `net10.0` with C# 13.

## CONTROLS

- Menu bars, popup menus, submenus, separators, disabled and checkable items.
- Yes/No, OK/Cancel, and Previous/Next/Cancel dialogs with explicit results.
- Single-select and multi-select lists, and drop-down selection.
- Folder selection and single/multiple existing-file selection.
- Labels, buttons, single-line text input, checkboxes, radio groups, borders,
  and scroll indicators.
- Inventory/action lists, bounded message history, status bars, help overlays,
  and text/number prompts.

## ROADMAPS

- [Main roadmap and recorded choices](ROADMAP.md).
- [1.0.0 development roadmap](Icod.Widgets-1.0.0-Development-Roadmap.md).
- [Design specification](docs/superpowers/specs/2026-10-09-icod-widgets-1.0-design.md).
- [Task-by-task implementation plan](docs/superpowers/plans/2026-10-09-icod-widgets-1.0.md).

## AUTHOR

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## COPYRIGHT

Copyright (C) 2026 Timothy J. Bruce.

## LICENSE

The library is **LGPL-3.0-or-later**. Samples and tests are
**GPL-3.0-or-later**, following the established Icod convention.
See [licensing assignments](LICENSING.md), [LGPL terms](LICENSE), and
[GPL terms](LICENSE.GPL).
