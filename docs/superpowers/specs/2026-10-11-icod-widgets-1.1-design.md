# Icod.Widgets 1.1 — Editing Foundation Design

**Date:** 2026-10-11\
**Status:** approved release direction; proposed API/resource details for roadmap review\
**Scope:** reusable single-document editing, not the dedit executable

## Goal and ownership

Provide an exact-text document with history, independent view state, and a
multiline terminal editing control. Improve host input fidelity and basic text
fields so the components can later support a tabbed workspace without redesign.
The [main roadmap](../../../ROADMAP.md) owns the 1.1/1.2 split.

Widgets owns text/edit semantics. DCurses 2.4 owns projection, Unicode cell
geometry, interaction mechanisms, clipboard transport facade, and cursor leases.
The application owns file identity, decoding/encoding/BOM, EOL conversion policy,
metadata-aware dirty state, safe saving/conflict detection, search execution,
document lifetime, command orchestration, terminal loop, and elapsed time.

## Global constraints

- C# 13; `net8.0;net9.0;net10.0`; AnyCPU; managed implementation.
- Sole direct runtime dependency: `Icod.DCurses 2.4.0`; Terminal 1.29.0 is transitive.
- Stable 1.0 public API compatibility; `AssemblyVersion` remains `1.0.0.0`.
- Library LGPL-3.0-or-later; samples/tests GPL-3.0-or-later with full prescribed headers.
- Application-owned input reader, event loop, async completion, and monotonic time.
- No terminal protocol strings, native/OS clipboard calls, or Widgets file writes.
- Preserve screen-only `WidgetHost` construction and existing session overload.
- Escape remains Cancel in dialogs, including OK-only dialogs; mnemonics remain distinct from shortcuts.
- Sequential implementation without subagent delegation; owner controls merge/tag/publication.

## Current integration gaps

The 1.0 `WidgetInput.From` drops function-key number, keyboard event phase,
paste phase, and focus state. The host handles Tab before the focused control,
gives menu shortcuts priority over contextual editing, and captures releases
without consistently routing outside moves to the owner. The host's session
constructor retains only the screen. Action queue helpers and capture-cancel
callbacks are internal. Text input is bounded single-line editing with no
selection/history. The scroll indicator is not an interactive scrollbar.

These are first-class prerequisites, not editor-local workarounds. Preserve old
constructors, factory defaults, enum numeric values, and record deconstruction;
add explicit metadata/hooks. Do not rewrite unrelated menus, pickers, dialogs,
or presentation architecture in this release.

## Document and coordinates

Use opaque document identity and two distinct monotonic concepts: revision
identifies a mutation observation; state identity identifies a history position.
Undo/redo advances revision but returns to the original state identity. A saved
checkpoint refers to identity, never just an undo count or a hash collision.

Use `long` UTF-16 offsets/line ordinals at the document boundary and `int` lengths
for bounded returned slices. Offsets are not bytes, grapheme indexes, or cells.
Expose indexed line starts/content lengths/line-ending lengths without joining
the entire document. Empty text has one logical line; a final line ending leaves
an empty final line. Treat CRLF as an indivisible line-ending unit for editing.

Retain text exactly, including tabs, CR/LF/CRLF mixtures, final-newline presence,
isolated surrogate units, and control characters. View sanitization cannot edit
the source. Movement/deletion respects full text-element boundaries; invalid
Unicode renders safely without silently changing data. Pixel shaping/BiDi/IME
composition is not promised beyond text that the terminal reports.

Construct from caller-supplied chunks; the library does not read a filename.
Input chunks may split CRLF, surrogate pairs, or combining sequences. Build the
line index incrementally; edits invalidate affected ranges, not every line.
Evaluate at least gap-buffer and piece-table/tree candidates before committing
the private storage design. Public types must not expose that storage choice.

