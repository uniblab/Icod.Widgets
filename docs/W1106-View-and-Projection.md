# W1106 — View State and Bounded Projection

## Outcome

Icod.Widgets now separates document state from per-view editing state. Multiple
`WidgetTextViewState` instances can observe one `WidgetTextDocument` while retaining
independent selections, preferred columns, wrapping policy, and scroll positions.
View-only changes do not change the document revision or modified state.

The internal `WidgetTextProjection` converts only a bounded visible slice into
DCurses text geometry. It returns projected rows, source mappings, caret position,
selection spans, cache/work counters, continuation state, and whether the vertical
extent is exact. It never constructs a full-file pad, layout, or string.

## Public contract

- `WidgetTextViewState` borrows, but does not own, its document. Disposing a view
  unsubscribes it from document notifications and does not dispose the document.
- Anchor and caret offsets are UTF-16 text-element boundaries. Direction is retained;
  `SelectionStart` and `SelectionEnd` expose the ordered range.
- Document changes transform each live view independently. Insertions exactly at an
  endpoint retain that endpoint before the inserted text; endpoints after a change
  move by its net length; endpoints inside replaced text collapse to its start.
- `GetLineIndex(long)` and `IsTextElementBoundary(long)` provide the document queries
  required by editing navigation without exposing the private piece tree.
- `WidgetEditorOptions` publishes the projection/cache/work limits that an editor can
  choose within the W1101 supported maxima.

## Projection bounds and geometry

The default projection reads at most 65,536 UTF-16 units and performs at most 64
logical-line work items per update. Retained cache defaults are 512 rows and
1,048,576 UTF-16 units. A caller can lower these values or increase them only to the
documented supported maxima.

No-wrap horizontal seeking is progressive. A seek farther than the current budget
advances a revision-scoped scan cursor and reports incomplete work; subsequent
updates continue from that cursor. Once the target is reached, local DCurses layout
columns are translated back to true document columns for caret, selection, tabs,
and hit testing. This avoids both whole-line reads and `int` overflow at very large
scroll offsets.

Projection does not split a text element. When a single cluster exceeds the bounded
cluster scan, it emits a replacement placeholder and continuation marker rather than
retaining or laying out the entire cluster. C0/C1 display controls other than tab are
also represented safely without changing their UTF-16 source offsets.

No-wrap vertical extent is the exact logical line count. Wrapped extent is explicitly
reported as estimated because undisplayed lines are not laid out merely to calculate
a scrollbar range.

## Verification

- `WidgetTextProjectionTests`: 15 passing scenarios covering independent views,
  document reconciliation, preferred columns, selective cache invalidation, bounded
  long lines and clusters, tabs, Unicode/control round trips, tiny viewports, work
  deferral, progressive far seeks, far-selection translation, and extent honesty.
- The complete net10.0 suite and all-target build are the milestone gates. net8.0 and
  net9.0 execution remain delegated to CI because those runtimes are unavailable in
  the implementation environment.
