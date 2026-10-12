# W1109 — Explicit Bounded Clipboard Requests

## Outcome

Editing controls now request clipboard work without owning terminal, operating-system,
thread, or async lifetimes. `WidgetClipboardRequestedAction` carries an immutable
`WidgetClipboardRequest`; the application performs its chosen transport/fallback
policy and returns a `WidgetClipboardResult` on the application loop through
`WidgetHost.ApplyClipboardResult`.

Operations are `Copy`, `Cut`, and `Paste`. Result statuses distinguish `Success`,
`Denied`, `Unsupported`, `TimedOut`, `Canceled`, `TooLarge`, `InvalidText`, and
`Failed`. A non-success result consumes a still-live request without mutating text.
Unknown, superseded, duplicate, detached, or otherwise stale completions return
`false` and cannot reach the old target.

## Identity and stale-completion guards

Each request captures a new request ID plus widget attachment generation, host focus
generation, view/document IDs, document revision, selection epoch and ordered range.
The host retains at most one request per widget. A new request supersedes the old one;
focus/scope loss and detach remove it immediately. Completion also revalidates the
live focused attachment and asks the target to revalidate revision, selection, and
read-only state before any action-capacity check or document mutation.

Reattaching the same object or reusing the same string widget ID receives a new
attachment generation. Moving focus away and back receives a new focus generation.
Neither can revive an earlier request. Opening a popup or modal also invalidates the
preceding focus lifetime even when an overlay reports the same logical owner.

## Copy and cut

Copy/Cut export exact selected text only when strict UTF-8 encoding fits
`WidgetEditorOptions.MaximumClipboardBytes`. The default is 65,536 bytes and the
supported maximum is 16 MiB. Byte accounting occurs before request publication;
payloads are never truncated. Invalid UTF-16 cannot be exported as strict UTF-8.

Cut retains the selection in the document until the application explicitly returns
`Success`. Only then does the editor reserve both semantic actions and commit one
undoable `Cut` transaction. Denial, timeout, cancellation, unsupported transport,
invalid data, oversize, generic failure, duplicate completion, and stale completion
all leave source text untouched.

## Paste

Paste requests contain no clipboard data. A successful result must contain strict
UTF-8-compatible text within both the clipboard byte budget and pending-paste UTF-16
budget. The editor revalidates every identity guard, reserves action capacity, and
applies the complete response as one exact transaction. Line endings, tabs, and
other text data are not normalized. Invalid or oversized success responses fail
closed without mutation.

## Sample adapter and capacity negotiation

`samples/Common/WidgetSampleClipboardAdapter.cs` demonstrates explicit use of
`CursesSession.WriteClipboardAsync` and `ReadClipboardAsync`, strict UTF-8 decoding,
typed exception mapping, and an application-selected local fallback. Success from a
terminal write means the complete frame was emitted; it does not prove the terminal
accepted or retained it. The sample policy may treat its own local fallback as an
accepted result because that choice belongs to the application, not Widgets.

DCurses sessions default their independent read and write limits to 65,536 bytes.
Programs that choose a larger Widgets budget must configure matching
`CursesClipboardOptions` before opening the session, up to the released 16-MiB
maximum. Widgets never enlarges a session, negotiates remote capacity, or silently
retries with a different transport.

## Verification

- `WidgetClipboardTests`: 15 passing cases including the 65,536/65,537 boundaries,
  multibyte text, no truncation, deferred cut, all controlled failure outcomes,
  stale focus/attachment/revision/selection/read-only guards, supersession,
  duplicate completion, strict invalid text, oversize response, and one-group undo.
- The shared adapter compiles against the released DCurses 2.4.0 public API.
- The complete net10.0 suite and clean net8/net9/net10 build remain the tranche gate;
  net8.0/net9.0 execution remains delegated to CI in this environment.
