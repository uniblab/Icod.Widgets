# Icod.Widgets 1.2.0 Development Roadmap

**Target:** `1.2.0` — Document Workspace\
**Decision date:** 2026-10-12\
**Baseline:** `d71b74d0a68792c2001b609978b434bffb14f030` on `main`; 1.1.0 merged stable source\
**State:** W1201–W1210 implemented and recorded; W1211 stable qualification in progress; owner merge/tag/publication pending\
**Execution:** sequential, native C# work; no subagent delegation

## Goal and boundaries

Deliver the reusable controls required to compose a multi-document editor, not
the dedit executable. Widgets owns tabs, active-content presentation, menu and
status interaction, composable dialogs, save-target selection, and search forms.
The application owns paths, encoding/BOM/EOL metadata, document lifetime,
external changes, save safety, close approval, and search/regex execution.

Read the [approved design](docs/superpowers/specs/2026-10-12-icod-widgets-1.2-document-workspace-design.md)
and the focused implementation plans together. This roadmap owns sequence and
release gates; the design owns behavior; the plans record signatures,
files, test-first increments, and commit boundaries. The public API was frozen
at W1210 after public-consumer review.

## Compatibility contract

- Preserve all public 1.0 and 1.1 members and established defaults, including
  positional record construction/deconstruction, menu factories, dialog outcomes,
  and existing `IWidgetFileSystemProvider` implementers.
- Package `1.2.0`; retain `AssemblyVersion` `1.0.0.0`, C# 13, AnyCPU, and
  `net8.0;net9.0;net10.0`.
- Keep `Icod.DCurses 2.4.0` as the sole direct runtime dependency; Terminal
  1.29.0 remains transitive. Do not add a direct Terminal/TermInfo package.
- Use application-loop actions and completion methods. No hidden input reader,
  timer, background worker, filesystem write, or document-disposal policy.
- LGPL-3.0-or-later library, GPL-3.0-or-later samples/tests, and full prescribed
  C#/project headers remain mandatory.
- Keep existing repository-local CI/template workflow. No change to
  `uniblab/.github`; the owner merges, tags, and publishes.

## Milestones and order

| Tranche | Deliverable | Prerequisites | Acceptance gate |
| --- | --- | --- | --- |
| W1201 | API and compatibility baseline; resource budgets; public extension audit | Approved plans | Proposed API compiles in an external consumer; 1.1 API witness and existing tests pass; limits and cancellation semantics recorded |
| W1202 | Stable-key tab strip, overflow and input | W1201 | Active tab remains visible; keyboard/pointer switch and close request emit once; duplicate and empty snapshots are deterministic |
| W1203 | Replaceable content host and active-view safety | W1202 | Three editor views retain state; old capture/paste/clipboard cannot affect newly active content |
| W1204 | Dynamic menu state, pointer and command route | W1201 | Open-menu state reconciles by key; mnemonics differ from shortcuts; no duplicate dispatch or disabled invocation |
| W1205 | Actionable status fields | W1201 | Priority overflow, keyboard/pointer activation, and focus removal are deterministic; inert fields retain 1.1 behavior |
| W1206 | Child-composable modal dialogs and decisions | W1201 | Scoped child focus, safe default/cancel, generation-safe validation, and Save/Don't Save/Cancel outcomes |
| W1207 | Separate save-target picker and additive provider capability | W1206 | New/existing targets, stale validation, unsupported capability, and no writes; old providers and existing-file picker remain compatible |
| W1208 | Typed Find/Replace/Go To forms | W1206 | Search intent is typed; stale revision/view result ignored; app executes all text/search changes |
| W1209 | Integrated in-memory workspace sample and documentation | W1202–W1208 | Multi-document smoke, guidance, and external consumer demonstrate public composition without file writes |
| W1210 | Performance, API/compatibility freeze, RC qualification | W1201–W1209 | Measured stress, three-framework CI, package-only consumer, API fingerprint, exact prerelease package audit |
| W1211 | Stable qualification and owner handoff | W1210 | Exact `1.2.0` package metadata/artifact, changelog, release notes, README and sample parity, scoped live results |

Implementation and candidate evidence are in the `docs/W1201`–`W1210` records;
exact stable results and unavailable live environments are recorded in
[`W1211`](docs/W1211-Stable-Qualification.md).

The dependency graph permits focused test development, but the chosen execution
is sequential. Each tranche ends with tests, review, and a `docs/W12xx-*.md`
record containing the exact source/package versions, commands and outcomes,
remaining limitations, and any approved plan amendment. A class existing is not
evidence that its tranche is done.

## Focused plans

