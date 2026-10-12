# Icod.Widgets 1.2 Document Workspace Design

**Date:** 2026-10-12\
**Status:** approved architectural direction; public API and task plan pending review\
**Release target:** 1.2.0\
**Source baseline:** `d71b74d0a68792c2001b609978b434bffb14f030`\
**Primary consumer:** [Icod.Dedit](https://github.com/uniblab/Icod.Dedit/pull/1)

## Intent

Icod.Widgets 1.1 supplies the bounded, Unicode-aware single-document editing
foundation. Version 1.2 will add the reusable workspace controls and interaction
contracts needed to assemble a multi-document editor without moving application
policy into the widget library.

The release succeeds when an application can present several caller-owned
documents, switch among their independent views, reflect active-document command
state, collect file targets and search/navigation input, obtain explicit close
decisions, and expose actionable status fields through one caller-driven event
loop. The same controls must remain useful outside dedit.

## Decision

Ship one cohesive **Document Workspace** release. This was chosen over:

1. a tab-strip-only release, which would leave every consumer to invent
   incompatible command, close, dialog, and active-content coordination; and
2. a dedit-specific workspace façade, which would encode file and editor policy
   that belongs to the GPL application rather than the LGPL library.

The workspace is a composition layer, not an application framework. Callers
supply immutable or replaceable presentation state and receive semantic actions.
Widgets does not execute file operations, search jobs, save policy, or document
lifetime decisions.

## Existing foundation and gaps

The design extends, rather than replaces, these 1.1 contracts:

- `WidgetHost` owns attachment, focus, modal routing, capture, action buffering,
  rendering, and caller-clock updates.
- `WidgetTextDocument`, `WidgetTextViewState`, and `WidgetTextEditor` separate
  text, view state, and editing control.
- `WidgetAction` carries semantic work back to the application loop.
- `WidgetDialog` provides framed, captioned modal completion; existing message,
  wizard, folder, and existing-file dialogs remain supported.
- `WidgetMenuItem` currently fixes enabled state at construction and exposes only
  internal checked-state mutation.
- `WidgetStatusBar` currently renders inert prioritized text fields.
- `IWidgetFileSystemProvider` validates existing files or directories and must
  remain source-compatible for third-party implementations.

Version 1.2 fills the workspace gaps without weakening those contracts.

## Ownership boundary

| Concern | Owner |
| --- | --- |
| Tab presentation, overflow, focus/navigation, switch and close requests | Icod.Widgets |
| Per-document text and view state | Caller, represented by Icod.Widgets document/view types |
| Document identity, path identity, title policy, encoding, BOM, and EOL metadata | Application |
| Dirty policy beyond document checkpoint state | Application |
| Save, overwrite, external-change, reload, and close approval policy | Application |
| File and directory enumeration/validation mechanism | Existing picker provider contract plus additive save-target capability |
| Search/regex execution, cancellation, result ordering, and replacement transactions | Application |
| Command presentation and routing | Icod.Widgets |
| Command meaning and execution | Application or focused widget, according to explicit routing |
| Event loop, input reader, monotonic time, background jobs, and completion dispatch | Application |

Widgets starts no loop, worker, timer, input reader, or filesystem write. A
successful validation is an observation, not a reservation or atomic promise.

## State and action model

Workspace controls follow four rules:

1. **Stable identity.** Tabs, commands, status fields, and asynchronous requests
   use caller-supplied stable keys. Captions and indexes are presentation, not
   identity.
2. **Replaceable state.** The application may replace tab, command, and status
   snapshots on its loop. Widgets validates duplicate keys and keeps selection
   only where the stable key still exists.
3. **Semantic output.** User intent produces bounded `WidgetAction` values such
   as activate tab, request close, invoke command, choose target, or submit a
   search form. Emitting an action never implies that policy work succeeded.
4. **Generation safety.** Operations that may finish later carry owner identity
   and a monotonically increasing generation. Closed, replaced, canceled, or
   otherwise stale owners reject late completion.

This model avoids callbacks during input dispatch and keeps all externally
observable mutation on the caller's application loop.

## Tabs and active content

The tab strip accepts an ordered snapshot containing at least a stable key,
caption, modified indicator, enabled/closable state, and optional application
metadata carried only in emitted actions. It provides:

- keyboard traversal, direct activation, and cycling independent of caption;
- primary-pointer activation and an explicit close affordance;
- deterministic overflow that always keeps the active tab visible and exposes
  hidden tabs without silently changing application order;
- activation and close-request actions rather than direct document mutation;
- rejection of duplicate keys and a defined empty state; and
- clipping that preserves cancellation and keyboard reachability in compact
  viewports.

Close is always a request. Widgets does not infer whether a tab is dirty, show a
save prompt automatically, save, remove, or dispose a document. The application
may ignore or deny a request and leave the tab active.

Active editor content remains application-owned. The implementation plan will
choose the smallest additive public composition seam that allows either a stable
content host to replace its child or an editor to rebind safely to another
`WidgetTextViewState`. Whichever API is selected must guarantee the same
behavior:

- each document view retains its caret, selection, preferred column, scroll,
  wrap, and pending-operation epoch;
- focus remains on the logical editor when the replacement is eligible;
- pointer capture, bracketed paste assembly, clipboard requests, and caller-clock
  autoscroll belonging to the old view are canceled or made stale before the
  new view becomes interactive; and
- no late completion can edit the newly active document by accident.

The implementation plan must resolve the public signature after tests compare
both composition approaches. It must not make `WidgetHost` internals public.

## Commands and menus

Menus keep their current distinction between structural navigation and command
invocation: `Alt+V` opens View, plain `F` may navigate within that open menu, and
`Ctrl+F` may invoke a different command directly.

Version 1.2 adds caller-controlled dynamic presentation for stable command and
menu-item keys:

- enabled/disabled and checked/unchecked state;
- visible text and optional shortcut display where an application must update
  context-sensitive wording;
- refresh while a menu is open without losing the selected stable item;
- pointer hover, activation, submenu traversal, and capture cancellation; and
- explicit routing to the focused widget first or to application commands,
  without invoking both.

State updates are snapshots applied on the application loop, not arbitrary
delegates invoked during render. If the selected item disappears or becomes
disabled, selection moves deterministically to the next eligible item, then the
previous eligible item, or closes that empty menu level. Duplicate item keys and
invalid submenu graphs fail before interaction.

## Composable dialogs

A general dialog surface composes caller-supplied child controls with an ordered
set of action descriptors. It provides:

- a framed title/caption and a clipped content region;
- initial focus resolved by stable child ID;
- one default action for Enter and one cancel action for Escape;
- enabled state and deterministic focus traversal for action buttons;
- validation requests and generation-safe completion where validation is
  asynchronous; and
- one public completion action with a typed or caller-supplied result value.

Escape always completes with `Cancel`, including an OK-only dialog. Default
activation never bypasses disabled state or pending validation. Compact layouts
must retain a keyboard-reachable cancel path even when not every button fits.

Save / Don't Save / Cancel is supplied as a reusable decision-dialog
configuration. Its result expresses the choice only; it never saves or closes a
document. Existing message and wizard dialog behavior remains compatible.

## Save-target picker

Save-target selection is a distinct dialog, not a mode that weakens the existing
file picker's guarantee that selected files already exist. It composes directory
browsing, a single-line target-name field, optional filter/extension guidance,
and validation state.

The picker returns a proposed target path plus validation observations needed by
the application to decide whether to proceed. It does not create a file, truncate
an existing file, reserve a name, or promise that an overwrite race cannot occur.
The application must perform the final safe-write and conflict checks.

`IWidgetFileSystemProvider` remains source-compatible. Save-specific inspection
is introduced through an additive optional capability or separate provider
interface, selected in the implementation plan after compatibility tests.
Unsupported capability is reported explicitly rather than guessed from path
syntax. Requests and results use picker identity and generation so navigation or
name changes invalidate stale validation.

## Search and navigation forms

Reusable Find, Replace, and Go To forms own only input and interaction state.
They provide typed semantic submissions for:

- query text, direction, case, whole-word, and regular-expression options;
- replace-one and replace-all intent with replacement text; and
- a validated one-based line target suitable for a Go To Line dialog.

The application executes searches and replacements against an explicit document
snapshot/revision, schedules or cancels any background work, chooses wrap and
not-found policy, and applies replacement transactions. Returned results carry
the originating document/view identity, revision, request ID, and generation;
stale results cannot move selection or edit text.

Forms support normal editing commands in their fields and preserve menu
shortcut behavior. Find-next commands can be invoked outside the form using
application-owned last-query state; Widgets does not create global search state.

## Actionable status fields

The status bar evolves additively from prioritized text to stable fields that
may optionally name a command. Position, encoding, EOL, indentation, overwrite,
and similar values remain application-supplied display state.

Actionable fields provide keyboard and primary-pointer invocation, visible focus,
disabled state, and semantic command actions. Overflow preserves priority and
must not leave an invisible focused field. Inert `WidgetStatusField` use remains
source-compatible and behaves as it does in 1.1.

## Public composition seams

Custom controls need supported mechanisms for child composition, focus
participation, layout, rendering, input, capture cancellation, semantic action
emission, and bounded invalidation. Version 1.2 exposes only the minimum seams
required by the workspace controls and third-party equivalents.

The design does not expose mutable host collections, raw modal stacks, action
buffers, or ownership setters. Attachment remains single-host and acyclic.
Subclass contracts must document which methods are application-loop-only and
which base behavior must be preserved.

## Failure and lifecycle behavior

- Invalid arguments, duplicate stable keys, ownership cycles, and configured
  capacity violations fail synchronously before partial attachment or mutation.
- Expected user outcomes such as Cancel, a denied close, no search match, or a
  validation failure are data, not exceptions.
- Provider and application exceptions remain application-owned; Widgets can
  display supplied error state but does not swallow or retry external failures.
- Removing, disabling, or hiding active content cancels capture, clears invalid
  focus, and invalidates pending generations deterministically.
- Dialog completion, tab close requests, and command invocation are emitted at
  most once per accepted input gesture.

## Capacity and performance

Existing host limits remain in force. New controls add explicit limits for tab
count, menu/state snapshot size, dialog children/actions, and form input. Defaults
must comfortably cover an editor workspace while preventing unbounded action or
layout work.

Rendering and input work are proportional to visible controls plus bounded
overflow bookkeeping, not total document text. Switching tabs must not copy the
document or rebuild every document projection. Benchmarks and allocation probes
will cover hundreds of tabs, repeated active-view switching, large dynamic menu
refreshes, and compact viewports before public API freeze.

## Compatibility and packaging

- Package target: `1.2.0`; assembly identity remains `1.0.0.0`.
- Public 1.0 and 1.1 APIs remain the compatibility floor; changes are additive.
- Direct runtime dependency remains `Icod.DCurses 2.4.0`; Terminal 1.29.0 stays
  transitive. No direct Terminal or TermInfo reference is added.
- Targets remain `net8.0`, `net9.0`, and `net10.0`, C# 13, AnyCPU, with the
  existing CMD, shell, and PowerShell 5.1 build entry points.
- Library code remains LGPL-3.0-or-later. Samples and tests remain
  GPL-3.0-or-later with the repository's prescribed full headers.

## Documentation and sample

Add a dedicated document-workspace sample rather than turning the focused 1.1
editor sample into an application. The new sample will demonstrate:

- several in-memory documents and independent views;
- tab switching, modified markers, overflow, and denied/accepted close requests;
- live menu command state and mnemonic-versus-shortcut behavior;
- Find, Replace, Go To, Save / Don't Save / Cancel, and save-target interaction;
- actionable status fields; and
- a deterministic `--smoke` path with no live terminal and no filesystem writes.

The root README will gain a concise 1.2 inventory and runnable command only when
the features exist. Detailed ownership, failure, key, and provider guidance goes
in versioned or focused documentation rather than release-status prose.

## Acceptance scenarios

The implementation roadmap must turn at least these scenarios into automated
or deterministic interactive evidence:

1. Switch repeatedly among three views and preserve every caret, selection,
   scroll offset, and undo history.
2. Complete an old clipboard, picker, search, or validation request after a tab
   switch/removal and prove that it cannot affect the active document.
3. Request close on a modified tab, choose each Save / Don't Save / Cancel result,
   and prove that only the application decides whether the tab is removed.
4. Change active document/read-only/selection state while a menu is open and
   observe correct enabled and checked state without losing valid selection.
5. Use `Alt+V`, then an unmodified mnemonic, while `Ctrl+F` remains an independent
   direct shortcut.
6. Navigate and close overflowing tabs with keyboard and pointer in a compact
   viewport.
7. Propose a new save target and an existing target, handle stale validation,
   and prove that the picker itself writes nothing.
8. Submit Find, Replace, Replace All, and Go To requests; reject results for an
   obsolete revision or view generation.
9. Invoke status commands by keyboard and pointer and preserve focus when lower
   priority fields overflow.
10. Run all samples in deterministic smoke mode and build/test every target in
    CI on Windows, macOS, and Linux, while reporting live-terminal qualification
    separately from automated CI.

## Exclusions for 1.2

Printing, tab reordering, split views, session recovery, syntax highlighting,
multiple carets, cloud services, plugin hosting, file watching, codecs, atomic
save implementation, and a regex engine are not part of this release. They
require separate requirements and approval.

## Planning gate

After this design is reviewed, write a versioned 1.2 development roadmap and an
executable implementation plan. Those documents will freeze proposed public
signatures, decompose W12xx tranches, identify test-first increments, and define
release qualification evidence. Runtime implementation does not begin before
that review resolves the active-content and save-provider API choices.
