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
- F7 cycles the terminator inserted by future Enter keys through LF, CRLF,
  and CR. F8 cycles future indentation through a tab, two spaces, and four
  spaces. The status row shows the current choices. Existing text is unchanged;
  the choices remain active when another file is opened.
- Tab indents and Shift+Tab outdents the current line or selected lines using
  the selected indentation.
- Ctrl+Z/Ctrl+Y undo and redo. Ctrl+X/C/V/A cut, copy, paste, and select all.
  With no selection, Copy/Cut uses the whole current logical line, including its
  existing line ending. A local linewise Paste with no selection inserts before
  the current line; with a selection, Paste replaces that selection. A copied
  final line without an ending gains the editor's current newline when pasted
  before existing text. A different external clipboard value pastes as plain text.
- Drag to select and use the wheel to scroll when mouse reporting is available.
- F6 or Shift+F6 moves focus between the editor and Open button. Plain Q is text
  while the editor has focus, and Escape does not exit from the editor. Ctrl+Q
  always exits the sample.

To try linewise editing, open a file with at least two lines, press Ctrl+Home,
Ctrl+C, Down, then Ctrl+V. The first line appears before the second, preserving
its original ending. Press Ctrl+Z to undo the paste. Place the caret on a line
without selecting text and press Ctrl+X to cut that line; Ctrl+Z restores it.
Clipboard operations finish asynchronously, so wait for the status row before
moving the caret after Copy or Cut.

The sample preserves mixed line endings, reports modified and clipboard status,
and intentionally provides no Save command.

```text
dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0 -- --smoke
```

Expected: `PASS exact file load, mutable insertion policies, linewise clipboard, in-memory edit, undo to checkpoint, cancellation preserves document`.
