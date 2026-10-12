# W1107 — Multiline Editor

## Outcome

`WidgetTextEditor` is a public, focusable multiline control over a caller-owned
`WidgetTextViewState`. It composes the document/history and bounded projection
layers instead of copying document text into a control-specific buffer. The control
borrows the view and document; their lifetimes remain the caller's responsibility.

The editor supports directional selection, grapheme movement and deletion, the
approved Unicode word categories, logical-line Home/End, document-bound Ctrl+Home/
Ctrl+End, vertical/page navigation with a retained preferred cell column, selection
replacement, undo/redo, insert/overwrite mode, indentation, configurable newline
insertion, and exact multiline paste.

## Mutation and action contract

Every successful transaction emits `WidgetTextEditedAction`, carrying only document
identity/revision/state, followed by `WidgetTextSelectionChangedAction`, carrying the
view identity and directional endpoints. The control reserves capacity for both
actions before calling `WidgetTextDocument.Apply`, `Undo`, or `Redo`; capacity failure
therefore leaves document and selection unchanged. Navigation emits only the
selection action and also checks capacity before moving.

Typing and adjacent deletion opt into the document's 750-ms coalescing policy when
`AllowTypingCoalescence` is enabled. Paste, indentation, outdentation, newline,
overwrite, and selection replacement remain distinct transactions. Undo restores
the invoking view's recorded directional selection.

Read-only mode permits navigation and selection but consumes and rejects mutation.
It disables undo/redo mutation and cancels an incomplete bracketed paste. Clipboard
Copy/Cut/Paste commands remain disabled until the typed request/completion protocol
is added in W1109; direct terminal paste input is already supported here.

## Input and paste behavior

- Text elements, including combining and ZWJ sequences, are movement/deletion units.
- Overwrite replaces at most the next complete element and never crosses a logical
  line ending. At the line end it inserts before the ending.
- Ctrl word movement groups Unicode letters, decimal digits, connector punctuation,
  and combining marks; whitespace is a separate run; other symbols/punctuation are
  separate runs.
- Tab inserts the configured indentation when there is no selection. With a
  selection, Tab indents each covered logical line and Shift+Tab removes one
  configured/tab/space indentation unit in a single undo group.
- Enter inserts `NewLine` (`\n` by default) and, when enabled, the current line's
  leading spaces/tabs. Existing endings and pasted data are never normalized.
- Associated key text wins over its scalar character and is inserted exactly once.
- Legacy `WidgetInput.Paste(string)` is one transaction. Bracketed Begin/Data/End
  input accumulates within `MaximumPendingPasteUnits` and commits only on End.
  Oversize, interrupted, stale-revision, changed-selection, or read-only sequences
  are discarded without partial mutation.

## Bounded presentation

Rendering uses W1106 projection slices and DCurses retained text layout. Tabs, wide
characters, combining text, safe control placeholders, selection spans, and true
source/cell mappings share the same geometry used by navigation. The editor never
creates a full-document display string or pad.

## Verification

- `WidgetTextEditorTests`: 11 passing scenarios for grapheme edits, word/page
  movement, atomic selection replacement, undoable indent/outdent, overwrite line
  boundaries, read-only mode, associated text, bracketed paste, oversize/interrupted
  paste, full action queues, and retained rendering of tabs/wide text/selection.
- The complete net10.0 suite and clean net8/net9/net10 build remain the tranche gate;
  net8.0/net9.0 execution remains delegated to CI in this environment.
