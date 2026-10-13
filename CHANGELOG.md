# Changelog

All notable changes to Icod.Widgets are recorded here.

## Unreleased — 1.4.0 editor insertion policies

- Added mutable `WidgetTextEditor.NewLine` (LF, CRLF, or CR) and
  `WidgetTextEditor.Indentation` (spaces and tabs) for future edits without
  replacing an editor or rewriting existing document text.
- Existing constructor options remain the initial policies. Invalid changes
  are rejected without changing the current policy; subsequent edits remain
  undoable with the existing document history.
- The in-memory editor sample lets operators cycle future newline and
  indentation policies with F7/F8; its smoke checks the resulting edits and undo.
- Retained net8.0/net9.0/net10.0, assembly version `1.0.0.0`, and the sole
  direct dependency on Icod.DCurses 2.5.0. This entry is a candidate, not a
  published release.

## 1.3.1 — editor projection correction

- Invalidate a cached line when text is inserted at its end. This restores the
  first typed character after rendering a blank document and subsequent
  appends, without changing the public API or the 1.0.0.0 assembly identity.
- Retain net8.0/net9.0/net10.0 and Icod.DCurses 2.5.0 as the sole direct
  dependency.

## 1.3.0 — widget color themes

- Added immutable part/state styles with independent foreground, background,
  and attributes, nearest-widget scoped overrides, and live host theme replacement.
- Styled the owned surfaces and states of controls, editors, tabs, status,
  scrollbars, menus, popups, dialogs, pickers, and game UI. Active tabs remain
  visible when focus is in another control.
- Added dark/light palettes based on DCurses' public color/attribute
  capabilities, with independent fallback of unavailable color axes.
- Expanded the gallery to seven pages with a workspace color example and a
  Ctrl+T theme cycle. Its smoke renders each page under all three themes.
- Updated the sole direct dependency to Icod.DCurses 2.5.0. Retained the
  `1.0.0.0` assembly identity and the net8.0/net9.0/net10.0 targets.

## 1.2.0 — document workspace

- Shipped stable-key document tabs and caller-owned active content replacement.
- Added controlled menu presentation, focused edit-command routing, actionable
  status fields, and pointer activation.
- Added composable form dialogs with Save/Don't Save/Cancel choices, a separate
  non-writing save-target inspector, and typed search/navigation requests.
- Added an in-memory multi-document workspace sample, public package consumer,
  API compatibility snapshot, and bounded workload measurements.
- Hardened the sample's empty-workspace commands, added F6 focus movement and
  F3 Find Next, and made Find-only forms advertise only available actions.
- Preserved the `1.0.0.0` assembly identity, net8.0/net9.0/net10.0 targets,
  and sole direct `Icod.DCurses 2.4.0` dependency.

## 1.2.0-rc.1 — document workspace candidate

- Added stable-key tabs, deterministic overflow, caller-owned active content
  replacement, and stale pointer/clipboard/paste cancellation.
- Added caller-controlled menu presentation, focused edit-command routing,
  pointer activation, and optional actionable status fields.
- Added child-composable dialog controls, explicit Save/Don't Save/Cancel
  outcomes, validation requests, and a separate non-writing save-target picker.
- Added typed Find/Replace/Go To Line requests with document/view/revision and
  generation guards. The application owns search execution and file writes.
- Added a three-document in-memory workspace sample, public package consumer,
  API compatibility snapshot, and bounded switching measurements.
- Retained assembly version 1.0.0.0, net8.0/net9.0/net10.0 targets, and the
  sole direct dependency on Icod.DCurses 2.4.0.

## 1.1.0 — reusable editing foundation

- Shipped the exact document/snapshot/history model, Unicode multiline editor,
  scrollbars, guarded clipboard integration, caret coordination, and upgraded
  single-line editing described by the 1.1 release candidate.
- Added the public editing guide, frozen additive API baseline, measured workload
  evidence, package-only consumer, and editable in-memory sample.
- Updated the sole direct runtime dependency to Icod.DCurses 2.4.0 while retaining
  assembly version 1.0.0.0 and net8.0/net9.0/net10.0 support.

## 1.1.0-rc.1 — editing foundation release candidate

- Added exact mixed-EOL text documents with indexed lines, immutable bounded
  snapshots, revision/state identity, atomic edits, checkpoints, and bounded undo/redo.
- Added independent text view state and a Unicode-aware multiline editor with
  selection, navigation, indentation, insert/overwrite, read-only mode, bracketed
  paste assembly, captured pointer selection, autoscroll, and bounded projection.
- Added horizontal and vertical scrollbars, explicit guarded clipboard requests
  and completions, and focused caret intent for application-owned DCurses leases.
- Added selection, clipboard commands, undo/redo, and caret intent to single-line
  fields while preserving their 1.0 normalization, length, and Tab behavior.
