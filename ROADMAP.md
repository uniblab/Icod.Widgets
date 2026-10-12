# Icod.Widgets Main Roadmap

**Decision date:** 2026-10-12\
**Baseline:** 1.1.0 stable source merged at `d71b74d0a68792c2001b609978b434bffb14f030`\
**Next target:** 1.2.0 document workspace\
**State:** scope and architecture approved; detailed implementation planning follows design review

## Purpose

Build reusable terminal controls above Icod.DCurses for applications and games.
The next consumer is [dedit](https://github.com/uniblab/Icod.Dedit/pull/1): an
Edit-inspired GPL text editor with multiple documents in a tabbed interface and
no print support. Widgets supplies reusable behavior, not dedit's file policy.

The 1.1 editing foundation is complete. The selected 1.2 direction is one
cohesive **Document Workspace** release: tabs, dynamic commands, composable
dialogs, save-target selection, search/navigation forms, actionable status
fields, and the public extension seams required to compose them. Tabs must not
force document lifetime or save policy into the library.

## Ownership and permanent constraints

| Owner | Responsibility |
| --- | --- |
| Icod.Widgets | Document text/edit history, view state, editing controls, focus/commands, scrolling, reusable workspace controls |
| Icod.DCurses | Unicode/cell geometry, retained projection, clipping/damage, interaction mechanisms, clipboard and cursor facades |
| Icod.Terminal | Terminal protocols, input decoding, clipboard transport/budgets, reversible terminal state |
| Application / dedit | File identity, encoding/BOM/EOL policy, metadata-aware dirty state, safe save, external changes, document lifetime/close, search execution, loop and timing |

One direct runtime dependency remains `Icod.DCurses`. Adopt **2.4.0** in the
first implementation tranche, consuming Terminal 1.29.0 transitively. Do not
add direct Terminal/TermInfo references, protocol escapes, another input reader,
hidden timers, or a filesystem-writing service to Widgets.

Preserve the stable 1.0 API and assembly version `1.0.0.0`. Continue managed
C# 13, `net8.0`/`net9.0`/`net10.0`, AnyCPU, Windows/macOS/Linux build targets,
and CMD/shell/PowerShell 5.1 tooling. The library remains LGPL-3.0-or-later;
samples and tests remain GPL-3.0-or-later with their prescribed full headers.

## Release sequence

| Release | Deliverable | Explicit boundary |
| --- | --- | --- |
| 1.0.0 — released | Forms, lists/drop-downs, menus, captioned dialogs, existing-file/folder pickers, game UI, public samples | Existing text input is single-line; editor sample is an open/browse witness |
| 1.1.0 — merged stable source | Reusable document/editing foundation; Unicode editing, bounded undo/redo and view projection; input/focus/capture repairs; scrollbars; explicit clipboard and caret integration; better single-line fields | Single-document editing is useful on its own; no tabbed application or file-save engine |
| 1.2.0 — approved design | Cohesive Document Workspace: tabs, composable forms/dialogs, save-target picker, dynamic command/menu state, find/replace/navigation forms, actionable status fields, and public composition seams | App owns documents, file writes, search/regex, close approval, and metadata |
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
defines the approved behavior, ownership boundaries, failure model, and release
acceptance scenarios. Freeze public signatures only in the implementation plan
after that design is reviewed against the merged 1.1 contracts. These are
direction commitments, not shipped features.

## Deliberately outside this programme

Printing, syntax highlighting, split views, multiple carets, tab reordering,
session recovery, cloud services, and plugin hosting are not first-delivery
requirements. Widgets never owns game rules, a dungeon engine, terminal
emulation, document file identity, codecs, or safe-save/external-change policy.

## Delivery and evidence

Keep 1.0 qualification and release history in [W1014](docs/W1014-Stable-Qualification.md),
the [1.0 roadmap](Icod.Widgets-1.0.0-Development-Roadmap.md), and
[CHANGELOG](CHANGELOG.md). Do not turn historical untested platforms into passes.

Each W12xx tranche will record exact source/package versions, changes, tests,
limitations, and acceptance evidence. Freeze API and package metadata before
stable qualification, then verify the exact merged artifact. No calendar date
or green documentation PR substitutes for implementation or release readiness.

The 1.1 editing foundation is implemented and merged. The 1.2 Document Workspace
design is approved for detailed planning and implementation, but no 1.2 runtime
feature is shipped by this planning state. The owner performs merge, tag, and
publication. No change to `uniblab/.github` is required.
