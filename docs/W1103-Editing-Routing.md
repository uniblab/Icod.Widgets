# W1103 — Editing routing and external control hooks

Widgets 1.1 provides a host-owned routing seam for editor controls without
moving the application loop into the library.

## Host behavior

- `Update(timestamp)` advances caller-supplied monotonic time and invokes each
  attached, enabled, visible widget's update hook.
- `MoveFocus(direction)` makes traversal explicit and remains scoped by the
  active DCurses overlay.
- `ExecuteEditCommand(command, timestamp)` offers Copy, Cut, Paste, Select All,
  Undo, and Redo only to the focused `IWidgetEditTarget`. A recognized but
  unavailable command returns `Disabled`; it is not allowed to fall through to
  an unrelated background command.
- A focused widget whose `AcceptsTab` is true receives Tab. Other widgets retain
  the 1.0 focus-traversal behavior.
- Key-release reports never activate controls or menu commands. Repeats remain
  routable input.

Pointer capture now routes moves and the matching release to the press owner.
Capture cancellation is delivered exactly once when ownership becomes invalid,
including focus loss, layout change, hiding, disabling, detachment, overlay
changes, or host disposal.

## External widget contract

Derived widgets can reserve action capacity before mutating state, emit actions
whose `WidgetId` matches their own ID, and receive protected update and capture-
cancellation callbacks. The host validates attachment, identity, and queue
capacity. The original internal capture callback remains the compatibility
bridge used by existing 1.0 controls.

The package-only consumer compiles and runs a derived widget through these
protected APIs, so the contract does not rely on test friend access.
