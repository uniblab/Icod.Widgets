# Icod.Widgets 1.10.0 Win32-Inspired Editor Scrollbar Design

**Date:** 2026-10-21  
**Status:** Approved design; implementation not started  
**Target:** Icod.Widgets 1.10.0, followed by an Icod.Dedit dependency and opt-in update

## Purpose

Bring the reusable `WidgetScrollBar` and the scrollbars owned by
`WidgetTextEditor` into one coherent, Win32-inspired interaction model. The
reference behavior is the classic Win32 `SCROLLBAR` control together with a
multiline `EDIT` control that owns and processes its horizontal and vertical
scrollbars.

This is behavioral inspiration rather than pixel emulation. Terminal cells,
Unicode display widths, bounded document work, and the already approved Dedit
caret-clamping rule remain authoritative where they differ from a graphical
Windows control.

## Goals

- Give vertical and horizontal scrollbars the same geometry and behavior.
- Add Win32-style unit, page, endpoint, thumb-drag, Shift+click, hold-repeat,
  and context-menu interactions.
- Add an opt-in, auto-hidden horizontal scrollbar to `WidgetTextEditor`.
- Compose horizontal and vertical editor bars without overlap or viewport
  oscillation.
- Route the standalone control and editor-integrated bars through one tested
  behavior engine.
- Keep rendering themeable and behavior independent of presentation.
- Preserve bounded processing and overflow safety for very large documents and
  `long` ranges.
- Keep the root README concise and put complete release history in the
  changelog and curated release notes.

## Non-goals

- Pixel-perfect reproduction of a specific Windows theme.
- Changing Icod.Terminal or Icod.DCurses in this release.
- Supporting the dedicated keyboard Menu/Apps key before that key is exposed by
  the input stack.
- Making editor-owned scrollbars independent tab stops. The editor remains the
  single focus owner.
- Enabling either editor scrollbar by default.
- Changing Dedit before Icod.Widgets 1.10.0 is published.

## Win32 Behavior Vocabulary

The design uses the Win32 request model:

- line/unit decrement and increment;
- page decrement and increment;
- start and end;
- live thumb tracking and final thumb position;
- end of a held scroll sequence.

For a multiline editor, one vertical unit is one displayed row, including a
wrapped continuation. One horizontal unit is one terminal display column, not
one UTF-16 code unit. A page is the current text viewport extent on that axis.

The multiline editor owns and processes its bars, as a Win32 `EDIT` control
does. Horizontal scrolling is available only in no-wrap mode. Wrapped text uses
the vertical visual-row range and no horizontal bar.

## Public API

### Editor option

Add one nonbreaking property:

```csharp
public bool ShowHorizontalScrollBar { get; init; }
```

Its default is `false`. `ShowVerticalScrollBar` remains unchanged and also
defaults to `false`.

When enabled, an editor bar is auto-hidden while its complete content fits.
`ShowHorizontalScrollBar` has no visible effect while wrapping is enabled.

### Anchored popup opening

Add a screen-anchored opening operation to `WidgetPopupMenu` so a reusable
popup can act as a context menu without being laid out as a persistent control:

```csharp
public void OpenAt( int row, int column );
```

The anchor is expressed in screen cells. Popup size is derived from its items,
clamped to the screen, and may move left or up to remain visible. Existing
constructors, `Open()`, bounds-based rendering, command dispatch, and menu-bar
behavior remain source and binary compatible.

Scrollbar-owned context menus use an internal command sink so their commands
are applied by the owning control rather than emitted as application commands.
Ordinary consumer-created popup menus continue to emit the existing actions.

No other new public type is required. Repeat timing and built-in scrollbar
commands are control behavior, not theme or application policy.

## Shared Geometry

Replace the vertical-only geometry factory and the separate horizontal math
with one orientation-neutral geometry value. It receives an axis length,
extent, viewport, and offset and returns:

- decrement-button cell;
- track start and length;
- thumb start and length;
- increment-button cell;
- maximum offset;
- hit target for an axis coordinate;
- offset for a drag delta;
- offset that centers the thumb on an absolute track coordinate.

