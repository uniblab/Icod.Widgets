# Icod.Widgets Samples

All samples are GPL-3.0-or-later and consume only public APIs.

- `Icod.Widgets.Gallery.Sample`: form/focus/rendering gallery.
- `Icod.Widgets.Editor.Sample`: file picker with application-owned file reading.
- `Icod.Widgets.Roguelike.Sample`: action lists and modal help restoration.

Each sample has a deterministic `--smoke` mode. Live applications should feed
DCurses input to `WidgetHost.Process`, consume returned actions, render, then ask
the application-owned session to refresh. Q/Escape are the documented exit and
cancel gestures for interactive hosts.