Snapshots are immutable, document-identified, revision/state-stamped, and
explicitly disposable. The application may hand a snapshot to a worker; mutation
and result application stay on the owning loop. Disposal releases retained state
predictably; using a disposed snapshot fails explicitly. Preserve snapshot data
through later edits/undo until disposal. Bound retained snapshots/history as
well as visible caches; no hidden unbounded piece fragmentation.

## Atomic editing and history

A transaction contains one or more nonoverlapping replacements addressed against
one expected revision. Validate ranges, grapheme/CRLF boundaries, resource limits,
and overlap before changing text or emitting actions. Failure changes neither
text, revision, selection, nor history. Return typed rejection for expected stale
or capacity failure. Programming errors may throw before mutation.

One selection replacement, paste, indentation command, or external replacement
batch is one undo group. Contiguous typing may coalesce only under explicit
policy with the caller's monotonic time; break at navigation, selection change,
focus change, paste, checkpoint, and noncontiguous edits. Undo restores caret and
selection for the invoking view; other views reconcile to the new revision.
After undo then a new edit, discard redo without reusing old state identities.

Keep history bounded by group count and retained text accounting. Never silently
perform an un-undoable oversized operation: reject atomically when an individual
transaction cannot meet the configured undo contract. Evict oldest whole groups
only after validation. Eviction does not make a modified document clean.

`MarkSaved(snapshot)` records the exact text state written by the application.
If snapshot N finishes saving after edit N+1, the current document stays modified.
Undo to N clears text-modified state if N remains reachable. Metadata changes
(encoding, BOM, EOL policy) remain the app's additional dirty reasons. Widgets
does not imply that a save occurred merely because a checkpoint was marked.

## View state and projection

Keep caret, anchor/active selection, preferred vertical cell column, wrapping,
tab width, horizontal scroll, and vertical scroll per view, separate from the
document. A single selection is directional. Programmatic document changes
transform/clamp view anchors to legal boundaries; view-only changes do not dirty
text. Support two views in tests without promising split-view UI.

Use DCurses Unicode/cell layout and hit testing on bounded source slices. A slice
must carry the true source origin and enough boundary state for tabs, CRLF,
graphemes, and continuation; slicing a combining sequence to fit a byte budget
is not acceptable. Preserve the preferred visual column across short lines.

Cache by document identity/revision, affected source range, width, tab/wrap
configuration, and display policy. Reuse unaffected work; invalidate only what
changed. Bound cached rows and UTF-16 content independently. Handle a pathological
single cluster using a safe placeholder and explicit bounded scan/continuation
policy, without changing text or claiming partial-cluster editing is safe.

No file-sized pad, whole-document string, or whole-long-line layout on every
keystroke. Initial indexing and cold far-offset navigation may do measured work;
incremental work must be cancellable or time-sliced where it could monopolize the
loop. Wrapped visual-row totals may be estimated until indexed: expose that
state rather than fake exact geometry. No-wrap uses indexed logical lines.

Freeze limits and measured ceilings at W1101: document length, single edit/paste,
history groups/retained units, retained snapshots, projection slice/cluster scan,
cache rows/units, and work per update. Record local measurements and formulas;
do not invent a universal large-file limit from the DCurses synthetic sample.

## Editing and input routing

Preserve function-key identity, associated text, supported modifiers, focus
state, key press/repeat/release, and paste lifecycle. Existing synthetic Key
events mean press; existing `Paste(string)` means one complete paste. Release
events never type or invoke a command. Associated text is inserted exactly once,
not once as a key and again as text. Treat pasted control bytes as text data,
never terminal commands or menu gestures.

Priority: active overlay/modal, then eligible focused edit command, then existing
host/menu application shortcuts. Explicit menu-opening navigation still works.
Do not steal Ctrl+C from application command handling when no text target exists.
Editor Tab indents; Shift+Tab outdents. Ordinary fields retain Tab traversal.
Provide an explicit host focus-movement API so the application can bind an escape
from editor focus (the sample uses F6/Shift+F6) without guessing a terminal key.

