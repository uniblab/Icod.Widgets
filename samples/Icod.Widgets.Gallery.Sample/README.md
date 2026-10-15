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
3. Menus: File, Edit, View and Help sit in one row above an editable document.
   Alt+F/E/V/H opens a root dropdown and selects its first enabled command.
   Up/Down wrap through commands; Left/Right switch headings. View's Submenu
   opens with Right/Enter and returns with Left/Escape. Escape closes a root
   menu and returns focus to the document. A plain mnemonic invokes within the
   open menu; Ctrl+R invokes Run directly. F6 switches between editor and bar.
   **F8 toggles hover highlighting**, initially off; it survives Ctrl+T theme
   changes. Hover a different command, then press Enter: the keyboard-selected
   command still executes. Clicking invokes the clicked item. Help > About
   opens a centered modal; dark/light themes dim the background. Escape closes
   it and restores the document. Editor text and layout stay in place.
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

Expected: `PASS seven gallery pages, theme cycle, menu navigation/hover/modal, provider-backed picker cancellation, wizard validation, rendering`.
The smoke cycles all three themes, verifies focus retention, and exercises
F6/Shift+F6 navigation on the workspace page, root menu switching, Escape focus
restoration, independent hover, and the About modal transition. It does not verify physical
terminal color output.

Nested submenus keep the parent menu: they open to the right when space permits,
or overlay the parent on narrow screens. Left/Escape returns to the parent;
exposed parent rows can still be clicked. Ctrl+Z undoes, Ctrl+Y redoes, and Ctrl+A
selects the document through the Edit menu bindings.

Legacy Alt-key and Windows Ctrl+Z input require the Terminal dependency patch
tracked in the [1.6.0 release gate](../../docs/releases/1.6.0.md).
