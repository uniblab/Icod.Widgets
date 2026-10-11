# W1108 — Scrolling and Pointer Selection

## Outcome

`WidgetScrollBar` adds an independent horizontal or vertical long-range control,
while `WidgetTextEditor` now supports primary-button placement, Shift extension,
captured drag selection, axis-specific wheel scrolling, and bounded autoscroll.
Scrollbars deliberately do not own or move an editor caret; applications bind a
`WidgetScrollChangedAction` to a view and refresh the bar with `SetRange`.

`WidgetTextEditor.HorizontalScrollRange` and `VerticalScrollRange` expose extent,
viewport, offset, and exact-versus-estimated state. No-wrap vertical extent is the
exact indexed logical-line count. Wrapped vertical extent is explicitly estimated.
Horizontal extent is estimated for unwrapped documents because undisplayed logical
lines are not scanned merely to size a scrollbar; wrapped presentation has no
document-driven horizontal extent.

## Scrollbar behavior

- `SetRange(long extent, long viewport, long offset)` validates nonnegative inputs
  and clamps offset to `max(0, extent - viewport)`.
- Thumb length and position use `BigInteger` intermediate arithmetic. Values near
  `long.MaxValue` therefore cannot overflow while mapping to a short terminal track.
- Arrow keys move one unit, PageUp/PageDown move one viewport, and Home/End reach
  either boundary. Track clicks page; primary drag maps the usable thumb travel.
- Vertical bars accept vertical wheel input and horizontal bars accept horizontal
  wheel input. Orthogonal wheel input is left unhandled.
- Zero extents, viewports, and one-cell tracks remain defined and non-draggable.
- Every user-originated offset change reserves action capacity before changing
  `Offset`, then emits `WidgetScrollChangedAction`.

## Editor pointer behavior

Pointer hit testing uses the exact retained W1106 projection for tabs, wide text,
combining sequences, controls, wrapping, and horizontal source origins. A primary
press starts a directional selection; Shift+press preserves the existing anchor.
Host pointer capture keeps move/release delivery with the editor outside its bounds.

An outside drag records direction but performs no private timing. Each
`WidgetHost.Update(timestamp)` applies at most `MaximumWorkPerUpdate` 50-ms scroll
steps, advances the caller-clock checkpoint, reprojects, and publishes at most one
selection change. Repeated calls at the same timestamp can drain a large elapsed
interval without exceeding one call's work ceiling. A backward timestamp is rejected
by the host before widget state changes.

Resize, terminal focus loss, focus movement, modal/popup activation, hide, disable,
detach, and host disposal all use the host's capture-cancellation hook. Cancellation
clears drag direction, width-provider reference, and caller-clock state immediately;
a later attachment cannot inherit the old drag.

## Verification

- `WidgetScrollBarTests`: 5 passing scenarios covering near-`long.MaxValue` thumb
  drag, zero/short tracks, track/keyboard paging, editor-caret independence, and
  rapid alternating wheel/pointer input.
- `WidgetEditorPointerTests`: 5 passing scenarios covering outside drag extension,
  Shift anchor retention, bounded monotonic ticks, exact/estimated ranges, and
  cancellation for focus loss, modal activation, resize, hide, and detach.
- The complete net10.0 suite and clean net8/net9/net10 build remain the tranche gate;
  net8.0/net9.0 execution remains delegated to CI in this environment.
