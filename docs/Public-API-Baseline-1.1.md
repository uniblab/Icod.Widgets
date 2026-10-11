# Icod.Widgets 1.1 Public API Baseline

**Release:** `1.1.0`
**Assembly version:** `1.0.0.0`
**Targets:** `net8.0`, `net9.0`, `net10.0`
**Direct runtime dependency:** `Icod.DCurses 2.4.0`

The 1.1 surface is an additive extension of the stable 1.0 baseline. A
reflection comparison found all 72 previously exported types and all recorded
1.0 declared public members unchanged. The candidate exports 103 types. Enum
values, existing positional-record constructors/deconstruction, factories, and
assembly identity remain compatibility constraints for the 1.x line.

The new editing family consists of:

- `WidgetTextDocument`, document options, lines, edits, edit contexts/results,
  change events, immutable snapshots, revision/state identity, and checkpoints;
- `WidgetTextViewState`, `WidgetEditorOptions`, and `WidgetTextEditor`;
- edit commands/targets plus host-scoped command execution and caller-driven updates;
- scrollbar orientation/range/control and semantic scroll actions;
- guarded clipboard requests/results/status and host completion;
- focused `WidgetCaretIntent`; and
- rich function-key, text, phase, focus, paste, and source-modifier input metadata.

Existing `WidgetTextInput` gains selection, read-only, clipboard, undo/redo, and
caret behavior without changing its 1.0 constructor, value normalization,
length limit, or Tab traversal default. `Widget.AcceptsTab` is an additive
virtual property used by multiline controls. `WidgetHost` gains `MoveFocus`,
`ExecuteEditCommand`, `Update`, `ApplyClipboardResult`, and `GetCaretIntent`.

The machine-readable declared-type/member snapshot is
[`Public-API-Fingerprint-1.1.json`](Public-API-Fingerprint-1.1.json). The historical
[`1.0 baseline`](Public-API-Baseline-1.0.md) and fingerprint remain in the
repository and continue to define the compatibility floor.
