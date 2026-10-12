# Icod.Widgets Editor Open-File Sample

GPL-3.0-or-later. Opens an existing text file into an exact, editable in-memory
document; it never writes files. The application reads files returned by the
picker with a 1 MiB sample limit.

```text
dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0
```

Ctrl+O or Enter on Open opens the picker. Arrows navigate; Enter opens a folder
or chooses and accepts one file; Space selects without accepting; Ctrl+S (or
Ctrl+Enter when reported) validates and accepts. Escape cancels. Ctrl+L enters a
path; Backspace moves to the parent; Ctrl+R lists roots; Ctrl+F cycles filters.
Cancel leaves the document unchanged. Resize freely and retry inaccessible or
deleted paths during acceptance.

After opening a file:

- Type and press Enter to edit. Backspace/Delete remove text; Insert toggles
  insert/overwrite mode.
- Arrows, Home/End, and Page Up/Down move the caret. Ctrl+Left/Right moves by
  word; Ctrl+Home/End moves to the document boundaries. Add Shift to select.
- Tab indents and Shift+Tab outdents the current line or selected lines.
- Ctrl+Z/Ctrl+Y undo and redo. Ctrl+X/C/V/A cut, copy, paste, and select all.
- Drag to select and use the wheel to scroll when mouse reporting is available.
- F6 or Shift+F6 moves focus between the editor and Open button. Plain Q is text
  while the editor has focus, and Escape does not exit from the editor. Ctrl+Q
  always exits the sample.

The sample preserves mixed line endings, reports modified and clipboard status,
and intentionally provides no Save command.

```text
dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0 -- --smoke
```

Expected: `PASS exact file load, in-memory edit, text-field quit routing, undo to checkpoint, cancellation preserves document`.
