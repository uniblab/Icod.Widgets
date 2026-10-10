# Icod.Widgets Samples

All samples are GPL-3.0-or-later and consume only public APIs.

- `Icod.Widgets.Gallery.Sample`: form/focus/rendering gallery.
- `Icod.Widgets.Editor.Sample`: file picker with application-owned file reading.
- `Icod.Widgets.Roguelike.Sample`: action lists and modal help restoration.

Run a live sample in a terminal with .NET 10:

```sh
dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0
dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0
dotnet run --project samples/Icod.Widgets.Roguelike.Sample -c Release -f net10.0
```

Each sample also has a deterministic `--smoke` mode used by pull-request CI on
Windows, macOS, and Linux. Live applications feed DCurses input to
`WidgetHost.Process`, consume returned actions, render, then ask the
application-owned session to refresh. Ctrl+Q exits a sample; Escape dismisses a
control or modal first and exits only when the focused control does not handle it.
