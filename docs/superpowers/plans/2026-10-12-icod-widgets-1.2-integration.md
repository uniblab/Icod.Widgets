# Icod.Widgets 1.2 Integration and Release Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Execute natively without subagents.

**Goal:** Demonstrate the complete public workspace API and qualify the exact 1.2.0 library/package for owner-controlled release.

**Architecture:** A dedicated sample composes in-memory documents/views with tabs, commands, dialogs, save-target observations, search requests, and status actions in one caller-owned loop. Public package consumers and binary compatibility checks validate API boundaries. RC and stable gates audit exact artifacts, not only source builds.

**Tech Stack:** C# 13, net8.0/net9.0/net10.0, Icod.DCurses 2.4.0, xUnit, repository PowerShell 5.1-compatible packaging scripts.

**Spec:** [1.2 Document Workspace design](../specs/2026-10-12-icod-widgets-1.2-document-workspace-design.md)

## Global Constraints

- Package `1.2.0`; assembly `1.0.0.0`; sole direct runtime package `Icod.DCurses 2.4.0`.
- Preserve public 1.0/1.1 compatibility; no file writes in workspace sample or library.
- Samples/tests GPL-3.0-or-later, library LGPL-3.0-or-later, full prescribed C#/csproj headers.
- Root README is packed by `Icod.Widgets.csproj`; full GitHub URLs for links outside the package and timeless installation wording.
- User owns merge, `v1.2.0` tag, and publication; unavailable live macOS/Linux checks remain Not tested.

## Review Focus

- App denies close after Cancel: document and tab stay alive with unchanged active view (Task 1).
- App accepts Don't Save: sample removes only that tab and does no write (Task 1).
- Delayed search or save observation after another tab becomes active: cannot alter the active view (Task 1).
- Package-only consumer accidentally resolves source project or NuGet 1.1: fail exact artifact validation (Task 2).
- Stable tag/version mismatch or residual `alpha`/`rc` strings: release gate fails before owner handoff (Task 3).

---

## File map and release contracts

Create `samples/Icod.Widgets.Workspace.Sample/Icod.Widgets.Workspace.Sample.csproj`, `Program.cs`, and `README.md`; add project to `Icod.Widgets.sln`; use `samples/Common/*.cs` for the established application loop. The sample's `--smoke` entry point runs headlessly, returns nonzero on failure, and prints one explicit PASS line; it never writes user files. Its in-memory save-target provider returns inspection observations only.

Extend `tests/PackageConsumer/Program.cs` and its package reference to the exact RC/stable version during qualification; keep it independent of project references and internals. Keep `tests/BinaryCompatibilityConsumer`'s 1.0 reference as its default; make the reference version overridable to 1.1 at build time, and execute both compiled baselines against 1.2. Keep existing CI workflow pattern; only add a workspace smoke invocation if CI does not discover it automatically.

### Task 1: W1209 integrated sample and documentation

**Files:** Create sample project/Program/README and `docs/W1209-Workspace-Sample.md`; modify `Icod.Widgets.sln`, `samples/README.md`, `docs/Controls-and-Interaction.md`; add `tests/Icod.Widgets.Tests/Workspace/WidgetWorkspaceScenarioTests.cs`. Do not update root packaged README to claim unshipped features before stable gate.

**Interfaces:** Consumes tabs/content host, dynamic menus/status, form/decision/save dialogs, search/go-to requests from prior plans. Sample owns `Dictionary<string, (WidgetTextDocument Document, WidgetTextViewState View, WidgetTextEditor Editor)>` and application policy. No file-writing API is invoked.

- [ ] **Step 1: Write failing scenario/smoke checks.** `ThreeDocumentsSwitchAndPreserveState`, `CancelCloseKeepsTab`, `DontSaveRemovesOnlySelectedTab`, `LateSaveAndSearchAreIgnored`, `AltVPlainFAndCtrlFRemainDistinct`, and `SaveProviderHasNoWritePath`; smoke asserts these plus status commands and compact tab overflow. Include application-owned last-query state to drive Find Next after the form closes.

  ```csharp
  Assert.Equal( 3, tabs.Tabs.Count );
  Assert.Same( firstView, firstEditor.View );
  Assert.Contains( "dirty", tabs.Tabs.Select( tab => tab.Key ) ); // Cancel did not remove it.
  Assert.Equal( 0, saveProvider.WriteCount );
  ```
- [ ] **Step 2: Verify red.** Run filtered `WidgetWorkspaceScenarioTests` and `dotnet run --project samples/Icod.Widgets.Workspace.Sample -c Release -f net10.0 -- --smoke`; expect missing sample/scenario implementation.
- [ ] **Step 3: Implement.** Add GPL headers and project to solution; compose public controls in a caller-owned input/render loop, use in-memory documents only, and handle semantic actions in one controller. Sample decision handlers never infer Save from Cancel. Add live/sample guide usage, keys, limitations, and smoke command.
- [ ] **Step 4: Verify green.** Build solution Release, run filtered/full net10 tests and all four sample `--smoke` paths; expect zero failures and explicit PASS lines. Record W1209 evidence.
- [ ] **Step 5: Commit.** Stage named files and `git commit -m "sample: demonstrate in-memory document workspace"`.

### Task 2: W1210 public API, performance and RC qualification

