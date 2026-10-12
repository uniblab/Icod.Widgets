# Document Workspace Sample

Run in an interactive terminal from the repository root:

```text
dotnet run --project samples/Icod.Widgets.Workspace.Sample -c Release -f net10.0
```

Run a headless integration check:

```text
dotnet run --project samples/Icod.Widgets.Workspace.Sample -c Release -f net10.0 -- --smoke
```

The sample owns three in-memory documents and their independent views/editors.
Its tab strip emits activation and close requests; the application switches
content, updates modified markers, and asks about closing a modified tab.

| Key | Action |
| --- | --- |
| `Ctrl+Tab` / `Ctrl+Shift+Tab` | Activate the next/previous document |
| `F6` / `Shift+F6` | Move focus among the editor, tab strip, and status fields |
| `Delete` | Request closing the active tab while the tab strip has focus |
| `Ctrl+F` | Open Find for the active document |
| `F3` | Repeat the last successful query after Find closes |
| `Alt+V`, then `F` | Invoke the scoped Go to File command |
| `Alt+V`, then `L` | Open Go to Line |
| `Alt+V`, then `S` | Open Save As target inspection |
| `Ctrl+Q` | Exit from any control |

Try this workflow:

1. Press Ctrl+Tab to switch to `notes`, edit it, and observe its modified marker.
2. Press F6 to focus the tab strip, then Delete. Escape keeps the document;
   choosing Don't Save removes it. Choosing Save leaves it open because the
   sample has no application-owned file writer.
3. Press Ctrl+F, enter a query, and press Enter. Escape closes Find; F3 locates
   the next occurrence, wrapping to the beginning when necessary.
4. Press F6 until the status bar has focus. Use Left/Right and Enter to invoke
   Find or Save As. Pointer activation works when mouse reporting is available.
5. Close the remaining clean documents. Document commands become unavailable,
   and the application remains in a safe empty-workspace state.

Go to File demonstrates menu command dispatch by reporting the request in the
status line; it does not open a file. The save-target provider only inspects an
in-memory proposed path. It never writes a file. The application checks
document/view/revision before applying a search result or Go To Line request.
Query execution, selection changes, close policy, and file writes remain
application responsibilities.

The shared application loop in `samples/Common/WidgetSampleApplication.cs` owns the DCurses session, input, monotonic clock, queued provider work, render/refresh, and cleanup. Its provider results return to that loop before any widget state is updated.

Expected smoke output begins with:

```text
PASS tab switching, cancel/don't-save, F6 focus, Find Next, empty workspace
```
