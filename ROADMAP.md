# Icod.Widgets Main Roadmap

**Decision date:** 2026-10-09 (America/New_York)\
**Target:** first stable release `1.0.0`\
**State:** approach and feature scope approved; detailed design/plan submitted for review\
**Repository baseline:** `d86e56deeac37b8faeadc2986c597634424ab07b` on `main`

## Purpose and recorded choices

Build a separate reusable terminal-control DLL for general applications and
roguelikes. The user approved all requested controls as the first stable-release
goal, delivered through four smaller milestones.

| Decision | Selected direction |
|---|---|
| Architecture | Widgets above DCurses; one direct runtime dependency, `Icod.DCurses` |
| Ownership | Application-owned event loop, command execution, validation, filesystem operations, and game state |
| Interaction | Common focus/navigation semantics; keyboard operation for every control; pointer support through DCurses |
| Dialogs | Explicit outcomes; modal interaction isolation and deterministic focus restoration |
| Selection | Highlight and selection are distinct; multi-selection survives navigation |
| File browsing | Shared navigation component with folder, single-file, and multiple-file policies; replaceable filesystem provider |
| Presentation | Common theme, Unicode-aware cell geometry, resize/clipping, usable compact layouts |
| Library license | `LGPL-3.0-or-later` |
| Samples/tests license | `GPL-3.0-or-later`; excluded from DLL compilation and NuGet payload |
| Platforms/tooling | Windows/macOS/Linux; managed C# 13; .NET 8/9/10; CMD, shell, PowerShell 5.1 build tooling |

The dependency baseline proposed for implementation is `Icod.DCurses 2.3.1`.
Confirm published restore availability at W1001; any change must be explicit in
the dependency record. Widgets will not declare direct Terminal/TermInfo package
references or introduce a terminal capability database/input reader.

## First stable-release scope

| Family | Required scope | Milestone |
|---|---|---|
| Foundation | Host, lightweight containers/layout, theme, focus, clipping, lifecycle | M1 |
| Forms | Labels, buttons, single-line text input, checkboxes, radio groups, borders, scroll indicators | M1 |
| Lists | Single/multiple selection, scrolling, keyboard search, disabled items | M2 |
| Drop-downs | Popup selection with explicit commit/cancel | M2 |
| Menus | Bars/popups/submenus, separators, disabled/checkable items, shortcuts | M2 |
| Dialogs | Yes/No, OK/Cancel, Previous/Next/Cancel, wrapped messages, default action, typed results | M2 |
| Folder picker | Start folder, roots/drives, parent navigation, select current folder | M3 |
| File picker | Existing single/multiple files, filters, folder navigation, selected-file display | M3 |
| Roguelike conveniences | Inventory/action lists, message history, status bars, help overlays, text/number prompts | M4 |
| Consumer qualification | Gallery, editor open-file flow, roguelike inventory/help flow; public and package-only consumers | M4 |

## Delivery decisions

1. **M1 — foundation and basic forms:** prove focus, geometry, rendering and
   application integration before composite controls.
2. **M2 — selection, menus and dialogs:** build reusable behaviors on the same
   foundation; prove modal restoration and explicit cancellation.
3. **M3 — filesystem pickers:** implement/test common browser first, then the
   distinct folder/file selection policies.
4. **M4 — roguelike conveniences and stable qualification:** exercise controls in
   realistic applications; freeze API/package/docs after acceptance.

Use `1.0.0-alpha.N` for implementation previews and `1.0.0-rc.N` only after all
four milestone gates pass. `1.0.0` is a release goal, not current package status.
No calendar estimate substitutes for a gate.

## Later candidates

Save-file/overwrite workflows; full multiline editing; data tables/tree controls;
advanced forms/data binding; asynchronous virtualized data sources; richer
accessibility integrations. These require a later scope decision and are not
prerequisites for the selected first release. No game rules or dungeon generation
belong in this library.

## Evidence and next action

This PR delivers planning and licensing only. No implementation tranche is
complete and no build/test/publication evidence is claimed.

The next action after review is W1001 in the
[development roadmap](Icod.Widgets-1.0.0-Development-Roadmap.md), using the
[design](docs/superpowers/specs/2026-10-09-icod-widgets-1.0-design.md) and
[implementation plan](docs/superpowers/plans/2026-10-09-icod-widgets-1.0.md).
Each implementation tranche records its exact commit, automated/manual evidence,
remaining limitations, and package version in a corresponding `docs/Wxxxx-*.md`
evidence file. Maintain README status and full CHANGELOG history as work lands.
