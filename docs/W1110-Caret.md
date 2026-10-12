# W1110 — Focused Caret Intent

## Outcome

`WidgetHost.GetCaretIntent()` now exposes the focused control's terminal-independent
caret presentation without performing terminal I/O. `WidgetCaretIntent` carries
visibility, absolute screen row/column, an optional semantic `CursesCursorStyle`,
and the current host focus generation. `null` means that no eligible caret should
be presented.

The protected `Widget.GetCaretIntent()` extension seam defaults to `null` so custom
controls opt in explicitly. The host rejects intents from hidden, disabled,
offscreen, unfocused, popup-covered, or detached controls and stamps the accepted
intent with its focus generation.

## Editor geometry and styles

`WidgetTextEditor` obtains its caret cell from the same bounded W1106 projection
used for rendering, selection rectangles, tabs, wide characters, combining/ZWJ
text, wrapping, controls, and horizontal source origins. It converts the viewport
cell to an absolute screen coordinate and returns no intent when the caret is
outside the clipped editor rectangle.

Insert mode requests `CursesCursorStyle.SteadyBar`; overwrite mode requests
`CursesCursorStyle.SteadyBlock`. Visibility and style remain independent. The host
does not acquire a style lease, move a physical cursor, start async work, or retain
a `CursesSession`.

## Sample coordination

`samples/Common/WidgetSampleCaretCoordinator.cs` is the application-owned bridge.
It positions and shows/hides the cursor through the released DCurses 2.4 API and
optionally owns one `CursesCursorStyleLease`. Acquisitions are serialized. Each call
increments a generation before waiting, so an acquisition that completes after a
newer intent is immediately disposed and never becomes current.

Style changes release the current lease before acquiring the replacement, preserving
DCurses' LIFO restoration contract. Unsupported or timed-out baseline observation
returns no lease and emits no guessed reset. Cleanup hides the cursor and disposes
the current lease once; lease disposal restores the exact observed preceding style.
The shared sample loop applies intent after retained refresh and asynchronously
disposes the coordinator before session shutdown.

## Verification

- `WidgetCaretTests`: 6 passing scenarios covering Unicode/tab geometry, hidden/
  offscreen/modal absence, insert-versus-overwrite styles, stale acquisition
  disposal, unsupported observation without guessed reset, and exactly-once cleanup.
- The coordinator is compiled both by the sample projects and the test assembly;
  fake-session tests exercise async ordering without opening a terminal.
- The complete net10.0 suite and clean net8/net9/net10 build remain the tranche gate;
  net8.0/net9.0 execution remains delegated to CI in this environment.
