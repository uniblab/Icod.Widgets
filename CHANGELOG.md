# Changelog

All notable changes to Icod.Widgets are recorded here.

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
