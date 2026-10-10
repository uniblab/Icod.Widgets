# Changelog

All notable changes to Icod.Widgets are recorded here.

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
