# Icod.Widgets Gallery Sample

GPL-3.0-or-later sample for the public widget API. The live gallery exercises
forms, focus traversal, lists, drop-downs, menus, borders, a message dialog, and
the wizard. Use Tab/Shift+Tab to move focus, arrows to navigate, Enter/Space to
act, and Ctrl+Q or unhandled top-level Escape to exit.

```sh
dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0
```

The deterministic smoke path constructs a form, replays input, verifies one
command action, and renders to an application-owned `CursesScreen`.

```sh
dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0 -- --smoke
```
