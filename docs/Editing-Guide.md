# Editing Guide

Icod.Widgets 1.1 separates exact document data, per-view presentation state,
the interactive editor control, and application-owned terminal services. This
keeps file policy, asynchronous I/O, clipboard access, and the event loop outside
the reusable control library.

## Create and host an editor

`WidgetTextDocument` preserves the supplied UTF-16 text exactly, including mixed
CR, LF, and CRLF endings. `WidgetTextViewState` owns one caret, selection, and
scroll position. Dispose a view when it is no longer used; a document may outlive
any view and may be shared by more than one view.

```csharp
WidgetTextDocument document = new([initialText.AsMemory()]);
using WidgetTextViewState view = new(document);
WidgetTextEditor editor = new("document", view);

using WidgetHost host = new(session, WidgetTheme.Default);
host.Attach(editor);
host.Arrange(session.Screen.Bounds);
host.Focus(editor);
```

Applications own file reads and writes. Read bounded chunks or text, construct a
document on the application loop, and replace the old view/control deliberately.
The library never opens or overwrites a path returned by a picker.

## Input, commands, and actions

Pass normalized input and monotonic time through `WidgetHost.Process`. Call
`WidgetHost.Update` on caller-controlled ticks for bounded autoscroll work. Text
input, selection replacement, indentation, deletion, and paste each produce
semantic actions; the application may use these to update status or commands.

`WidgetHost.ExecuteEditCommand` routes Copy, Cut, Paste, Select All, Undo, and
Redo only to the focused edit target. This lets the same menu command operate on
a single-line field or the document editor without application type checks.
The editor consumes Tab for indentation. Applications that need to leave it can
provide an explicit command such as F6 and call `WidgetHost.MoveFocus` or `Focus`.

## Editor insertion policies

`WidgetTextEditor.NewLine` controls the terminator inserted by subsequent Enter
keys. Set it to `"\n"`, `"\r\n"`, or `"\r"`. `WidgetTextEditor.Indentation`
controls subsequent Tab and Shift+Tab edits and accepts only spaces and tabs.
Both properties start with their `WidgetEditorOptions` values; changing either
property leaves existing text, view state, and undo history intact. For example:

```csharp
editor.NewLine = "\r\n";
editor.Indentation = "  ";
```

Applications still own whole-document line-ending conversion and file-format
policy. Changing `NewLine` does not normalize the document or mark it modified;
a later Enter edit does so through the ordinary undoable edit path.

## Clipboard completion

Clipboard commands emit `WidgetClipboardRequestedAction`; they do not perform
terminal I/O. Execute the request asynchronously through the application's
DCurses/session policy, then apply the typed result on the application loop:

```csharp
foreach (WidgetAction action in result.Actions) {
    if (action is WidgetClipboardRequestedAction requested) {
        _ = CompleteClipboardAsync(requested.Request);
    }
}

async Task CompleteClipboardAsync(WidgetClipboardRequest request) {
    WidgetClipboardResult result = await clipboard.ExecuteAsync(request);
    // Marshal this call back to the application loop.
    host.ApplyClipboardResult(result);
}
```

For multiline editors, Copy/Cut without a selection exports the current
logical line including its existing terminator. `request.IsLinewise` tells the
application to retain that local metadata. When returning a local clipboard
value for Paste, pass `IsLinewise: true` in `WidgetClipboardResult` to insert
at the current line start if there is no selection. When an unterminated
copied line precedes existing text, the editor adds its current `NewLine` as
a separator. A selected range is replaced normally. Clipboard transports carry plain text; a new external
value has no linewise flag. The sample adapter retains the flag for its own
local fallback or a terminal read matching its most recent local copy.

The host rejects completions made stale by selection, revision, focus,
attachment, modal, read-only, or newer-request changes. Cut removes text only
after successful export. The default budget is 65,536 UTF-8 bytes and may be
raised, together with the negotiated DCurses/Terminal transport limit, to the
16 MiB hard maximum. No operation silently truncates text.

## Checkpoints, snapshots, and save policy

Snapshots are immutable, bounded, and disposable. Capture a snapshot for a save,
write that exact snapshot outside the widget loop, and call `MarkSaved(snapshot)`
only after the write succeeds. A late save of an older state does not mark newer
text clean. `IsModified` compares state identity rather than revision number, so
undoing back to the saved state becomes clean again.

The editor does not choose encoding, newline conversion, atomic replacement,
backup, conflict, or recovery policy. Those decisions belong to applications
such as dedit.

## Caret and rendering ownership

After render/refresh, query `WidgetHost.GetCaretIntent()`. A visible focused
field/editor returns a position and insert/overwrite cursor style. The application
owns the DCurses cursor lease and must release or hide it when the intent is null,
when a modal covers the editor, and during shutdown.

Projection reads bounded source slices and caches bounded rows; it does not copy
the complete document for ordinary rendering. Unrenderable pathological clusters
use a safe placeholder without modifying source text.

## Default limits

| Resource | Default | Hard maximum |
| --- | ---: | ---: |
| Document text | 268,435,456 UTF-16 units | 1,073,741,823 units |
| One edit/paste | 16,777,216 units | 268,435,456 units |
| Undo groups | 4,096 | 65,536 |
| Retained history | 67,108,864 units | 536,870,912 units |
| Live snapshots | 16 | 128 |
| Projection source slice | 65,536 units | 1,048,576 units |
| Clipboard transport | 65,536 UTF-8 bytes | 16,777,216 bytes |

See [W1101](https://github.com/uniblab/Icod.Widgets/blob/main/docs/W1101-Contract-and-Storage-Decision.md)
for every cache/work limit and its failure behavior. See the
[editor sample](https://github.com/uniblab/Icod.Widgets/tree/main/samples/Icod.Widgets.Editor.Sample)
for a runnable application loop with file selection, clipboard completion,
modified status, caret coordination, and no file-writing behavior.
