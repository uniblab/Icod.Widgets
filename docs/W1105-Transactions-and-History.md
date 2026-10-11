# W1105 — Atomic transactions and checkpoint-aware history

`WidgetTextDocument.Apply` accepts one or more nonoverlapping replacements
addressed against an expected revision. Every range, UTF-16 text-element/CRLF
boundary, configured length, undo-retention budget, selection, and resulting
fragmentation state is validated before the document root, revision, history, or
saved-state bookkeeping changes. Expected failures return a typed status;
programming errors such as a null inserted string throw before mutation.

Edits in a batch use their common pre-edit coordinates and commit as one history
group. Change notifications carry ranges rather than document text and run only
after the new root, revision, and state identity are visible.

## Revision, state, and checkpoints

Revision increases for every Apply, Undo, and Redo mutation. State identity is
different: Undo and Redo return to the original state's identity, while a new
branch always receives a never-reused ID. `MarkSaved(snapshot)` records the exact
state written by the application. A late save of an older snapshot therefore
does not clean newer text; undoing to a reachable saved state does. History
eviction cannot accidentally declare a document clean.

Snapshots retain their immutable tree root through later edits, undo, redo, and
history eviction. A foreign or disposed snapshot cannot become a checkpoint.

## Grouping and bounds

History retains persistent before/after roots and accounts deleted plus inserted
UTF-16 units logically. A transaction larger than its configured retained-unit
budget is rejected rather than performed without undo. Once a valid group is
committed, oldest whole groups are evicted to satisfy the group-count and unit
budgets; redo is discarded only when a replacement branch actually commits.

Coalescing is opt-in in `WidgetTextEditContext`. Adjacent typing, backward delete,
or forward delete from the same view and kind may join within 750 ms. A saved
checkpoint, undo/redo, explicit noncoalescing operation, selection discontinuity,
kind change, timeout, or noncontiguous range starts a new group.

Text-element validation scans a bounded 4,096-unit context by default (maximum
65,536) and has a constant-time ASCII path. CRLF and valid surrogate pairs are
always indivisible. A pathological boundary that cannot be proven safe is
rejected; source text is never repaired or normalized.

Piece fragmentation uses the W1101 limit (one piece per eight live units, capped
at 1,048,576 and floored at 4,096). At the threshold, the current root is rebuilt
by streaming leaf-sized immutable chunks; no whole-document string is formed,
and retained snapshots/history continue to reference their prior roots.

## Measured evidence

On the W1101 Ubuntu x64/.NET 10 environment, 5,000 alternating end edits against
the 1,300,000-unit/100,000-line fixture, followed by all reachable undos, took
18.99 ms and allocated 9,105,960 bytes in Release. The default 4,096 complete
groups remained undoable, the oldest 904 were evicted whole, and a snapshot held
before the trace still matched the original text exactly.

Deterministic and randomized tests cover batch coordinates/atomic rejection,
stale revisions, undo branching, checkpoint timing, retention trimming,
coalescing boundaries, CRLF/combining/surrogate rejection, line aggregates,
fragmentation maintenance, and snapshot survival.
