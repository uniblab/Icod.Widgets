# W1111 — Single-Line Field Editing Parity

## Outcome

`WidgetTextInput` retains its 1.0 constructor, `Value`, `MaxTextElements`,
`WidgetValueChangedAction<string>`, single-line normalization, 4,096-element default,
100,000-element maximum, and ordinary Tab traversal. It now adds directional UTF-16
selection, selection replacement, bounded local undo/redo, contextual clipboard
requests, word navigation/deletion, read-only policy, selected rendering, and focused
caret intent.

`Anchor`, `Caret`, `SelectionStart`, `SelectionEnd`, and `HasSelection` use UTF-16
offsets and always land on complete text-element boundaries. `SetSelection` rejects
invalid or mid-cluster offsets. Shift extends from the directional anchor; an
unshifted Left/Right collapses an existing selection toward the corresponding edge.

## Compatibility and history

Typing, associated text, Backspace/Delete, Unicode movement, initial value
normalization, and tail visibility preserve the established field behavior. Every
successful mutation still emits exactly one `WidgetValueChangedAction<string>`;
selection-only changes emit no legacy value action. The field keeps at most 4,096
complete before-states, evicting the oldest whole state, and clears redo on a branch.
Undo/redo restores both value and directional selection.

Reducing `MaxTextElements` preserves whole text elements, clamps both endpoints,
clears history when the value is truncated, and invalidates pending bracketed or
clipboard work. Read-only and clipboard-budget changes also advance the field's
operation epoch so toggling a policy away and back cannot revive a stale completion.

## Clipboard and paste policy

The field participates in the W1109 request/result protocol with independent
buffer/view identities, revision, selection epoch, focus, and attachment guards.
Copy/Cut use strict UTF-8 and the 65,536-byte default budget. Cut remains deferred
until accepted; Paste completion is one local undo state.

Multiline editor paste remains exact. A single-line field deliberately applies its
1.0 normalization: whitespace runs become one space, line endings and tabs do not
survive, and other controls are removed. Bracketed paste is accumulated before this
normalization and commits only at End. A clipboard completion that would exceed the
text-element limit is rejected instead of silently truncating; direct legacy paste
retains the historical bounded truncation behavior.

## Caret and gallery witness

Field rendering styles the selected source range while preserving Unicode widths.
Focused caret intent uses the same retained visible slice and requests a steady bar.
The gallery's form-page description and smoke path now exercise selection replacement
and undo in addition to Unicode and normalized paste.

## Verification

- `WidgetTextInputEditingTests`: 7 passing scenarios for selection replacement and
  undo, single-line paste, old limit/action compatibility, focused copy command,
  Tab traversal, limit/paste invalidation, and Unicode caret geometry.
- Existing form, prompt, picker, routing, clipboard, and gallery smoke tests remain
  the compatibility gates.
- The complete net10.0 suite and clean net8/net9/net10 build remain the tranche gate;
  net8.0/net9.0 execution remains delegated to CI in this environment.
