# Icod.Widgets Samples

All samples are GPL-3.0-or-later and use only public APIs. Run from the repository
root in an interactive terminal. No additional package beyond restored project
dependencies is needed. Commands use .NET 10; net8.0 and net9.0 are also supported.

| Sample | Live command | Workflow |
|---|---|---|
| Gallery | `dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0` | Seven pages: forms, lists/drop-downs, menus, dialogs/wizard, folders/files, game UI, workspace colors |
| Editor | `dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0` | Ctrl+O opens an in-memory document; a themed gutter, current-line band, and auto-hidden two-axis scrollbars demonstrate wrapped/no-wrap navigation; F7/F8 change future Enter/indentation edits, F9 toggles wrap, and Copy/Cut/Paste demonstrate linewise behavior |
| Roguelike UI | `dotnet run --project samples/Icod.Widgets.Roguelike.Sample -c Release -f net10.0` | Inventory, actions, messages, status, modal help |
| Document workspace | `dotnet run --project samples/Icod.Widgets.Workspace.Sample -c Release -f net10.0` | Three in-memory documents, stable-key tabs, close decisions, Find, Save As observation, actionable status |

The gallery's **Ctrl+T** cycles dark, light, and
terminal-default presentation. Dark/light use the running DCurses session's
color/attribute observations; default demonstrates the legacy six-role theme.
On the Menus page, Alt+F/E/V/H opens a dropdown above an editable document.
Arrows navigate; nested submenus keep their parent level open, appearing beside
it when space permits or over it in narrow layouts. Escape restores document
focus. F8 toggles theme hover styling without changing keyboard selection. Help > About demonstrates a
centered modal and theme-defined dim backdrop; click OK to close it and restore
the editor. Try the same widget and overlay under each mode. The gallery smoke also cycles
all three while checking that keyboard focus is retained. On its workspace
color page, F6 and Shift+F6 cycle among tabs, editor, the standalone horizontal
scrollbar, and actionable status. Focus the bar to try Left/Right, Page Up/Down,
Home/End, and Shift+F10; the smoke exercises this route too.

On the Dialogs page, the generic decision demonstrates `[Yes]`,
`[Don't Save]`, and `[Cancel]` captions, Y/N/C mnemonics, Cancel as the default
and cancel action, matching pointer activation, and an instance-only warning
style. The style changes presentation only; outcomes and shortcuts remain part
of the action array.

Append `-- --smoke` for deterministic checks without opening a terminal. These
checks exercise the same application controllers as live mode. CI runs them on
every supported operating system.

Tab/Shift+Tab focus; arrows navigate; Enter activates; Space toggles; Escape
closes the active dialog/menu. Q/Escape exits outside an overlay and text field.
**Ctrl+Q exits from any control**, including text fields. Samples request raw
input through DCurses so terminal flow control does not swallow Ctrl+Q. Mouse
and paste protocols are acquired when available; keyboard operation is always usable.

## Windows live verification

Icod.DCurses 2.5.2 supplies the Terminal 1.29.2 Windows console corrections
used by these samples. In Windows Terminal, Contour, or another Windows console
host, run the gallery and editor commands above and check:

1. Gallery frames and popup borders remain cell-aligned, with intact corners
   and no replacement characters or doubled box-drawing cells.
2. On the Menus page, `Alt+F` opens File. Mouse presses and releases select menu
   headings and commands; moving the pointer highlights rows only when F8 has
   enabled the theme's hover treatment. Select text, click Edit > Copy, and then
   click Edit > Paste over that same selection: the selection collapses just as
   it does with Ctrl+C followed by Ctrl+V.
3. A nested submenu appears to the right when space permits. After narrowing
   the terminal, it overlays its parent without replacing or closing that parent.
4. Help > About opens the modal; click OK to close it and restore editor focus.
5. In the editor sample, move through normal and wrapped lines. A muted band
   spans the full width of every visual row belonging to the current logical
   line, while selected text keeps its own stronger style. The fixed gutter
   labels only the first visual row; gutter-origin clicks preserve selection,
   and a very narrow resize suppresses the gutter before it consumes all text
   space. Open a document longer than the viewport and verify the auto-hidden
   vertical scrollbar appears at the extreme right with `▲`, `▼`, `│`, and `┃`
   chrome. Press F9 to disable wrapping and verify an overflowing line adds a
   bottom bar with `◄`, `►`, `─`, and `━`; when both bars are visible their
   bottom-right corner is inert.
   Ctrl+Up/Ctrl+Down scroll one displayed row, including within a wrapped line;
   the wheel scrolls three. Click both arrow buttons, page through the track,
   and drag the thumb to both ends. Right-click either bar for its standard
   scroll menu: Top/Bottom, Page Up/Down, and Scroll Up/Down vertically;
   Left/Right Edge, Page Left/Right, and Scroll Left/Right horizontally. Track
   presses always page, including when Shift or Ctrl is reported. When the
   caret would leave the viewport it is pulled to the approaching edge; when it
   remains visible it does not move.
   Resize until all content fits and verify the bar auto-hides without changing
   the line-number gutter. Type text and press
   `Ctrl+Z`; the edit is undone rather than suspending or exiting the application.

The shared live loop explicitly requests `AnyMotion` mouse reporting because
the gallery demonstrates pointer hover as well as clicks and captured drags.
Protocol acquisition remains best-effort; keyboard behavior does not depend on
mouse availability.

In the editor, F6 or Shift+F6 escapes the text editor and toggles focus to the
Open button. F7 cycles LF/CRLF/CR for future Enter edits; F8 cycles tab,
two-space, and four-space indentation; F9 toggles wrapped/no-wrap presentation.
Existing text stays unchanged.
Ctrl+Z/Ctrl+Y undo and redo; Ctrl+X/C/V/A cut, copy, paste, and
select all. With no selection, Copy/Cut uses the current line; a local linewise
Paste inserts at the target line's start. Try Ctrl+Home, Ctrl+C, Down, Ctrl+V
after opening a file with two lines, then Ctrl+Z. Clipboard transport is attempted
through DCurses, with a process-local fallback when the terminal does not provide it. Status text reports modified and
clipboard outcomes. The sample preserves mixed line endings and edits only in
memory: it deliberately provides no save command and never overwrites a file.

In the document workspace, Ctrl+Tab/Shift+Ctrl+Tab switches documents. F6 and
Shift+F6 move focus among the editor, tab strip, and actionable status fields;
Delete requests a close while the tab strip has focus. Ctrl+F opens Find, and F3
repeats the last successful query after the form closes. Alt+V followed by plain
F demonstrates scoped menu command dispatch. Save As inspects a proposed target
and never writes it.

The application owns DCurses input, time, resize/layout, provider jobs, actions,
render/refresh, leases and session cleanup. Provider jobs never mutate widgets;
results apply on the loop. Pickers return paths; the editor application reads
files (1 MiB sample limit), replaces its in-memory document, and never writes
them. The roguelike sample is a UI
integration witness with no dungeon/rules engine.

The [workspace sample](Icod.Widgets.Workspace.Sample/README.md) includes a
step-by-step walkthrough of caller-owned document lifetime, close decisions,
typed search/save requests, and the empty-workspace state. It contains no
file-writing code.

See the [`1.3 stable qualification record`](../docs/W1310-Stable-Qualification.md)
for automated coverage and the explicitly unavailable live environments.
