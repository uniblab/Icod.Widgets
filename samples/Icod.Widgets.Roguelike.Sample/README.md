# Roguelike UI Sample

GPL-3.0-or-later public-API witness for inventory/action lists and modal help.
Game rules and state stay in the application. Tab changes panels, arrows move,
Enter invokes an action, `?` opens help, T opens a text prompt, N opens a number
prompt, and Ctrl+Q or unhandled top-level Escape exits.

```sh
dotnet run --project samples/Icod.Widgets.Roguelike.Sample -c Release -f net10.0
dotnet run --project samples/Icod.Widgets.Roguelike.Sample -c Release -f net10.0 -- --smoke
```