Both orientations reserve one button cell at each end when the axis has at
least two cells. A two-cell bar contains only its buttons. A three-cell bar has
one track cell. Zero- and one-cell bars are inert and never perform arithmetic
on a nonexistent track.

Thumb size is proportional to `viewport / extent`, has a minimum of one track
cell, and fills the track when the range cannot scroll. All scaling uses
`BigInteger` intermediates and clamps to the nonnegative `long` range.

“Scroll Here” and Shift+track click place the thumb center on the requested
track coordinate, subject to endpoint clamping. The mapping uses the same
rounding rule in both orientations.

## Shared Interaction State Machine

An internal, orientation-neutral state machine translates input into semantic
scroll commands. `WidgetScrollBar` emits `WidgetScrollChangedAction` after
applying a command. `WidgetTextEditor` applies the same command to its view and
then performs editor-specific caret handling.

The semantic commands are:

- unit decrement;
- unit increment;
- page decrement;
- page increment;
- start;
- end;
- absolute offset;
- thumb-track offset.

Every input path calls this command layer. Mouse clicks, repeat ticks, keyboard
input, context-menu commands, and thumb dragging must not contain independent
offset logic.

Commands clamp before mutation. A handled command that is already at a boundary
does not emit a redundant change action.

## Pointer Contract

### Primary button

| Target | Press behavior | Hold behavior |
|---|---|---|
| Decrement button | Move one unit immediately | Repeat unit decrement |
| Increment button | Move one unit immediately | Repeat unit increment |
| Track before thumb | Move one page immediately | Repeat pages toward pointer |
| Track after thumb | Move one page immediately | Repeat pages toward pointer |
| Thumb | Begin live drag | Track pointer until release |

Track repeat stops when the thumb reaches or crosses the held pointer
coordinate. It must not page past that coordinate.

Shift+primary press on a track performs one absolute “Scroll Here” operation.
It does not start page repeat. Shift does not alter button behavior, and a
Shift+thumb press still starts an ordinary drag.

Thumb dragging updates content live. Release applies the final position and
ends capture. Moving outside the control while captured remains valid and
clamps at the endpoints.

### Hold repeat

The first action occurs on press. Repetition starts after a 400 ms initial
delay and advances every 50 ms. `WidgetHost.Update` supplies the monotonic
clock. One update performs at most `MaximumWorkPerUpdate` repeat steps so a
stalled host cannot create unbounded catch-up work.

Repeat and drag state cancel on:

- primary release;
- pointer-capture cancellation;
- host focus loss;
- resize or rearrangement;
- modal activation;
- detach;
- disablement or effective hiding.

### Secondary button

A secondary-button release over a bar opens its context menu at that pointer
location. A canceled or off-bar secondary sequence opens nothing. Opening the
menu cancels any active primary interaction.

### Wheel

Existing wheel increments remain compatible: vertical wheel events move a
vertical range and horizontal wheel events move a horizontal range. The
operation routes through the semantic command layer and clamps normally.

## Keyboard Contract

A focused standalone `WidgetScrollBar` implements the Win32 scrollbar-control
keyboard vocabulary:

- Up or Left: unit decrement;
- Down or Right: unit increment;
- Page Up: page decrement;
- Page Down: page increment;
- Home: start;
- End: end;
- Shift+F10: open the context menu at the current thumb.

Editor-owned bars are not focusable children. Existing editor caret keys remain
editor commands. Existing Ctrl+Up and Ctrl+Down viewport scrolling continues to
use the vertical shared command path. Embedded bar context menus are invoked by
secondary click only.

The dedicated Menu/Apps key is outside this release because it is not currently
represented by `CursesKey`/`WidgetKey`.

## Context Menus

Vertical menu:

1. Scroll Here
2. separator
3. Top
4. Bottom
5. separator
6. Page Up
7. Page Down
8. separator
9. Scroll Up
10. Scroll Down

Horizontal menu:

1. Scroll Here
2. separator
3. Left Edge
4. Right Edge
5. separator
6. Page Left
7. Page Right
8. separator
9. Scroll Left
10. Scroll Right

