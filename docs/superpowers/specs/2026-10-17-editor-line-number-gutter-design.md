# Editor Line-Number Gutter Design

**Date:** 2026-10-17  
**Status:** Proposed written specification for review

## Context

`WidgetTextEditor` already owns document projection, logical-to-visual row
mapping, wrapping, horizontal scrolling, caret placement, selection geometry,
and pointer hit testing. Applications cannot reproduce a synchronized gutter
without either duplicating that machinery or exposing projection internals as
public API.

The line-number gutter therefore belongs inside `WidgetTextEditor`. Widgets
owns the reusable mechanism and its terminal-safe defaults. Applications such
as `dedit` decide whether to enable it and may replace its styles through the
existing theme and scoped-style APIs.

## Goals

- Provide an opt-in, fixed left gutter containing one-based logical line
  numbers.
- Keep numbers synchronized with wrapped visual rows, scrolling, resizing,
  Unicode text layout, caret placement, and pointer hit testing.
- Give applications complete control over gutter colors and attributes through
  named widget style roles.
- Let `dedit` reproduce the useful presentation shown by `edit.exe`: a stable
  numbered margin and a distinct current-line indication.
- Preserve bounded projection work and existing behavior when the feature is
  disabled.

## Non-goals

This release does not add line selection, caret movement, breakpoints,
diagnostic markers, folding controls, relative numbering, custom gutter text,
custom separator glyphs, a general projection callback, a runtime gutter
toggle, or a persisted `dedit` preference. These may be designed separately if
real consumers require them.

## Public API

`WidgetEditorOptions` gains:

```csharp
public bool ShowLineNumbers { get; init; }
```

The default is `false`. Existing applications therefore retain their current
layout and input behavior without source changes.

`WidgetStyleRole` gains two values, appended after all existing values:

```csharp
EditorGutter,
EditorGutterCurrentLine
```

Appending the values preserves the numeric identity of all existing roles.
`WidgetPalette.Create` supplies capability-aware styles for both roles.
`EditorGutter` uses a subdued foreground over the editor surface.
`EditorGutterCurrentLine` uses a stronger foreground over a background
compatible with `EditorCurrentLine`. Applications can replace either complete
style through `WidgetTheme.Parts` or `Widget.Styles`.

The current gutter style is independent of
`WidgetEditorOptions.HighlightCurrentLine`. An application may show the current
line number without showing a full-width text band, or make the two styles
visually identical if no gutter emphasis is wanted.

## Geometry

When line numbers are enabled, the natural gutter width is the number of ASCII
decimal digits needed to display `Document.LineCount`, plus one trailing spacer
column. The number is one-based and right-aligned. Width is based on the whole
document rather than visible rows so ordinary scrolling does not move the text
boundary.

If reserving the natural gutter would leave no text column, the editor
temporarily suppresses the gutter. It never truncates a line number and never
consumes the final editable column. The gutter reappears automatically after a
resize provides sufficient room.

The editor divides its bounds into:

1. a fixed gutter rectangle; and
2. a text viewport occupying all remaining columns.

Only the text viewport width is passed to `WidgetTextProjection`. Horizontal
offsets and horizontal scroll ranges are measured within the text viewport.
The gutter never moves horizontally. Caret coordinates are translated by the
gutter width. Vertical geometry and page size are unchanged.

Changing the document between decimal thresholds, such as 99 to 100 lines,
changes the gutter width on the next layout/render operation. That may reflow
wrapped text by one column and is intentional. Existing projection cache keys
already include viewport width, so stale layouts must not be reused.

## Projection and rendering

The existing internal projected-row result remains internal. No projection
objects or rendering callbacks become public API.

For each visible projected row:

- The logical line number is rendered only when `VisualLineIndex` is zero.
- Wrapped continuation rows contain no repeated number.
- Every gutter cell is filled with `EditorGutter` before the number is written.
- Every visible row belonging to the caret's logical line instead uses
  `EditorGutterCurrentLine`, including blank continuation gutter rows.
- A disabled editor uses its disabled presentation and does not emphasize the
  caret line.

The text viewport retains the established rendering order: surface, projected
text, current-line band when enabled, and selection last. Selection never
extends into the gutter. The gutter is rendered from visible projected rows
only, so the feature does not scan the whole document or allocate one label per
document line.

ASCII decimal digits are used regardless of the current culture. This keeps
width calculation deterministic and avoids terminal-width ambiguity.

## Pointer behavior

The gutter is decorative.

- A primary-button press that begins in the gutter does not move the caret,
  alter the selection, select a line, or begin a selection drag.
- The associated move and release events have no document effect, even if the
  pointer later enters the text viewport.
- Host-level focus behavior is unchanged: because the gutter is part of the
  editor's interaction region, pressing it may focus the editor.
- A selection drag that begins in the text viewport continues normally if the
  pointer crosses into or beyond the gutter. Leftward autoscroll and selection
  extension therefore remain available.
- Mouse-wheel events over the gutter continue to scroll the editor because
  they are scrolling gestures rather than gutter activation.

Text hit testing subtracts the gutter width before mapping a pointer column to
a source offset. Horizontal autoscroll boundaries use the text viewport rather
than the complete editor bounds.

## `dedit` integration

After a Widgets package containing this API is published, `dedit` updates its
Widgets dependency and creates each `WidgetTextEditor` with both
`ShowLineNumbers = true` and `HighlightCurrentLine = true`.

The gutter is always enabled in `dedit` for this release. No menu command or
preference is added. `DeditTheme` may override the two gutter roles if the
built-in palette does not provide sufficient visual continuity with the
current-line band; this does not introduce `dedit`-specific rendering logic.

The dependency sequence is deliberate:

1. implement, qualify, merge, and publish the Widgets release;
2. rebind `dedit` to that published package;
3. enable and qualify the feature in the existing `dedit` 1.0.0 work.

Neither repository is merged automatically.

## Compatibility and failure behavior

- `ShowLineNumbers = false` follows the current rendering, projection, caret,
  scrolling, and pointer paths.
- Invalid configuration is impossible because the new option is Boolean.
- A zero-sized editor renders nothing, as before.
- Insufficient width suppresses only the gutter; editing remains available.
- Unsupported color axes and attributes are filtered through the existing
  capability-aware palette logic.
- Projection limits, cache bounds, source-read budgets, and document limits do
  not change.

## Verification

Widgets tests cover:

- default-off compatibility;
- one-based, right-aligned numbering at digit-width boundaries;
- automatic width changes after edits and resizes;
- fixed positioning during horizontal scrolling;
- first-row-only numbering for wrapped logical lines;
- current-line gutter styling across all wrapped rows;
- independence from the text current-line option and selection precedence;
- caret, Unicode text, and pointer hit testing after the text inset;
- decorative gutter press/move/release behavior;
- text-originated selection drags crossing the gutter;
- wheel scrolling over the gutter;
- narrow-view suppression and restoration;
- disabled-editor presentation;
- direct-RGB, indexed-color, limited-color, and attribute-only palettes;
- bounded work for large documents; and
- package-consumer compilation against the new public API.

The editor sample enables line numbers and demonstrates wrapping, current-line
presentation, selection, pointer input, horizontal scrolling, and resize.
README coverage remains concise; the editing guide, sample documentation,
changelog, and curated release notes contain the operational detail.

After rebinding, `dedit` tests verify that its editor enables both presentation
options, line numbers occupy the extreme left of the editor area, the current
logical line is distinguishable, gutter presses leave caret and selection
unchanged, and tiny viewports preserve an editable text column. Its README
briefly describes the visible editor, while complete release history remains in
the changelog and curated 1.0.0 notes.
