# Icod.Widgets Main Roadmap

**Decision date:** 2026-10-19\
**Baseline:** 1.8.0 generic dialog actions released\
**Next target:** 1.9.0 editor viewport scrollbar over Icod.DCurses 2.5.2\
**State:** 1.9.0 implementation and qualification in PR #19; owner merge and publication pending

## Purpose

Build reusable terminal controls above Icod.DCurses for applications and games.
The next consumer is [dedit](https://github.com/uniblab/Icod.Dedit/pull/1): an
Edit-inspired GPL text editor with multiple documents in a tabbed interface and
no print support. Widgets supplies reusable behavior, not dedit's file policy.

The 1.1 editing foundation through 1.8 generic dialogs are released. The
selected 1.9 direction is an opt-in, auto-hidden editor scrollbar whose
viewport commands count wrapped display rows. This completes the reusable
scrolling mechanism needed by dedit without moving file policy or application
commands into Widgets.

## Ownership and permanent constraints

| Owner | Responsibility |
| --- | --- |
| Icod.Widgets | Document text/edit history, view state, editing controls, focus/commands, scrolling, reusable workspace controls |
| Icod.DCurses | Unicode/cell geometry, retained projection, clipping/damage, interaction mechanisms, clipboard and cursor facades |
| Icod.Terminal | Terminal protocols, input decoding, clipboard transport/budgets, reversible terminal state |
| Application / dedit | File identity, encoding/BOM/EOL policy, metadata-aware dirty state, safe save, external changes, document lifetime/close, search execution, loop and timing |

One direct runtime dependency remains the published `Icod.DCurses 2.5.2`.
Do not add direct Terminal/TermInfo
references, protocol escapes, another input reader, hidden timers, or a
filesystem-writing service to Widgets.

Preserve the stable 1.0 API and assembly version `1.0.0.0`. Continue managed
C# 13, `net8.0`/`net9.0`/`net10.0`, AnyCPU, Windows/macOS/Linux build targets,
and CMD/shell/PowerShell 5.1 tooling. The library remains LGPL-3.0-or-later;
samples and tests remain GPL-3.0-or-later with their prescribed full headers.

## Release sequence

| Release | Deliverable | Explicit boundary |
| --- | --- | --- |
| 1.0.0 — released | Forms, lists/drop-downs, menus, captioned dialogs, existing-file/folder pickers, game UI, public samples | Existing text input is single-line; editor sample is an open/browse witness |
| 1.1.0 — released | Reusable document/editing foundation; Unicode editing, bounded undo/redo and view projection; input/focus/capture repairs; scrollbars; explicit clipboard and caret integration; better single-line fields | Single-document editing is useful on its own; no tabbed application or file-save engine |
| 1.2.0 — merged | Cohesive Document Workspace: tabs, composable forms/dialogs, save-target picker, dynamic command/menu state, find/replace/navigation forms, actionable status fields, and public composition seams | App owns documents, file writes, search/regex, close approval, and metadata |
| 1.3.0 — prepared in PR #6 | Capability-aware color theme selection, part/state styles, complete surface painting, and a themed gallery above DCurses 2.5.0 | Existing monochrome theme and public APIs remain compatible; applications still control presentation choices and repaint timing |
| 1.4.0–1.8.0 — released | Editor insertion and linewise clipboard policies, menu interaction/themes, current-line/gutter presentation, wrapped caret navigation, and generic dialog actions | Application file policy and presentation choices remain outside Widgets |
| 1.9.0 — in PR #19 | Opt-in auto-hidden editor scrollbar, displayed-row viewport commands, pointer controls, and caret edge clamping | Public scroll offsets remain logical-line compatible; wrapped-row bookkeeping is internal |
| Later — unscheduled | Data tables/trees, virtualized data sources, richer accessibility, advanced forms | Separate requirements, design, and approval; not blockers for dedit's first release |

## 1.1.0 — editing foundation

Separate three cooperating components:

1. **Document:** exact text, indexed lines, immutable snapshots/revisions,
   grouped bounded history, and saved-checkpoint identity.
2. **View state:** caret, selection anchor, preferred vertical column, wrap and
   horizontal/vertical scroll independent of document storage.
3. **Editor control:** input and editing policy, visible-slice DCurses projection,
   interactive scrollbars, explicit clipboard requests, and focused caret intent.

Measure candidate text stores before freezing one. Test real many-line and
long-line documents, random edits, snapshots, history memory, and bounded layout.
The DCurses synthetic ten-million-record demonstration is mechanism evidence,
not a real-file performance claim.

Required behavior includes grapheme-safe navigation/deletion, word movement,
selection replacement, indentation, insert/overwrite, read-only operation,
undo/redo, preserved tabs/mixed line endings/final newline, captured drag and
caller-clock autoscroll. Distinguish UTF-16 offsets from grapheme positions,
visual cells, and encoded bytes. Rendering must not normalize source text.

First repair lost rich-input metadata and host routing: release events cannot
duplicate edits, complete bracketed paste is one transaction, editor Tab differs
from form traversal, and focus/capture cancellation is deterministic. Provide
the minimal public extension and contextual-edit-command hooks needed by this
release; the full dynamic menu/workspace model remains 1.2 work.

Clipboard uses DCurses 2.4 explicitly, with caller-owned fallback and completion
on the application loop. Its 65,536-byte default read/write limits are independent
and configurable up to 16 MiB, not remote capacity negotiation. Late paste cannot
land in a different view, selection, revision, or focus lifetime. Cut must not
delete text on a failed copy. Caret styling remains best-effort and reversible.

The [1.1 development roadmap](Icod.Widgets-1.1.0-Development-Roadmap.md),
[design](docs/superpowers/specs/2026-10-11-icod-widgets-1.1-design.md), and
[implementation plan](docs/superpowers/plans/2026-10-11-icod-widgets-1.1.md)
define the task order and acceptance gates.

## 1.2.0 — document workspace

The selected scope is a complete reusable workspace layer rather than a narrow
tab-strip release or dedit-specific façade. The library exposes presentation
state and semantic requests; the application remains the authority for policy,
I/O, asynchronous work, and document lifetime.

- **Tabs and active content:** stable keys, captions, modified indicators,
  overflow, keyboard/mouse switching, and cancellable close requests. A tab
  never saves or disposes the application's document automatically. Each view
  retains its own caret/scroll, and switching invalidates pending interaction
  that must not cross into another view.
- **Composable dialogs:** child controls, initial focus, default/cancel actions,
  validation, public completion, and Save / Don't Save / Cancel. Preserve framed
  titles and Escape = Cancel, including OK-only dialogs.
- **Save target:** a distinct picker for new or existing target paths. Preserve
  the existing-file picker and third-party provider contracts. Validation is an
  observation, not an atomic promise against overwrite races; the app writes.
- **Commands and menus:** query enabled/checked state from active document and
  focused control; support menu pointer interaction and contextual routing.
  Keep menu navigation mnemonics distinct from command shortcuts.
- **Search and navigation forms:** reusable find/replace/go-to-line controls and
  typed requests/results. dedit executes searches/regex, cancels stale work, and
  coordinates replacement transactions against document snapshots.
- **Status and extension:** actionable position/encoding/EOL/indentation fields
  and public custom-control hooks, without exposing host internals.

The [versioned 1.2 design](docs/superpowers/specs/2026-10-12-icod-widgets-1.2-document-workspace-design.md)
and [1.2 development roadmap](Icod.Widgets-1.2.0-Development-Roadmap.md)
record the delivered scope and its W1201–W1211 qualification. The 1.2 package
remains on its historical DCurses 2.4.0 dependency; the 1.3 track updates it.

## 1.3.0 — widget color themes

`WidgetTheme` retains six complete `CursesStyle` values. The 1.3 source adds
scoped part styles and capability-aware palettes. Tabs distinguish active
state even when the editor has focus. Menus, scrollbars, status fields,
dialogs, pickers, and clipped popup regions paint their owned cells.

- Preserve the six-role positional `WidgetTheme` constructor/deconstruction and
  `WidgetTheme.Default` appearance. Add opt-in styles for control surfaces and
  parts, with explicit state precedence and predictable fallback to existing
  roles. Allow a scoped per-widget override without making a global theme a
  mutable shared object.
- Use `CursesPresentationCapabilities` to choose a conservative terminal-default,
  indexed, or direct-RGB palette. DCurses retains responsibility for per-axis
  normalization; Widgets must not approximate colors or assume background
  support follows foreground support. Preserve readable non-color distinctions.
- Paint every owned cell of an opaque control or overlay with the selected
  surface style. Preserve borrowed background and transparent-panel semantics;
  a title, border, selection, wide grapheme, empty row, and padding must not
  accidentally reveal a different color.
- Exercise all widget families, especially tabs, editor/fields, menus and
  drop-downs, lists/pickers, dialogs, buttons, scrollbars, and status fields.
  Keep the gallery's existing smoke path and add a public-only colored theme
  demonstration with a terminal-default fallback.

The [1.3 development roadmap](Icod.Widgets-1.3.0-Development-Roadmap.md)
orders W1301–W1310, defines compatibility and package gates, and names the
acceptance scenarios. The local stable artifact and qualification record are
in PR #6; merge, tag, publication and the final remote CI gate remain.

## Deliberately outside this programme

Printing, syntax highlighting, split views, multiple carets, tab reordering,
session recovery, cloud services, and plugin hosting are not first-delivery
requirements. Widgets never owns game rules, a dungeon engine, terminal
emulation, document file identity, codecs, or safe-save/external-change policy.

## Delivery and evidence

Keep 1.0 qualification and release history in [W1014](docs/W1014-Stable-Qualification.md),
the [1.0 roadmap](Icod.Widgets-1.0.0-Development-Roadmap.md), and
[CHANGELOG](CHANGELOG.md). Do not turn historical untested platforms into passes.

Each 1.3 tranche records exact source/package versions, changes, tests,
limitations, and acceptance evidence. Freeze API and package metadata before
stable qualification, then verify the exact merged artifact. No calendar date
or green documentation PR substitutes for implementation or release readiness.

The 1.1 editing foundation and 1.2 Document Workspace are merged. The 1.3 color
theme track is implemented and locally qualified; remote CI and owner handoff
remain. The owner performs merge, tag, and publication. No change to
`uniblab/.github` is required.
