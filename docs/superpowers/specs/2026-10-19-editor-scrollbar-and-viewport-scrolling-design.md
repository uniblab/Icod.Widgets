# Editor Scrollbar and Viewport Scrolling Design

**Date:** 2026-10-19  
**Status:** Proposed written specification for review

## Context

`WidgetTextEditor` already owns text projection, wrapping, caret placement,
selection geometry, pointer hit testing, and scroll ranges. Its current
vertical offset is a logical-line offset, however, so it cannot stop at a
wrapped continuation row. Ctrl+Up and Ctrl+Down are also currently treated as
ordinary caret movement.

The reference editors demonstrate two related behaviors:

- the editor presents a one-column vertical scrollbar only when the document
  exceeds the viewport; and
- Ctrl+Up/Down scroll the viewport rather than directly navigating the caret.
  The caret keeps its document position while it remains visible, then is
  pulled along the nearest viewport edge when further scrolling would hide it.

Widgets must own both mechanisms because only the editor has the reusable
logical-to-visual projection needed to make them correct under word wrap.
Applications such as `dedit` opt in, choose theme styles, and report the
resulting document position.

## Goals

- Add an opt-in, auto-hidden vertical scrollbar to `WidgetTextEditor`.
- Render the scrollbar with terminal-safe one-cell glyphs: `▲`, `▼`, `│`, and
  `┃`.
- Make every vertical scrolling route operate in displayed rows, including
  wrapped continuation rows.
- Make Ctrl+Up/Down scroll without moving the caret until the caret reaches a
  viewport edge, then pull the caret along that edge.
- Apply the same caret-clamping rule to mouse-wheel, arrow-button, track, and
  thumb-drag scrolling.
- Preserve the caret's preferred visual column when an edge clamp moves it.
- Keep projection, indexing, memory use, and per-update work bounded for large
  documents and very long logical lines.
- Preserve all existing behavior unless the scrollbar option is enabled,
  except that Ctrl+Up/Down become viewport-scroll commands for every text
  editor.

## Non-goals

This release does not add a horizontal editor scrollbar, scrollbar marks,
minimap presentation, arbitrary consumer glyph callbacks, line folding,
smooth/sub-cell scrolling, a persisted `dedit` scrollbar preference, or a
public projection-row API.

## Public API

`WidgetEditorOptions` gains one additive property:

```csharp
public bool ShowVerticalScrollBar { get; init; }
```

The default is `false`. Existing consumers retain their editor width and
presentation without source changes. Ctrl+Up/Down behavior is corrected at the
editor level and is not conditional on scrollbar visibility.

No new style roles are required. The scrollbar uses the existing
`ScrollTrack`, `ScrollThumb`, and `ScrollFocusedThumb` roles, so applications
can already replace the complete styles through `WidgetTheme.Parts` or
`Widget.Styles`.

`WidgetTextViewState.VerticalOffset` and
`SetScroll(long horizontalOffset, long verticalOffset)` retain their public
logical-line meaning for compatibility. Calling `SetScroll` starts at the
first displayed row of the requested logical line. Widgets stores the wrapped
continuation component of the viewport anchor internally.

`WidgetTextEditor.VerticalScrollRange` reports displayed-row units when
wrapping is enabled and logical rows when wrapping is disabled. Its existing
`IsExtentExact` flag continues to distinguish exact ranges from progressively
measured wrapped ranges.

## Ownership split

### Icod.Widgets 1.9.0

Widgets owns:

- the option and auto-hide policy;
- scrollbar geometry, glyphs, rendering, theme roles, pointer capture, and
  hit testing;
- the logical-line plus wrapped-row viewport anchor;
- bounded wrapped-row measurement and scroll-range refinement;
- Ctrl+Up/Down, wheel, arrow, track, and thumb scrolling;
- caret edge clamping and selection notification;
- samples, public API records, and reusable documentation.

### Icod.Dedit 1.0.0 candidate

After Widgets 1.9.0 is published, Dedit owns only:

- rebinding its package dependency to 1.9.0;
- setting `ShowVerticalScrollBar = true` for document editors;
- any Dedit-specific color overrides through existing style roles;
- verification that the gutter, current-line band, scrollbar, status position,
  wrapping, empty workspace, and compact layouts compose correctly.

Dedit does not render, hit-test, index, or synchronize the scrollbar itself.

## Viewport position and wrapped-row index

The internal vertical viewport position is a pair:

1. a zero-based logical line; and
2. a zero-based displayed row within that logical line.

The second component is zero in no-wrap mode. Public `VerticalOffset` exposes
the first component, preserving its established meaning.

Widgets maintains a bounded, width-keyed wrapped-row map. Entries are keyed by
document identity/revision, logical line, viewport text width, tab width, wrap
mode, and text-width provider. Known entries contain the displayed-row count
for that line. Unknown lines contribute a conservative one-row estimate until
measured. Edits invalidate the changed line and all affected prefix totals;
width, tab, wrap, or width-provider changes select a different map key.

Nearby scrolling measures only enough rows to find the new viewport anchor and
fill the viewport. Scrollbar range values refine as additional lines are
measured. `IsExtentExact` becomes true only when the relevant row counts are
known. A request for the absolute beginning or end resolves exactly to the
document boundary. No operation allocates a row object for every document row
or performs an unbounded whole-document scan.

## Visibility and geometry

Scrollbar visibility is determined against the editor's full text width:

1. probe up to one displayed row beyond the viewport;
2. if the complete document fits, keep the scrollbar hidden and give the text
   the full width;