“Scroll Here” remembers the pointer coordinate that invoked the menu. For a
keyboard-opened standalone menu, the anchor is the current thumb center and
“Scroll Here” is disabled because it would be a no-op.

Endpoint and directional items are disabled when their operation cannot move.
Selecting an item closes the menu, restores prior focus, and invokes the same
semantic command used by direct input. Escape and outside clicks dismiss the
menu without scrolling.

The menu uses the existing popup theme, border, mnemonic, clipping, keyboard,
and pointer behavior. The built-in English captions are control strings; this
release does not add a localization subsystem.

## Rendering and Themes

Preferred single-cell glyphs are:

| Part | Vertical | Horizontal |
|---|---|---|
| Decrement | `▲` | `◄` |
| Track | `│` | `─` |
| Thumb | `┃` | `━` |
| Increment | `▼` | `►` |

If the active width provider does not measure the preferred glyph set as one
cell per glyph, use `^`, `|`, `#`, `v` vertically and `<`, `-`, `=`, `>`
horizontally.

Retain the existing `ScrollTrack`, `ScrollThumb`, `ScrollFocusedThumb`, and
`Disabled` roles. Add optional roles for buttons, hover, pressed state, and the
two-bar corner. Each new role falls back to an existing scrollbar or editor
surface role so existing themes remain complete and visually compatible.

Pointer hit testing always works. A theme may make hover and pressed roles
visually distinct or identical. Presentation never changes hit regions,
commands, repeat timing, offsets, or caret policy.

## Editor Layout

Extend `WidgetEditorViewport` with horizontal-bar and corner rectangles. Layout
is solved monotonically:

1. Begin with the gutter and full remaining text area.
2. Determine known/requested overflow on each axis.
3. Reserve each eligible bar.
4. Recalculate overflow against the reduced text rectangle.
5. Repeat until no bar is added.

Adding a bar only reduces the text viewport, so the calculation converges after
at most the two bars are added. It never removes a bar during the same layout
pass, preventing visibility oscillation.

When both bars are visible, the vertical bar owns the extreme-right column
except for the bottom-right corner; the horizontal bar owns the bottom row
except for that corner. The corner is inert and rendered with its theme role.

Suppress a bar when there is not enough space to leave a usable text cell and
its two end buttons. Existing one-row vertical suppression remains compatible;
the equivalent rule applies to narrow horizontal layouts.

## Horizontal Extent Discovery

No-wrap horizontal auto-hide requires the widest display-column width in the
document. Measuring an arbitrarily large document synchronously would violate
the editor's bounded-work contract.

Add a width cache keyed by document identity/revision, tab width, wrap mode,
and width-provider identity. Projected visible lines update the lower bound
immediately. `OnUpdate` scans additional logical lines, bounded by
`MaximumWorkPerUpdate`, until the extent is exact.

While discovery is incomplete:

- `HorizontalScrollRange.IsExtentExact` is `false`;
- extent is at least `HorizontalOffset + viewport columns`;
- the bar appears as soon as measured content proves overflow;
- a nonzero existing horizontal offset keeps the bar visible;
- a bar is not shown solely because an unseen line might overflow.

When a complete scan proves that all content fits, the bar remains hidden. If a
later exact extent is smaller than the current range, clamp offset and caret
transactionally before rendering.

Any document revision invalidates the exact width result. Re-discovery remains
bounded. Small documents normally complete in the first update; large
documents refine without blocking input.

In wrapped mode, horizontal extent equals the text viewport, horizontal offset
is normalized to zero, the range is exact, and the horizontal bar is hidden.

## Editor Caret Policy

Viewport movement and caret revelation are separate stages, following the
Win32 edit-control distinction between scrolling text and revealing the caret.
Widgets then applies the previously approved Dedit policy:

- leave a visible caret and selection unchanged;
- if vertical scrolling would move the caret above or below the text viewport,
  collapse it to the nearest visible displayed row while preserving preferred
  visual column where possible;
- if horizontal scrolling would move the caret left or right of the text
  viewport, collapse it to the nearest visible grapheme-cluster boundary;
- emit one selection-changed action only when clamping actually changes the
  selection/caret.

