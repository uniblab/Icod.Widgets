# Icod.Widgets Gallery Sample

GPL-3.0-or-later. Run from the repository root:

```text
dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0
```

Ctrl+N/Ctrl+P change page; Tab/Shift+Tab focus controls. **Ctrl+T** cycles
dark, light, and terminal-default themes without disturbing focus. The first
two use `WidgetPalette.Create(session.PresentationCapabilities)`; when a
profile offers no color, foreground and background stay at terminal defaults.
Compare selected rows, disabled entries, popup borders and empty cells, dialog
captions, status fields, and the editor's focused/unfocused selection. Seven pages:

1. Forms: type/paste Unicode; Left/Right/Home/End move by text element;
   Backspace/Delete edit. Space toggles checkbox; arrows choose radio;
   Enter activates a button. The footer shows semantic actions.
2. Lists/drop-down: Enter commits single choice; Space toggles multi-selection;
   Ctrl+A selects enabled entries; Shift+arrows select a range. Type a search
   prefix (750 ms expiry). Enter/Down opens the drop-down; arrows preview;
   Enter commits; Escape/outside click cancels. Disabled items are inert.
3. Menus: Alt+F/Alt+V opens the matching menu-bar entry from another control;
   an unmodified mnemonic letter invokes within the open level; Ctrl+R invokes
   its command directly. Shortcuts render at the right edge of framed popups.
   Enter/Down opens; arrows navigate; Right/Enter opens a submenu; Left/Escape
   returns.
4. Dialogs: all titles are captions in themed frames. The OK-only alert
   acknowledges with Enter; Yes/No defaults to No; OK/Cancel to Cancel.
   Arrows/Tab choose; Enter completes; Escape always reports Cancel. Wizard first rejects Next to show
   application validation; Enter again advances; Left returns; Escape closes.
   Text and culture/range-validated number prompts are also available.
5. Pickers: arrows/Page keys browse; Enter opens directory or toggles file;
   Space toggles file. **Ctrl+S (or Ctrl+Enter when reported) validates/accepts** the current folder or
   selected files. Backspace/Alt+Up moves to parent. Ctrl+L enters a path
   (type/paste; Enter loads). Ctrl+R lists roots/drives. Ctrl+F cycles file
   filters. Ctrl+B shows basket; Enter/Space/Delete removes a basket entry.
   Escape cancels. Selection survives directory/filter changes; failures stay open.
6. Game UI: inventory, messages, status, and modal help.
7. Workspace color: an active modified tab, multiline editor, scrollbar track
   and thumb, plus actionable status. F6/Shift+F6 cycles focus among tabs,
   editor, and status, including a way out of the editor, which consumes Tab.
   Ctrl+T switches themes while preserving the active tab and document view.

Resize repeatedly, including compact layouts. Ctrl+Q always exits; Q/Escape
exits outside a dialog/menu and text field. Keyboard operation requires no mouse.
The sample uses retained text presentation and no raster graphics.

```text
dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0 -- --smoke
```

Expected: `PASS seven gallery pages, theme cycle, provider-backed picker cancellation, wizard validation, rendering`.
The smoke cycles all three themes, verifies focus retention, and exercises
F6/Shift+F6 navigation on the workspace page. It does not verify physical
terminal color output.
