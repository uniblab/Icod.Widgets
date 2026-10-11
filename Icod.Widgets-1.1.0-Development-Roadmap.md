# Icod.Widgets 1.1.0 Development Roadmap

**Target:** `1.1.0` — reusable editing foundation\
**Decision date:** 2026-10-11\
**Baseline:** `d56cb9a06b78b8fa975de23cf590e6d198e36fc3` on `main`; stable API/package 1.0.0\
**State:** planning only; W1101–W1113 not started\
**Execution:** sequential, in the main working session; no subagent delegation

## Goal and scope

Make Icod.Widgets a sound single-document editing foundation for dedit and other
terminal applications. Separate exact document text and history from view state
and rendering. Deliver keyboard/mouse editing, bounded viewport work, undo/redo,
scrollbars, explicit clipboard integration, and caret coordination while keeping
the application in charge of its loop, async work, and file policy.

This is not the tabbed dedit executable. Tabs, composite dialogs, save-target
selection, dynamic menus, search forms, and actionable status fields belong to
the later [1.2 direction](ROADMAP.md#120--document-workspace).

Read the [design specification](docs/superpowers/specs/2026-10-11-icod-widgets-1.1-design.md)
and [task-by-task plan](docs/superpowers/plans/2026-10-11-icod-widgets-1.1.md)
together. This roadmap owns delivery order and acceptance; the design owns
behavior; the plan owns file/interface/test changes. Proposed new API names are
reviewed at W1101 and frozen only at W1112.

## Compatibility and dependency contract

- Preserve the 1.0 public members, enum values, constructor/factory signatures,
  positional-record constructors/deconstruction, and established default behavior.
- Keep assembly version `1.0.0.0`; do not encode package minor versions into
  assembly identity. Retain C# 13, `net8.0;net9.0;net10.0`, and AnyCPU.
- At W1101 update the sole direct production dependency from DCurses 2.3.1 to
  **2.4.0**. Terminal **1.29.0** remains transitive. Use released public APIs;
  no direct Terminal/TermInfo packages, internal access, protocol strings, or
  duplicated Unicode cell-width engine.
- Keep existing screen-only and session-based host construction. Clipboard and
  caret services must not require headless users to create a terminal session.
- Keep LGPL-3.0-or-later library and GPL-3.0-or-later samples/tests with full
  prescribed source/project headers. No native or Python runtime implementation.
- Use the existing repository-local library workflows and build tooling. No PR
  or settings change in `uniblab/.github`; no merge, tag, or publish by the agent.

## Milestones and dependency order

| Tranche | Deliverable | Prerequisites | Acceptance gate |
| --- | --- | --- | --- |
| W1101 | Dependency baseline, compatibility witness, document-store experiment, resource/API decision | Approved plan | 1.0 regressions on DCurses 2.4; measured candidate comparison; explicit limits and APIs recorded |
| W1102 | Rich input fidelity | W1101 | Function keys, associated text, key phase, focus state, and paste phase survive normalization; release never activates twice |
| W1103 | Focus, capture, contextual edit commands, extension hooks, caller ticks | W1102 | Editor Tab and form traversal coexist; captured move/release/cancel and scoped commands are deterministic |
| W1104 | Exact document text, line index, revisions, immutable snapshots | W1101 | Chunked text retained exactly; Unicode/EOL-safe ranges; incremental indexing and stable snapshot reads |
| W1105 | Transactions, undo/redo, checkpoint identity | W1104 | Grouped changes are atomic; bounded history; undo to checkpoint cleans, stale save does not clean newer text |
| W1106 | View state and bounded projection | W1104, W1105 | Caret/selection/scroll separate from text; visible-slice cache and long-line continuation remain bounded |
| W1107 | Multiline editing control | W1102, W1103, W1105, W1106 | Unicode movement/editing, selection, indent/outdent, insert/overwrite, read-only, one-transaction paste |
| W1108 | Interactive scrollbars and selection drag/autoscroll | W1103, W1106, W1107 | Both axes, overflow-safe thumb mapping, outside capture, monotonic caller-driven updates and cancellation |
| W1109 | Explicit clipboard request/completion flow | W1103, W1105, W1107 | Bounded copy/cut/paste, no silent truncation, app fallback, stale-result rejection, no destructive failed cut |
| W1110 | Focused caret coordination | W1103, W1106, W1107 | Correct visible position, insert/overwrite style intent, hidden/offscreen/modal cancellation, safe lease cleanup |
| W1111 | Single-line field parity | W1103, W1105, W1109 | Selection and contextual copy/cut/paste/undo without changing 1.0 single-line normalization/limits |
| W1112 | Public samples, performance/compatibility audit, API freeze and RC | W1101–W1111 | Real public consumer, measured edit/render behavior, docs/package/API parity, all automated gates |
| W1113 | Stable qualification and release preparation | W1112 | Exact 1.1.0 artifact and metadata, environment-scoped live results/waivers, owner handoff |

Execute in listed order; dependencies describe contracts, not an instruction to
delegate tasks. Each tranche closes with tests and a review before the next one.
Record evidence in `docs/W11xx-<subject>.md`, including exact SHA, package version,
commands/results, remaining limitations, and any explicitly approved plan change.
Do not mark a tranche complete because its classes exist.

## First gate: decisions that must precede implementation

W1101 compares at least a gap-buffer and piece-table/tree candidate behind the
same internal experiment harness. Compare sequential typing, edits at distant
offsets, many short lines, a single huge line, snapshots retained during edits,
undo trimming, and mixed Unicode/EOL. Record time, allocations, retained memory,
index updates, and viewport work separately. Select the simplest candidate that
passes the agreed workload, not a data structure by reputation.

The decision record must specify coordinate types; snapshot ownership and limits;
document/transaction/history/cache budgets and overflow outcomes; undo grouping;
default indentation/newline policy; word movement; and quantitative performance
targets for the measured environment. Explicitly approve those limits before
implementing W1104–W1108. The experiment may be revised without breaking public
API because its storage types are internal.

Start with the proposed interfaces in the implementation plan. If measurements
require a different boundary, amend the design and plan together before dependent
tasks begin. A real DCurses API gap must be demonstrated by a minimal public-API
reproducer; do not assume an additional lower-layer release is needed.

## Verification strategy

### Behavior and data safety

Use deterministic model tests and randomized edit/undo sequences against a
simple string oracle. Include empty text, CR/LF/CRLF and mixed endings, no/final
newline, tabs, combining sequences, supplementary characters, ZWJ emoji,
width-two characters, isolated surrogate code units, controls, and very long
grapheme clusters. Preserve source even when rendering uses a safe placeholder.

Exercise exact paste assembly, release/repeat handling, focus changes, modal
isolation, stale completions, reattachment with the same widget ID, read-only
changes while work is pending, capacity rejection before mutation, and history
branching/eviction. A successful copy transport does not prove host clipboard
acceptance; a timeout is not proof of unsupported capability.

### Performance and public integration

Measure real document storage and editing, not only a synthetic visible-record
source. Initial loading/indexing may scan the input once; ordinary edits and
viewport changes must not recreate the entire file or line. Bound layout slices,
cache retention, oversized-cluster fallback, and caller-tick work. Separate
cold far-offset seeks from warm local edits. Record hardware/runtime, workload,
raw measurements, and chosen ceilings in the W1101 and W1112 evidence.

Build a package-only external consumer without project references or internals.
It must exercise document/snapshot/history, a custom action-emitting control,
editor and field command precedence, stale clipboard completion, and screen-only
operation. Run a consumer compiled against 1.0 with the 1.1 implementation as
well as compiling a new 1.1 consumer. Review new API across all target frameworks.

### Build and package checks

Use matching configurations; never run `test --no-build` against missing or
differently configured binaries. The local scripts default to Debug, PR CI uses
Staging, and distribution/release validation uses Release.

```text
dotnet restore Icod.Widgets.sln
dotnet build Icod.Widgets.sln -c Staging --no-restore
dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Staging --no-build
dotnet build Icod.Widgets.sln -c Release --no-restore
dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release --no-build
dotnet pack Icod.Widgets.csproj -c Release --no-build -o artifacts/packages
dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0 --no-build -- --smoke
dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0 --no-build -- --smoke
dotnet run --project samples/Icod.Widgets.Roguelike.Sample -c Release -f net10.0 --no-build -- --smoke
```

Run tests on .NET 8/9/10 and the repository's Windows/Linux/macOS CI matrix.
Inspect the actual nupkg/nuspec: exact version, sole direct DCurses dependency,
framework DLL/XML assets, README/license/icon, symbol package, and no compiled
samples/tests. Restore the consumer from that exact artifact, not NuGet's old 1.0
package. Keep the existing distribution verifier in the release gate.

### Live qualification

Exercise Unicode edits/undo, both scroll axes, captured drag outside bounds,
autoscroll, resize during input/paste, read-only, clipboard denial/timeout/limits,
caret restoration, menus and nested dialogs, clean shutdown, and old samples.
Record exact commit/package, OS/build/architecture, terminal/version, SDK/runtime,
scenario, and outcome. Mark unavailable macOS/Linux live environments **Not tested**;
the owner's prior decision to proceed without those machines does not become a
pass or waive automated tests. Record accepted limitations for this release.

## Version and publication checklist

This documentation PR leaves `Version`, `PackageVersion`, and the DCurses
reference unchanged. It must not claim to be an implementation or release build.

- [ ] W1101: use `1.1.0-alpha.1` for implementation; align package-consumer and
  test expectations. Further previews use increasing `alpha.N` values.
- [ ] W1112: after feature gates, freeze the additive API snapshot and prepare
  `1.1.0-rc.N`; publish no prerelease without explicit owner authorization.
- [ ] W1113: set both `Version` and `PackageVersion` to **1.1.0**, keep
  `AssemblyVersion` **1.0.0.0**, and verify DCurses **2.4.0** in project and nuspec.
- [ ] Update package description/tags/release notes for actual shipped editing
  features; retain the existing library workflow. If GitHub topics are updated,
  respect its maximum of **20** and obtain authorization for that metadata write.
- [ ] Update README install/status/dependency text, sample guide, CHANGELOG,
  curated `docs/releases/1.1.0.md`, API baseline, and qualification record together.
  Keep README links full GitHub URLs and installation instructions durable.
- [ ] Build/test/pack the exact stable source and consume the resulting package;
  check prerelease strings and old version pins rather than trusting filenames.
- [ ] Record live results and limitations; verify CI and release-workflow inputs
  at the actual merged SHA, not just the PR's earlier head.
- [ ] Owner merges and creates `v1.1.0`; the configured tag workflow publishes.
  Verify that the tag matches the artifact version and intended commit. The agent
  does not merge, tag, or publish as part of this plan.

## Completion definition

1.1.0 is ready only when all W11xx gates have evidence and known limitations are
accepted. Deliver useful single-document editing and single-line field parity;
do not silently grow the milestone into tabs, safe save, search, or printing.