This policy applies equally to keys, wheel events, button presses, track pages,
Shift+click, context commands, repeat ticks, and thumb dragging.

## Failure, Cancellation, and Capacity Rules

- Reserve action capacity before mutating an offset or caret that must emit an
  action.
- Popup opening must either complete with valid focus/capture ownership or
  leave the prior state unchanged.
- Range, geometry, and repeat arithmetic must not overflow for `long.MaxValue`
  extents or delayed updates.
- A range change during drag/repeat cancels the active interaction.
- A disappearing editor bar cancels interaction and closes its context menu.
- Boundary operations are handled no-ops and do not enqueue actions.
- Detach and disposal leave no pending repeat, capture, popup, or background
  width-discovery mutation.

## Verification

### Unit tests

- Shared geometry for both orientations and lengths 0, 1, 2, 3, and normal
  tracks.
- Proportional thumb size/position and `long.MaxValue` overflow safety.
- Absolute scroll-here mapping and endpoint clamping.
- Every hit target and semantic command on both axes.
- Shift+track behavior; Shift+button and Shift+thumb behavior.
- Live drag, release, outside drag, and cancellation.
- Repeat delay/cadence, bounded catch-up, track stop-at-pointer, and every
  cancellation route.
- Context-menu labels, disabled states, remembered coordinate, command parity,
  dismissal, clipping, and focus restoration.
- Keyboard mapping including both directional-key pairs and Shift+F10.
- Preferred glyphs, ASCII fallback, theme fallback, hover, pressed state, and
  inert corner rendering.

### Editor integration tests

- Horizontal auto-hide for fitting and overflowing no-wrap documents.
- Horizontal suppression and offset normalization in every wrap mode.
- Vertical, horizontal, and simultaneous overflow.
- Fixed-point cases where adding one bar causes the other axis to overflow.
- Gutter plus both bars and a noninteractive corner.
- Display-column scrolling across tabs, combining sequences, wide glyphs, and
  surrogate pairs.
- Incremental extent discovery, invalidation after edits, exactness changes,
  and bounded work on large documents.
- Caret/selection preservation and clamping for every scroll input path.
- Pointer capture and popup cancellation across focus loss, modal opening,
  resize, disable, hide, detach, and disposal.

### Qualification

- All tests pass for net8.0, net9.0, and net10.0.
- API fingerprint and binary compatibility checks pass.
- Editor, gallery, workspace, and roguelike sample smokes pass.
- Exact package and symbol-package validation pass.
- Package-only consumer exercises both orientations and the new editor option.
- Windows, Linux, and macOS CI pass before merge.

## Documentation and Samples

- Add one compact README example showing both editor options and link to the
  editor sample. Do not add version history to the README.
- Expand the editor sample with a no-wrap long-line document, both auto-hidden
  bars, Shift+click, drag, hold-repeat, and context-menu verification notes.
- Add the full change list to `CHANGELOG.md`.
- Add curated `docs/releases/1.10.0.md` notes with compatibility, verification,
  package identity, and Dedit integration instructions.
- Update package release notes without duplicating the curated document.

## Release and Dedit Sequence

1. Implement and qualify Icod.Widgets 1.10.0 in a new pull request; do not
   merge automatically.
2. After owner review, merge, pack, publish, and confirm NuGet availability.
3. Update Icod.Dedit in its existing pull request to reference 1.10.0 and enable
   both `ShowVerticalScrollBar` and `ShowHorizontalScrollBar` for document
   editors.
4. Verify Dedit in wrap and no-wrap modes, with fitting, vertically overflowing,
   horizontally overflowing, and two-axis-overflowing documents.

## Approved Decisions

- Include editor-integrated horizontal scrolling in this release.
- Use `◄` and `►` for horizontal buttons.
- Include arrow and track hold-repeat.
- Include right-click and Shift+F10 context menus where focus ownership permits.
- Use a shared geometry/interaction engine rather than child scrollbar controls
  or duplicated editor logic.
- Clamp an editor caret that scrolling would push out of view on either axis.
- Keep Terminal/DCurses unchanged; defer the dedicated Menu/Apps key.
- Target Icod.Widgets 1.10.0 and update Dedit only after publication.