Support grapheme left/right and deletion, word movement/deletion, Home/End and
document bounds, vertical/page movement with preferred cell column, Shift
selection, Ctrl+A, selection replacement, undo/redo, indent/outdent, and explicit
insert/overwrite mode. Read-only allows navigation, selection, copy, and scrolling,
but no editing, cut, undo/redo mutation, or paste. New Enter uses configured newline
and indentation policy; existing text is never normalized to that policy.

Collect bracketed paste phases into a bounded transaction; commit on completion.
If interrupted, canceled, or oversized, discard the pending transaction with no
partial text mutation. Do not freeze painting or allocate outside the budget
while gathering chunks. Configuration and incomplete-paste behavior are explicit.

## Pointer, scrollbars, and time

Use DCurses capture mechanisms through the host. Captured move and release route
to the owner even outside bounds; a scrollbar does not accidentally move the
document caret. Primary press and Shift+press establish/extend selection; drag
extends it. Add keyboard-operable horizontal and vertical scrollbars with
overflow-safe long-range thumb math, track paging, and non-draggable zero ranges.

Cancel capture and pending drag on focus loss, modal activation, resize, hidden
or disabled state, detach, and host disposal. A new attachment lifetime cannot
inherit old capture. Autoscroll uses `WidgetHost.Update(TimeSpan)` and the app's
clock; cap work per call and stop on cancellation. No timers/tasks inside a widget.
Backward timestamps fail before mutation, consistently with `Process`.

## Clipboard and caret integration

Controls emit immutable clipboard requests with operation, document/view
identity, revision, selection epoch, focus/attachment generation, and request ID.
The application chooses DCurses clipboard access, a local fallback, or rejection,
then applies a typed completion on its loop. At most one pending request per
target; a newer request invalidates the old one. Timeout, denial, unsupported,
invalid UTF-8, oversized payload, cancellation, and stale results are distinguishable.

Default DCurses read/write budgets are independently **65,536 bytes** and may be
configured up to **16 MiB** before session creation. Widgets does not negotiate
remote capacity or enlarge the session silently. Enforce bytes before export,
text/edit/history budgets before import, and never truncate silently. App-local
fallback is explicit. Cut commits deletion only after a completion the caller
accepts as successful; failed/stale copies leave text untouched. Transport write
completion alone cannot establish remote clipboard acceptance.

Paste applies only to the original still-valid target state. A different focus,
selection, revision, read-only state, overlay, detach/disposal, or reattachment
invalidates completion. Duplicate completion cannot reapply text or delete twice.

The host exposes focused caret intent: visibility, clipped screen position, and
insert/overwrite style. The application coordinates the actual DCurses cursor
position/visibility and optional async style lease. It must restore the observed
preceding style in LIFO order; unsupported/timed-out observation causes no guessed
reset. Offscreen/hidden/covered controls request no visible caret. No terminal
session is needed to compute intent or test rendering. Async style completions
are serialized/revalidated against focus lifetime before use.

## Single-line parity and public extension

Add directional selection, selection replacement, local undo/redo, and contextual
copy/cut/paste to `WidgetTextInput`. Reuse editing primitives where useful but
preserve its constructor limits, existing normalized single-line paste, Enter
behavior, and ordinary Tab traversal. Do not turn a one-line prompt into an editor
or change existing field limits to document limits.

Expose minimal protected action-capacity/emission, focus-policy, capture-cancel,
and time hooks needed by external controls. Preserve ownership validation and
bounded atomic action queues. Test an external consumer; an internal-only test
cannot prove extensibility. General composite-dialog APIs remain 1.2 work.

## Evidence and non-goals

1.1 includes public editing and clipboard examples, fake-service tests, randomized
document tests, performance evidence, package-only and old-binary consumers,
API snapshots for all TFMs, and environment-specific live qualification.
Unavailable live platforms are recorded as Not tested, not inferred from CI.

No tabs, file writes, safe-save engine, codecs, regex/search engine, composable
dialogs, save-target picker, printing, syntax highlighting, split views, multiple
carets, recovery, or OS-specific clipboard service in this release.