- Updated the editor sample into a real in-memory editor with exact file reads,
  modified status, command shortcuts, focus escape, clipboard integration, and
  no file-writing behavior.
- Raised the sole direct runtime dependency to Icod.DCurses 2.4.0 while retaining
  net8.0/net9.0/net10.0, AnyCPU, and assembly version 1.0.0.0.

## 1.0.0 — first stable package

- Delivered the reusable .NET 8/9/10 control set: forms, lists, drop-downs,
  menus, dialogs, file and folder pickers, and domain-neutral game UI.
- Added scoped menu mnemonics and independent command shortcuts, framed popups
  and captioned dialogs, an OK-only acknowledgement, and consistent Escape
  cancellation across message dialogs.
- Hardened modal focus and lifecycle, Unicode editing, compact layouts,
  pointer handling, and stale asynchronous picker results.
- Included interactive gallery, editor, and roguelike samples; a package-only
  consumer; and an LGPL library package with a separate symbol package.
- This entry describes the prepared stable source. Live terminal qualification
  and tagged publication are tracked separately in `docs/W1014-Stable-Qualification.md`.

## 1.0.0-rc.2 — interactive acceptance completion

- Added real DCurses terminal loops and six gallery pages, file-open and roguelike
  UI samples; retained deterministic smoke modes using the same application flows.
- Added shared picker listing/path/root/basket presentation, keyboard and pointer
  selection, explicit provider-load/validation actions, and generation-guarded roots.
- Moved overlays into retained DCurses panels so closing preserves borrowed background.
- Fixed modal Tab isolation, modal detach/unwind, modeless lifecycle, hidden-container
  focus/rendering, popup scope/focus restoration and outside dismissal.
- Fixed Unicode caret editing, long drop-down/menu scrolling, Ctrl+A selection,
  wrapped messages, visible wizard validation, and action-overflow mutation ordering.
- Added separate menu mnemonics and direct command shortcuts: Alt opens a
  top-level menu, unmodified letters navigate its active level, and visible
  right-aligned Ctrl/Alt/Shift chords invoke commands without menu navigation.
- Framed expanded drop-down and menu popups with the theme border while retaining
  usable borderless rendering in viewports too compact for a complete frame.
- Added an acknowledgement-only OK dialog. Framed every dialog with its title as
  a caption and inset content, with compact fallback; Escape always reports Cancel.
- Reject stale file validation after direct basket changes; closed pickers ignore loads.
- Added regression and sample CI coverage. Stable multi-platform live qualification
  remains open; this candidate has not been published.

## 1.0.0-rc.1 — release candidate

- Established the managed `.NET 8`, `.NET 9`, and `.NET 10` library and test projects.
- Declared `Icod.DCurses 2.3.1` as the sole direct runtime dependency.
- Established LGPL-3.0-or-later library and GPL-3.0-or-later sample/test boundaries.
- Began the public host and immutable theme contracts.
- Added retained widget trees, bounded attachment, focus traversal, monotonic
  input dispatch, immutable action snapshots, stack/grid layout, and clipped labels.
- Added buttons, single-line Unicode text input, checkboxes, radio groups,
  borders, scroll indicators, and a deterministic public-API gallery smoke path.
- Added bounded single/multiple lists with stable keys, range selection,
  disabled-item rules, scrolling, replacement preservation, and timed search.
- Added drop-down selection with saved preview state, explicit commit/cancel,
  focus preservation, and bounded above/below popup placement.
- Added menu bars, popup menus, commands, checkable commands, separators,
  shortcuts, and bounded nested submenus.
- Added safe-default Yes/No and OK/Cancel dialogs, typed outcomes, nested modal
  leases, background isolation, deterministic focus restoration, and a
  caller-validated Previous/Next/Cancel wizard shell.
- Added an asynchronous provider-backed filesystem browser with stable
  controller generations, typed errors, stale-result rejection, and a local
  nonrecursive System.IO provider.
- Added a folder-selection dialog for roots/current directories with explicit
  caller-applied validation and stale/canceled acceptance protection.
- Added single/multiple existing-file selection with named suffix filters,
  all-files mode, directory-preserving filtering, and an ordered persistent
  comparer-aware selection basket.
- Added domain-neutral inventory/action lists, bounded message history,
  prioritized status fields, effective-binding help, and text/number prompts.
- Added deterministic gallery, editor, and roguelike sample smoke workflows and
  a package-only consumer restored from the produced NuGet artifact.
- Added live terminal loops for the gallery, editor, and roguelike samples, with
  resize handling, clean Q/Escape exits, and CI smoke execution on all PR hosts.
- Hardened modal isolation and lifecycle, inherited disabled/hidden state,
  pointer capture, action-queue atomicity, container cycles, compact scrolling,
  wizard completion, caret editing, and async picker input/validation.