**Files:** Modify `tests/PackageConsumer/Program.cs`, `tests/PackageConsumer/PackageConsumer.csproj`, `tests/BinaryCompatibilityConsumer/*`, `Icod.Widgets.csproj`, `CHANGELOG.md`; create `docs/Public-API-Baseline-1.2.md`, `docs/Public-API-Fingerprint-1.2.json`, `docs/W1210-RC-Qualification.md`; modify CI only if workspace smoke is not covered. Add focused measurements to `tests/EditorMeasurements/Program.cs` or a new `tests/WorkspaceMeasurements` project.

**Interfaces:** Freeze tested additive API after comparing with `docs/Public-API-Baseline-1.1.md`. Use package version `1.2.0-rc.1` (increment `.N` for another RC), assembly `1.0.0.0`, DCurses `2.4.0`. The external consumer restores from the exact newly packed nupkg, not a remote prior package.

- [ ] **Step 1: Write failing gates.** Add public-only consumer calls for each new control/provider/action, binary-compatibility runs separately compiled against 1.0 and 1.1 then executed against 1.2, and an artifact assertion for RC version/dependency/assets. Add measurement cases: 500 tabs, 10,000 switches across three 16 MiB documents, menu updates, width 1/12/80; assert no document copies and under 256 KiB allocated per switch after warmup, while recording latency and retained memory rather than imposing an unstable wall-clock threshold.

  ```csharp
  Assert.Equal( "1.2.0-rc.1", nuspecVersion );
  Assert.Equal( "2.4.0", directDcursesVersion );
  Assert.True( allocatedPerSwitch < 256 * 1024, $"Allocated {allocatedPerSwitch} bytes/switch" );
  ```
- [ ] **Step 2: Verify red.** Run package consumer before the RC artifact exists and new measurements/gates; expect missing RC artifact or version mismatch, not an unrelated restore error.
- [ ] **Step 3: Implement.** Freeze additive API snapshot/fingerprint; set `Version`/`PackageVersion` to `1.2.0-rc.1`, update release notes/tags to actual features, pack with matching configuration, and point package-only consumer to the local exact artifact. Add `BaselineVersion` MSBuild property to binary consumer, default `1.0.0`, with `1.1.0` override for the second compile. Do not publish RC without owner authorization. Record hardware/runtime and raw measurement results; resolve any budget failure before freeze.
- [ ] **Step 4: Verify green.** Run Staging and Release solution builds/tests, all sample smokes, `dotnet pack Icod.Widgets.csproj -c Release --no-build -o artifacts/packages`, repository package verifier, external package consumer, binary compatibility consumer, and the Windows/macOS/Linux PR matrix. Expect no failed jobs and exact RC nuspec. Record W1210 evidence; unavailable live terminals remain Not tested.
- [ ] **Step 5: Commit.** Stage named files and `git commit -m "release: qualify 1.2 workspace candidate"`.

### Task 3: W1211 stable artifact and owner handoff

**Files:** Modify `Icod.Widgets.csproj`, root `README.md`, `samples/README.md`, `CHANGELOG.md`, `tests/PackageConsumer/PackageConsumer.csproj`; create `docs/releases/1.2.0.md`, `docs/W1211-Stable-Qualification.md`; update `Icod.Widgets-1.2.0-Development-Roadmap.md` evidence/status without changing the permanent design boundary.

**Interfaces:** Exact package `1.2.0`, assembly `1.0.0.0`, sole direct `Icod.DCurses 2.4.0`. Root packaged README now describes shipped features with full external GitHub URLs and current timeless installation instructions; version chronology lives in changelog/release notes.

- [ ] **Step 1: Write failing stable checks.** Assert csproj/nuspec `Version` and `PackageVersion` are `1.2.0`, assembly is `1.0.0.0`, package consumer references 1.2.0, no prerelease string in packed README/release metadata, and no built samples/tests in nupkg. Assert new sample command and feature inventory appear in root/sample README.

  ```csharp
  Assert.Equal( "1.2.0", nuspecVersion );
  Assert.Equal( new Version( 1, 0, 0, 0 ), loadedAssemblyVersion );
  Assert.DoesNotContain( "alpha", packedReadme, StringComparison.OrdinalIgnoreCase );
  ```
- [ ] **Step 2: Verify red.** Run release metadata/package checks on RC; expect version mismatch.
- [ ] **Step 3: Implement.** Set exact stable versions and package notes; reconcile changelog, release notes, root/sample docs, public API links and qualification limitations. Do not claim unavailable macOS/Linux interactive testing passed. Keep owner-controlled merge/tag/publish unchecked.
- [ ] **Step 4: Verify green.** Restore/build/test Staging and Release with matching binaries across net8/net9/net10; run all four smoke paths; pack and inspect nupkg/snupkg/readme/license/icon/XML/nuspec; consume exact stable package and run binary witness. Verify CI at final PR SHA and record exact commands/results in W1211. Report any inaccessible platform as Not tested.
- [ ] **Step 5: Commit.** Stage named files and `git commit -m "release: prepare Icod.Widgets 1.2.0 stable"`. Hand off PR; do not merge, tag, or publish.

## Handoff

The release decision requires W1201–W1211 evidence plus owner acceptance of limitations. A green documentation PR is not a release qualification.
