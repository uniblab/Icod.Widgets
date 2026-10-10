# Changelog

All notable changes to Icod.Widgets are recorded here.

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
