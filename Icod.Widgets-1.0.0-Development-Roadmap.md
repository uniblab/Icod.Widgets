# Icod.Widgets 1.0.0 Development Roadmap

**Planning date:** 2026-10-09 (America/New_York)\
**Target:** `1.0.0`, with `1.0.0-alpha.N` and `1.0.0-rc.N` qualification stages\
**State:** W1001–W1013 complete; stable 1.0.0 source prepared; W1014 live qualification pending\
**Runtime:** managed C# 13, `net8.0;net9.0;net10.0`, AnyCPU\
**Direct runtime dependency:** proposed `Icod.DCurses 2.3.1`, availability checked at W1001\
**License:** library LGPL-3.0-or-later; samples/tests GPL-3.0-or-later

**Design authority:** [1.0 design](docs/superpowers/specs/2026-10-09-icod-widgets-1.0-design.md)\
**Execution detail:** [implementation plan](docs/superpowers/plans/2026-10-09-icod-widgets-1.0.md)\
**Decision history:** [main roadmap](ROADMAP.md)

## Goal

Deliver every selected control through a reusable library with consistent
keyboard behavior, deterministic modal/results semantics, cross-platform file
browsing, and runnable application examples. Applications keep their event loop;
Widgets returns actions and draws into DCurses-owned retained presentation.

## Milestone/tranche gates

| Tranche | Milestone | Deliverable | Required acceptance |
|---|---|---|---|
| W1000 | Planning | Main roadmap, design, execution plan, license assignments | Documents agree on scope/ownership; license texts included; no implementation claimed |
| W1001 | M1 | Solution, projects, templates, API/dependency feasibility, first failing contract tests | One packable DLL; licenses/frameworks exact; DCurses public integration works; package-only restore witness |
| W1002 | M1 | Host, widget lifecycle, theme, containers and geometry | Registration/disposal bounded; Unicode/clipping; hidden/disabled focus excluded; resize has no stale regions |
| W1003 | M1 | Labels/buttons/input/checkbox/radio/border/scroll indication | Keyboard-only forms usable; Unicode edits and paste sanitized; explicit actions; basic gallery runs |
| W1004 | M2 | Single/multi-select list | Cursor distinct from selection; disabled items skipped; stable-key replacement deterministic; visible-only rendering |
| W1005 | M2 | Drop-down control | Opening/cancel restores selection/focus; commit exactly once; popup remains inside viewport |
| W1006 | M2 | Menu bar/popups/submenus | Disabled/separator inert; shortcuts respect modal precedence; submenu closure/restoration and bounds exact |
| W1007 | M2 | Modal/modeless dialogs and wizard shell | No/Cancel distinct; safe defaults; focus restored after nested close; validation/page contents application-owned |
| W1008 | M3 | Provider and shared browser | Roots/parent/path rules provider-owned; canceled/stale loads ignored; permission/disappearance errors retain usable UI |
| W1009 | M3 | Folder picker | Start/current/root selection correct; selected folder validated; failure leaves dialog open with error |
| W1010 | M3 | Single/multiple existing-file picker | Filters platform-correct; ordered deduplicated paths; basket survives navigation/filter changes; revalidation before completion |
| W1011 | M4 | Roguelike reusable controls | Inventory/actions, bounded messages, status/help and prompts work without game-model coupling |
| W1012 | M4 | Public-only gallery/editor/roguelike consumers | Keyboard, mouse where available, resize, clean cancellation/exit; package-only consumer uses packaged APIs |
| W1013 | M4 | Hardening, API/docs/package freeze and RC | Adversarial/large-data checks; exact API baseline; license/header and package payload audit; all automated gates pass |
| W1014 | Stable | Exact-head live qualification and release readiness | Windows/macOS/Linux evidence recorded; applicable x64/ARM64 smoke evidence; stable metadata/tag/version and curated notes agree |

W1000–W1013 are implemented with per-tranche evidence in `docs/`. M1–M4
automated gates are complete. W1014 live platform/terminal qualification and
stable metadata and curated notes are prepared. The live gate remains open and
is not replaced by automated runner evidence. Record that work in
[`docs/W1014-Stable-Qualification.md`](docs/W1014-Stable-Qualification.md).

## Dependency and review order

Execute W1001–W1014 in order. W1004 selection is reused by drop-downs, menus and
pickers. W1007's overlay/focus behavior is reused by pickers and help. W1008
separates provider I/O from UI state before any picker completes a selection.
Review and record each tranche before proceeding to the next.

At W1001, map the proposed host to published DCurses interaction regions,
scopes/leases, command routing, text layout, panels, and geometry. If a required
operation needs an unavailable public capability, record the specific gap and
resolve it in the owning library before promising that API. Do not invent raw
terminal operations or a second router to bypass a missing primitive.

## Verification strategy

### Deterministic automated evidence

Test pure state transitions and geometry independently of a live terminal.
Replay normalized DCurses input into the public host; compare retained cells and
returned actions. Use fake filesystem providers for roots, path comparison,
permission failure, deletion, cancellation, stale/out-of-order completion and
links. Use temporary real directories for OS integration; always clean them up.
Inject a clock into keyboard-search tests; timers and scheduling remain caller-owned.

Cover full Unicode text elements, width-two characters, zero-size and compact
viewports, repeated attach/detach/dispose, focus removal, nested overlays,
keyboard-only operation, changing items while selected, and cancellation while
file I/O is outstanding. Large-list work must scale with visible rows; browsing
is explicitly snapshot-based for 1.0 and must document its memory cost.

### Build/package evidence

At W1001 import and adapt current `uniblab/.github` tooling, preserving its
notices. The existing template defaults are local Debug, PR Staging, main
Release; validate those exact imported revisions rather than assuming historic
defaults. Use one root solution and `Debug;Staging;Release` configurations.
Build/test all target frameworks on Windows/macOS/Linux; retain applicable
x64/ARM64 execution witnesses or explicitly record an unqualified platform.

Planned commands after scaffolding:

```text
dotnet restore Icod.Widgets.sln
dotnet build Icod.Widgets.sln -c Staging --no-restore
dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Staging --no-build
dotnet pack Icod.Widgets.csproj -c Release -o artifacts/packages
dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0
```

Inspect the packed nuspec for exact version, `LGPL-3.0-or-later`, and the sole
direct runtime dependency; inspect contents for framework DLLs, XML docs and
README, and for absence of samples/tests. Compile and run a consumer against
the packed `.nupkg` with no project reference.

### Live acceptance

Record OS/architecture, terminal/version, .NET version, exact package/commit,
steps and outcomes. Exercise every requested control; focus before/after modal
close; repeated resizing; keyboard-only navigation; mouse when supported;
Unicode; failed file browsing; and clean Q/Escape exits in samples.
For raster-backed game examples, retain text-only qualification and document
DCurses immediate-raster overlay restrictions; Widgets owns no graphics fallback.

## Stable release exit criteria

- Every requested control has public API, XML documentation, automated behavior
  coverage and a runnable acceptance path.
- Current source, API fingerprint, package-only consumer and CI evidence agree.
- README retains man-page-style consumer guidance and includes installation,
  usage, keys, configuration, errors, limitations, examples and licensing.
- CHANGELOG records the full history; sample READMEs contain reproducible commands.
- Project/nuspec/package version is exactly `1.0.0`; assembly version becomes
  `1.0.0.0`; tag is `v1.0.0`; curated release notes describe actual functionality.
- NuGet publication and GitHub Release are separate explicitly authorized release
  actions. A green planning PR is not implementation or publication readiness.
