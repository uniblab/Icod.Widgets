# Icod.Widgets Samples

All samples are GPL-3.0-or-later and use only public APIs. Run from the repository
root in an interactive terminal. No additional package beyond restored project
dependencies is needed. Commands use .NET 10; net8.0 and net9.0 are also supported.

| Sample | Live command | Workflow |
|---|---|---|
| Gallery | `dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0` | Seven pages: forms, lists/drop-downs, menus, dialogs/wizard, folders/files, game UI, workspace colors |
| Editor | `dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0` | Ctrl+O opens an existing file into an exact, editable in-memory document |
| Roguelike UI | `dotnet run --project samples/Icod.Widgets.Roguelike.Sample -c Release -f net10.0` | Inventory, actions, messages, status, modal help |
| Document workspace | `dotnet run --project samples/Icod.Widgets.Workspace.Sample -c Release -f net10.0` | Three in-memory documents, stable-key tabs, close decisions, Find, Save As observation, actionable status |

The gallery's **Ctrl+T** cycles dark, light, and
terminal-default presentation. Dark/light use the running DCurses session's
color/attribute observations; default demonstrates the legacy six-role theme.
Try the same widget and overlay under each mode. The gallery smoke also cycles
all three while checking that keyboard focus is retained.

Append `-- --smoke` for deterministic checks without opening a terminal. These
checks exercise the same application controllers as live mode. CI runs them on
every supported operating system.

Tab/Shift+Tab focus; arrows navigate; Enter activates; Space toggles; Escape
closes the active dialog/menu. Q/Escape exits outside an overlay and text field.
**Ctrl+Q exits from any control**, including text fields. Samples request raw
input through DCurses so terminal flow control does not swallow Ctrl+Q. Mouse
and paste protocols are acquired when available; keyboard operation is always usable.

In the editor, F6 or Shift+F6 escapes the text editor and toggles focus to the
Open button. Ctrl+Z/Ctrl+Y undo and redo; Ctrl+X/C/V/A cut, copy, paste, and
select all. Clipboard transport is attempted through DCurses, with a process-local
fallback when the terminal does not provide it. Status text reports modified and
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
