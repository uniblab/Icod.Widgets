# Icod.Widgets

## NAME

Icod.Widgets — reusable .NET terminal UI controls for applications and games.

## STATUS

The full planned 1.0 control set is implemented as **1.0.0-rc.1** for .NET 8,
9, and 10. Automated Linux qualification and package-only consumption pass.
Live Windows, macOS, Linux terminal, and applicable ARM64 evidence remains the
final stable-release gate. No NuGet package has been published.

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

## INSTALLATION

Until publication, build the repository or restore the package produced by
`dotnet pack Icod.Widgets.csproj -c Release -o artifacts/packages`. The package
declares `Icod.DCurses 2.3.1` as its sole direct runtime dependency.

## USAGE

Applications create a `WidgetHost` over their DCurses session or screen, attach
a widget tree, and keep ownership of the loop:

```csharp
using WidgetHost host = new(session, WidgetTheme.Default);
WidgetButton save = new("save", "Save", "file.save");
host.Attach(save);
host.Arrange(session.Screen.Bounds);

WidgetDispatchResult result = host.Process(inputEvent, elapsed);
foreach (WidgetAction action in result.Actions) {
    // Execute application commands or update application state.
}
host.Render();
await session.RefreshAsync();
```

Tab and Shift+Tab traverse controls. Arrows navigate lists, radio groups, menus,
and drop-downs. Enter activates or commits, Space toggles, and Escape cancels the
top dialog/menu. Applications supply monotonic timestamps for list search.

Filesystem loading and selection validation are asynchronous provider calls.
Apply returned browser/validation results on the application loop; stale,
foreign, canceled, and disposed-controller results are ignored. Pickers return
paths and never open, create, delete, or write files.

## LIMITS

One host accepts 1,024 widgets, 4,096 pending actions, 16 modal levels, and 16
submenu levels. Lists and directory snapshots accept 100,000 items. Extremely
small viewports retain keyboard cancellation and clip rendering. DCurses
immediate-raster presentation cannot compose overlapping retained panels, so an
application using raster content must select its text presentation while an
overlay is visible.

## EXAMPLES

Run the GPL gallery, editor, and roguelike witnesses with `--smoke`; see
[`samples/README.md`](samples/README.md) for commands and ownership boundaries.

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