- [Tabs and active content](docs/superpowers/plans/2026-10-12-icod-widgets-1.2-workspace.md): W1201–W1203.
- [Commands and status](docs/superpowers/plans/2026-10-12-icod-widgets-1.2-commands-status.md): W1204–W1205.
- [Dialogs and save targets](docs/superpowers/plans/2026-10-12-icod-widgets-1.2-dialogs-save.md): W1206–W1207.
- [Search and navigation forms](docs/superpowers/plans/2026-10-12-icod-widgets-1.2-search.md): W1208.
- [Sample and release qualification](docs/superpowers/plans/2026-10-12-icod-widgets-1.2-integration.md): W1209–W1211.

## Public API decisions for implementation

Use a replaceable `WidgetContentHost` containing a separate `WidgetTextEditor`
for each application-owned view. Switching detaches the old editor and attaches
the new one atomically from the caller's perspective; detachment cancels pointer
capture and invalidates pending clipboard requests, while each
`WidgetTextViewState` preserves its own state. The application keeps the other
editors and documents alive. Do not make `WidgetTextEditor.View` mutable.

Add `IWidgetSaveTargetProvider` beside the existing
`IWidgetFileSystemProvider`. A provider composes/inspects a proposed name and
returns an observation; it does not reserve or write. Existing-file selection
continues to validate existence using the old interface. No path construction
or overwrite decision is inferred by Widgets.

Use complete caller-applied presentation snapshots keyed by stable identity.
The menu's legacy self-toggle behavior remains when no controlled state is
applied; controlled checkable items emit intent and wait for application state.
Semantic requests include source identity and generation where late completion
could otherwise act on replaced or closed UI.

Before implementation, reconcile any signature changes in all affected focused
plans and the design. W1201 validates source and binary compatibility with 1.1
public consumers; W1210 freezes the final surface only after this gate.

## Verification

### Behavior and safety

Use deterministic screen-only host tests for key phases, pointer press/move/
release, compact viewport clipping, modal isolation, escape, disabled state,
duplicate IDs, attachment capacity, empty states, and close/reopen generations.
Late clipboard, directory, target-inspection, dialog-validation, and search
results must be ignored after owner removal or state changes. Exercise action
queue capacity before mutation and prove every accepted gesture emits once.

### Performance and public integration

Measure hundreds of tabs, repeated switching among large independent views,
dynamic menu updates, and compact layouts. Record hardware/runtime, allocation,
latency, retained memory, and thresholds in W1210. Switching must not clone
document text or render hidden document projections.

Compile an external package-only consumer against the actual RC artifact. It
must compose custom controls, tabs, dialogs, save-target provider, search form,
menu/status state, and an in-memory multi-document loop without internal access.
Also run the existing 1.1 compatibility consumer against the 1.2 implementation.

### Build, package, and platforms

Use matching configurations: local scripts default to Debug; PR CI uses Staging;
release validation uses Release. Run `dotnet restore Icod.Widgets.sln`, then
build and test the solution in Staging and Release. Never use `--no-build` unless
that exact configuration/framework was built first. Run the existing gallery,
editor, and roguelike `--smoke` paths plus the new workspace smoke on net10.

CI must build/test `net8.0`, `net9.0`, `net10.0` on Windows, Linux, and macOS.
Automated CI is not live terminal qualification. Record Windows interactive
results with exact terminal and source identity; mark unavailable macOS/Linux
interactive checks **Not tested**, not passed.

Inspect the packed nupkg/snupkg and nuspec: exact 1.2 version, unchanged
assembly identity, one DCurses 2.4.0 direct dependency, three framework
assets/XML, durable packaged README/license/icon, and no sample/test binaries.
No prerelease package is uploaded without owner authorization.

## Version and publication gate

1. W1201–W1209 use an increasing `1.2.0-alpha.N` source identity; align any
   package-consumer version assertions. Preview does not imply publication.
2. W1210 freezes additive API and metadata and prepares `1.2.0-rc.N` for exact
   artifact validation; record API fingerprint and performance evidence.
3. W1211 sets both `Version` and `PackageVersion` to **1.2.0**, retains
   `AssemblyVersion` **1.0.0.0**, and verifies DCurses **2.4.0** in csproj/nuspec.
4. Update root README installation/status/inventory, the packaged README,
   sample guide, controls guide, changelog, curated `docs/releases/1.2.0.md`,
   API baseline, and qualification record to describe only shipped behavior.
5. Build, test, pack, validate, and consume the exact stable artifact; check for
   stray alpha/RC strings and stale 1.1 version pins. Verify CI at the intended
   merged commit. The owner alone merges, creates `v1.2.0`, and publishes.

Printing, tab reordering, split views, recovery, syntax highlighting, multiple
carets, file watching, codecs, atomic save, and a regex engine remain excluded.
