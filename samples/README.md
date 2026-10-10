# Icod.Widgets Samples

All samples are GPL-3.0-or-later and use only public APIs. Run from the repository
root in an interactive terminal. No additional package beyond restored project
dependencies is needed. Commands use .NET 10; net8.0 and net9.0 are also supported.

| Sample | Live command | Workflow |
|---|---|---|
| Gallery | `dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0` | Six pages: forms, lists/drop-downs, menus, dialogs/wizard, folders/files, game UI |
| Editor | `dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0` | Ctrl+O opens an existing file; arrows/page keys browse lines |
| Roguelike UI | `dotnet run --project samples/Icod.Widgets.Roguelike.Sample -c Release -f net10.0` | Inventory, actions, messages, status, modal help |

Append `-- --smoke` for deterministic checks without opening a terminal. These
checks exercise the same application controllers as live mode. CI runs them on
every supported operating system.

Tab/Shift+Tab focus; arrows navigate; Enter activates; Space toggles; Escape
closes the active dialog/menu. Q/Escape exits outside an overlay and text field.
**Ctrl+Q exits from any control**, including text fields. Samples request raw
input through DCurses so terminal flow control does not swallow Ctrl+Q. Mouse
and paste protocols are acquired when available; keyboard operation is always usable.

The application owns DCurses input, time, resize/layout, provider jobs, actions,
render/refresh, leases and session cleanup. Provider jobs never mutate widgets;
results apply on the loop. Pickers return paths; the editor application reads
files (1 MiB sample limit) and never writes them. The roguelike sample is a UI
integration witness with no dungeon/rules engine.

See [`live qualification`](../docs/W1014-Stable-Qualification.md).
