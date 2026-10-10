# Editor Sample

GPL-3.0-or-later package consumer showing that the picker returns an existing
path while the application remains responsible for opening and reading it.
Use arrows to move, Enter to open a directory or choose a file, Space to select,
A to accept, F to cycle filters, and Escape to cancel.

```sh
dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0
dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0 -- --smoke
```

The smoke workflow uses a deterministic temporary file. The live workflow reads
the selected file only after the picker returns its validated path.
