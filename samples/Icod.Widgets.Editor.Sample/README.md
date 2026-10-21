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
  On wrapped text, vertical and page movement traverse visual rows while
  retaining the preferred visual column across short rows; document positions
  remain logical.
  A muted full-width band follows the current logical line, including all of
  its wrapped visual rows; selected text remains distinct above that band.
  A fixed themed gutter shows one-based logical line numbers. Wrapped
  continuation rows remain blank; the gutter disappears when a narrow resize
  would leave no text column and returns when space is restored.
- When content exceeds the viewport, opt-in scrollbars occupy the extreme-right
  column and, in no-wrap mode, the bottom row. They auto-hide independently when
  the document fits and are suppressed when too little usable text space remains.
  `▲` and `▼` scroll one displayed row, a `│` track click pages, and the `┃`
  thumb may be dragged. Ctrl+Up/Ctrl+Down scroll one displayed row and the
  wheel scrolls three, so wrapped continuations count just like ordinary rows.
  Press F9 to toggle wrapping. In no-wrap mode, `◄` and `►` step horizontally,
  track clicks page, and the `━` thumb drags. The bottom-right corner is inert
  when both axes are visible. Right-click a bar for its standard axis-specific
  scroll menu. The vertical menu provides Top, Bottom, Page Up/Down, and Scroll
  Up/Down; the horizontal menu provides Left/Right Edge, Page Left/Right, and
  Scroll Left/Right. There is no direct-position command: every track press
  pages, including when Shift or Ctrl is reported. If
  scrolling would strand the caret off-screen, the caret is clamped to the
  approaching viewport edge at its preferred visual column; otherwise it and
  the selection remain untouched.
  The chrome uses the scrollbar track, thumb, button, hover, pressed, corner,
  and disabled theme roles, so themes control presentation without changing
  behavior.
- F7 cycles the terminator inserted by future Enter keys through LF, CRLF,
  and CR. F8 cycles future indentation through a tab, two spaces, and four
  spaces. The status row shows the current choices. Existing text is unchanged;
  the choices remain active when another file is opened.
- F9 toggles text-element wrapping and no-wrap presentation. Horizontal scroll
  state is used only while wrapping is disabled.
- Tab indents and Shift+Tab outdents the current line or selected lines using
  the selected indentation.
- Ctrl+Z/Ctrl+Y undo and redo. Ctrl+X/C/V/A cut, copy, paste, and select all.
  With no selection, Copy/Cut uses the whole current logical line, including its
  existing line ending. A local linewise Paste with no selection inserts before
  the current line; with a selection, Paste replaces that selection. A copied
  final line without an ending gains the editor's current newline when pasted
  before existing text. A different external clipboard value pastes as plain text.
- Drag to select and use the wheel, scrollbar arrows, track, or thumb to scroll
  when mouse reporting is available.
  A press that begins in the decorative line-number gutter leaves the caret and
  selection unchanged; a drag that begins in text may cross the gutter.
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

For live scrollbar qualification, open a file with enough lines and one long
line. Verify the right bar composes with line numbers, then exercise
Ctrl+Up/Ctrl+Down within a wrapped line, the wheel, arrow buttons, track paging,
context menus, and thumb dragging. When the terminal reports them, repeat a
track press with Shift and Ctrl and confirm it still pages normally. Press F9
and repeat on the bottom
bar. With both axes visible, click the corner and confirm nothing moves. Resize
wide/tall enough for all content to fit and confirm auto-hide, then shrink to one
row and confirm no partial scrollbar is painted.

```text
dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0 -- --smoke
```

Expected: `PASS exact file load, gutter and auto-hidden two-axis scrollbar geometry/input including modifier paging, wrapped-row Ctrl and wheel scrolling, mutable insertion policies, linewise clipboard, in-memory edit, undo to checkpoint, cancellation preserves document`.