3. if overflow exists, reserve the rightmost column and project text using the
   reduced width.

This ordering prevents show/hide oscillation when reserving the scrollbar
column changes wrapping. If bounded work cannot yet prove that wrapped content
fits, measurement continues from retained state; the control may conservatively
show the scrollbar during discovery but must hide it as soon as fit is proven.

The scrollbar is suppressed when fewer than two rows are available because
both required buttons cannot be shown. With exactly two rows, only `▲` and `▼`
are rendered. With three or more rows, the interior is the track and contains
a proportional thumb of at least one cell.

The line-number gutter remains at the extreme left, the text viewport occupies
the middle, and the optional scrollbar occupies the extreme right. Selection,
current-line styling, and the physical caret never paint over the scrollbar.

## Rendering and theme behavior

The vertical scrollbar renders:

- `▲` in the top cell;
- `▼` in the bottom cell;
- `│` in unoccupied track cells; and
- `┃` in thumb cells.

All glyphs are one terminal cell under the DCurses width provider. Buttons and
track use `ScrollTrack`; the thumb uses `ScrollThumb`, or
`ScrollFocusedThumb` when the owning scrollbar/editor has focus. Disabled
presentation uses `Disabled`.

The standalone vertical `WidgetScrollBar` adopts the same glyph and button
geometry so the public control and the integrated editor chrome do not present
conflicting interaction models. Horizontal scrollbar behavior is unchanged in
this release.

## Input behavior

Every successful vertical operation first computes a new displayed-row
viewport anchor, then applies caret clamping:

- Ctrl+Down scrolls by one displayed row.
- Ctrl+Up scrolls by one displayed row.
- Wheel down/up retains the existing three-row step but measures those steps in
  displayed rows.
- Clicking `▼` or `▲` scrolls by one displayed row.
- Clicking the track below or above the thumb scrolls by one viewport page.
- Dragging the thumb maps its proportional destination to a displayed-row
  target; dragging to either limit resolves the exact document boundary.

Control must be present and Alt absent for Ctrl+Up/Down. Shift does not extend
selection for these viewport-scroll commands. Ordinary Up/Down and
Shift+Up/Down retain caret-navigation and selection behavior.

If the caret remains visible after scrolling, its anchor, caret, affinity,
selection, preferred visual column, and selection epoch do not change. If the
caret would leave the viewport:

- scrolling toward the document end places it on the topmost displayed row;
- scrolling toward the document beginning places it on the bottommost
  displayed row; and
- hit testing that edge row uses the existing preferred visual column and
  Unicode text-element boundaries.

An edge clamp collapses any selection to the new caret and emits the existing
`WidgetTextSelectionChangedAction`. A viewport-only change emits no selection
action. Dedit therefore updates its `real-line:character-position` status only
when the caret actually moves.

The same rule applies to Ctrl keys, wheel input anywhere in the editor,
scrollbar buttons, track paging, and thumb dragging. Pointer capture is
cancelled on release, focus loss, detach, modal interruption, resize, or
capture cancellation.

## Compatibility and failure behavior

- `ShowVerticalScrollBar = false` preserves the current editor width and
  rendering path.
- No-wrap `VerticalOffset` and vertical range values remain line based, which
  is also displayed-row based in that mode.
- Public `SetScroll` resets the internal wrapped-row component to zero.
- Zero-sized, one-row, and zero-column editors remain safe and editable where
  a text cell exists.
- A document that fits has no scrollbar cells and recovers the rightmost text
  column.
- Document edits, undo/redo, wrap changes, tab-width changes, resize, and
  Unicode-width-provider changes invalidate or re-key measured row data.
- Saturating arithmetic is retained for very large documents and offsets.
- Existing projection source-unit, cache, and work limits remain authoritative.
- The release remains AnyCPU and targets .NET 8, 9, and 10 with C# 13.

## Verification

Widgets tests cover:

- default-off API compatibility and explicit opt-in;
- hidden scrollbar for fitting wrapped and unwrapped documents;
- visibility after overflow and after edits/resizes cross the fit threshold;
- rightmost-column geometry with and without the line-number gutter;
- exact `▲`, `▼`, `│`, and `┃` rendering and theme roles;
- two-row and narrower compact behavior;
- one-displayed-row arrow and Ctrl scrolling through wrapped continuations;
- three-displayed-row wheel scrolling;
- page and proportional thumb behavior;
- Ctrl and pointer scrolling that leave a visible caret unchanged;
- downward top-edge and upward bottom-edge caret pulls;
- preferred-column preservation across short and wide wrapped rows;
- selection collapse and action emission only when an edge pull occurs;
- ordinary Up/Down and Shift+Up/Down compatibility;
- exact document limits, large ranges, rapid drag/wheel input, and pointer
  capture cancellation;
- row-map invalidation after edit, undo/redo, wrap, tab, resize, and width
  provider changes;
- bounded work and allocation for large documents and million-unit lines;
- package-consumer compilation against the new option; and
- editor sample smoke coverage with line numbers, wrapping, scrollbar clicks,
  Ctrl scrolling, wheel scrolling, and auto-hide.

The root README receives only concise usage and option documentation. The
editor sample guide, changelog, and curated `docs/releases/1.9.0.md` contain
the operational and release detail.

After Widgets 1.9.0 is published, Dedit tests verify opt-in construction,
scrollbar placement, auto-hide, wrapped-row stepping, caret-edge pulls,
line/character status changes, themed rendering, gutter composition, and no
scrollbar in the empty workspace.
