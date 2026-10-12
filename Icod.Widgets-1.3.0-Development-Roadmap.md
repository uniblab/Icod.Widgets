# Icod.Widgets 1.3.0 Development Roadmap

**Target:** `1.3.0` — widget color themes\
**Decision date:** 2026-10-12\
**Baseline:** `main` merge `a009190c1b79e3aa761919466e8b854fe8a2ea8c` (1.2.0 Document Workspace)\
**State:** W1301–W1309 implemented and pushed; W1310 stable source qualified locally, remote CI pending\
**PR:** [#6](https://github.com/uniblab/Icod.Widgets/pull/6)

## Goal

Make independent foreground and background colors usable and consistent in
every Widgets control, above the Icod.DCurses 2.5.0 color surface. Give dedit a
coherent palette for its editor, active/inactive tabs, menus, dialogs, pickers,
and status bar while keeping the current terminal-default monochrome appearance
available. This release changes reusable presentation, not dedit's file or
document policy.

## Starting facts and layer boundaries

- `WidgetTheme` already supplies six complete `CursesStyle` values (`Normal`,
  `Focused`, `Disabled`, `Selected`, `Error`, `Border`). Callers can request both
  color axes with the existing constructor. This milestone adds part/state
  precision and ensures color reaches every owned cell; it does not invent a
  second color protocol.
- DCurses 2.5.0 leaves the public style API unchanged. Its requested styles,
  capability observations, per-axis fallback, retained cells, and opaque-panel
  composition are the lower-layer contract. Widgets must use public DCurses
  APIs only and must not inspect or emit Terminal/TermInfo control strings.
- The merged Widgets 1.2.0 package directly depends on DCurses 2.4.0. First
  verify that the stable 2.5.0 package is published and restorable, then move
  the *single* direct dependency to 2.5.0. Icod.Terminal 1.29.0 remains
  transitive. Use a synchronized `1.3.0-alpha.N` source/package identity during
  development; advance to RC and stable only after their respective gates.
- The application selects a palette and drives input, timing, render and
  refresh. Widgets resolves presentation roles and paints cells; DCurses
  handles terminal-specific representation and independent axis fallback.

## Compatibility and scope

Preserve all published 1.0–1.2 public members and behavior. In particular,
retain the six-position `WidgetTheme` constructor and deconstruction, existing
property names, `with` use, the monochrome `WidgetTheme.Default`, existing
`WidgetHost` constructors, and `AssemblyVersion` `1.0.0.0`. Add new styles
through non-positional members or a separate immutable style set; check record
equality and cloning against existing consumers before freezing the API.
Overrides must be scoped to an attached control or subtree and must not mutate
the shared theme. A missing part/state style resolves to the nearest defined
role and ultimately to the six published roles. No unbounded string-key style
registry or implicit background-color inheritance that loses a foreground axis.

Keep `net8.0;net9.0;net10.0`, C# 13, AnyCPU, LGPL-3.0-or-later library code,
and GPL-3.0-or-later samples/tests with the prescribed complete headers. No
new library dependency, hidden event loop, filesystem write, terminal probe,
theme-file parser, or change to the existing command/dialog semantics.

## Style resolution contract to settle in W1301–W1302

The design must name the finite set of roles and precedence before renderers
are changed. Cover at least these surfaces and parts:

| Family | Required distinctions |
| --- | --- |
| Host and containers | Application surface versus intentionally transparent container; scoped control override |
| Tabs and active content | Strip/empty area, active, inactive, disabled, focus, modified marker, close affordance, overflow |
| Text editor and single-line fields | Surface, text, disabled/read-only, focus indication, selection with and without focus, caret intent unaffected |
| Menus and drop-downs | Bar/closed field, open popup surface and padding, border, item normal/focused/disabled, shortcut/mnemonic/check marker |
| Lists and pickers | View surface, normal/selected/focused/disabled rows, paths, validation error, basket/filter and empty rows |
| Dialogs/forms/buttons | Opaque interior, caption, border, normal/default/focused/disabled action, validation error |
| Scrollbars and status/game controls | Track/thumb and focus, status surface/field/action, indicators, labels, check/radio/border/prompt states |

An active tab remains visibly active when keyboard focus is in the editor.
Selection and focus are independent concepts; a selected editor range remains
identifiable when focus moves. Disabled wins over an interactive hover/focus
state; validation/error remains distinguishable without color. Resolve a full
`CursesStyle` for each part/state, not a single color that silently replaces
the other axis. Reverse is a requested attribute, never a stored swap of the
foreground and background. Legacy six-role themes keep their old rendering
where possible; any necessary difference for complete surface ownership gets
an explicit compatibility test and release note.

## Ordered milestones

| Tranche | Deliverable | Prerequisite | Exit evidence |
| --- | --- | --- | --- |
| W1301 | DCurses 2.5 package and baseline/API audit | Published 2.5.0 | Exact restore; current 1.2 tests/smokes against 2.5; source/binary API witness; inventory of every renderer and owned surface |
| W1302 | Immutable style roles, fallback and scoped override contract | W1301 | Tests for all role/state precedence, both color axes, old constructor/deconstruct/with/equality, nested overrides and clear/removal |
| W1303 | Host theme replacement and surface ownership | W1302 | Caller-triggered theme switch repaints without dropping focus, active tab, modal state or pending actions; opaque blank cells and borrowed background distinguished |
| W1304 | Core controls and text editing | W1303 | Labels, borders, buttons, checks/radios, lists, text input/editor and selection render full styled bounds; Unicode widths and editing unchanged |
| W1305 | Tabs, status, scrollbars and game controls | W1304 | Active-unfocused tab, modified marker, track/thumb, actionable field and priority overflow have independent styles; pointer/keyboard behavior unchanged |
| W1306 | Menus, drop-downs, dialogs and pickers | W1303–W1305 | Framed/compact popups and modal interiors fill owned cells; caption/disabled/error/selection survive resize, close/reopen and nested panels |
| W1307 | Capability-aware palette and non-color fallback | W1302–W1306 | Public DCurses capabilities select default/indexed/RGB requests conservatively; one unsupported axis falls back independently; monochrome remains usable |
| W1308 | Public sample and user documentation | W1307 | Gallery demonstrates both axes across all widget families, a dark/light or palette switch, and fallback; existing smoke path plus new deterministic smoke; README explains controls and limits |
| W1309 | Stress, API/package freeze and RC | W1301–W1308 | Measured repaint/allocation budget; old package consumers; new package-only consumer; API fingerprint; exact prerelease package and CI matrix |
| W1310 | Stable `1.3.0` qualification and handoff | W1309 | Exact stable package, symbol and metadata inspection; README/sample/changelog/release-note parity; clean exact-commit CI; owner merge/tag/publication |

Implement sequentially in PR #6 with a reviewable commit and push after each
completed tranche. Each `docs/W13xx-*.md` record names source and dependency
versions, commands/results, remaining risks, and any scope change. Tests should
first expose an observable failure, then verify the minimal correction. Freeze
public signatures only after the W1309 external-consumer gate.

## Rendering acceptance matrix

Use screen-only tests to inspect `CursesCell.Style` at text, trailing blanks,
padding, blank rows, borders, selected cells, and both halves of wide graphemes.
Test at normal and one/two-column compact bounds, clipped and offscreen regions,
resize, repeated render, hover/focus changes, modal stacking, and popup removal.
Compare logical and composed output so a styled blank in an opaque panel covers
underlay, while intentionally transparent or non-owned cells preserve it.
Closing an overlay must reveal the original styled background without copying
staging-screen default blanks into unrelated cells.

Exercise foreground-only, background-only, and paired styles; default/indexed/
RGB color requests; Reverse combined with an explicit pair; unsupported color
axes; and profiles with restricted attributes. Preserve distinct active,
selected, disabled and error cues when colors cannot be shown. Capability
selection is a public, deterministic function of DCurses observations; never
claim RGB or an indexed count that the observed profile does not advertise.
Do not reimplement DCurses fallback or approximate unavailable colors in
Widgets. An actual terminal may still render a requested palette differently.

Renderers to audit include `src/Core/WidgetHost.Presentation.cs`,
`src/Controls/`, `src/Workspace/WidgetTabStrip.cs`, `src/Menus/`,
`src/Dialogs/`, `src/Pickers/`, `src/Search/`, and `src/GameUi/`. Existing
interaction tests remain regression gates for mnemonics versus shortcuts,
Escape = Cancel, dynamic menu state, focus and capture restoration, clipboard
completion, tab activation, and action delivery. The colored gallery uses
only public Widgets/DCurses APIs and must not write files.

## Build, compatibility and release gates

1. In W1301, pin the sole direct package dependency to published
   `Icod.DCurses 2.5.0`; do not change the 1.2 historical records. Restore and
   run the existing gallery, editor, roguelike and workspace `--smoke` paths.
2. For each implementation tranche, run focused tests, the full solution test
   suite on available frameworks, and the relevant public sample smoke. Build
   each configuration before using `--no-build`. PR CI must build/test all
   three target frameworks on Windows, Linux and macOS, including package
   validation. Automated CI is not a live terminal color observation.
3. W1309 compares the exported API with 1.0, 1.1 and 1.2 baselines, compiles
   an external consumer against the new theme API, and runs consumers built
   against prior packages with the 1.3 DLL. Check the six-role record's binary
   construction and deconstruction. Measure repeated theme switches and large
   editor/tab surfaces without reprojecting hidden documents or allocating a
   screen-sized staging buffer per control unnecessarily.
4. Package `1.3.0-rc.N` only for local/CI qualification; inspect nupkg and
   snupkg for three DLL/XML pairs, license, icon, durable packaged README,
   `Icod.DCurses 2.5.0` as the only direct runtime dependency, and absence of
   sample/test binaries. A fresh package-only consumer must restore and run.
5. W1310 synchronizes `Version` and `PackageVersion` at `1.3.0`, retains
   `AssemblyVersion` `1.0.0.0`, updates CHANGELOG, root README, samples index,
   gallery guide, curated `docs/releases/1.3.0.md`, API baseline/fingerprint
   and qualification record. Check packaged README links and commands, package
   dependency metadata, stale alpha/RC strings, and exact stable artifact
   hashes. Verify the intended merged commit before the owner's tag and NuGet
   publication.

Record any available interactive Windows terminal observation with exact
terminal, OS, and source identity. Mark unavailable macOS/Linux visual checks
**Not tested**, even when their automated CI passes. Do not make a live visual
check an implicit prerequisite that contradicts the agreed release approach.

## Exclusions and handoff to dedit

Syntax highlighting, rich text, a theme-file format, live appearance-change
subscription, animation, terminal palette mutation, custom color quantization,
printing, and dedit-specific file/encoding/save policy are outside 1.3. dedit
may proceed with functional document workflows on Widgets 1.2.0; once 1.3.0
is published it can consume the style roles for its final visual design.
The maintainer alone merges, tags, and publishes.
