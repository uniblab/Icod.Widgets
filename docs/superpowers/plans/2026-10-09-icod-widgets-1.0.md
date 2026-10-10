# Icod.Widgets 1.0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Execution method is subject to user review; this plan does not authorize subagents or publication.

**Goal:** Deliver all approved application/roguelike controls as a reusable 1.0.0 DLL with consistent behavior, public-only samples, and qualified packages.

**Architecture:** Application-owned loops feed normalized DCurses input into a retained widget host and consume immutable semantic actions. Controls share DCurses presentation/routing, and filesystem pickers share an asynchronous provider-backed browser whose results are applied explicitly on the application loop.

**Tech Stack:** C# 13; .NET 8/9/10; AnyCPU; Icod.DCurses; xUnit following Icod test conventions; CMD/shell/PowerShell 5.1 tooling from `uniblab/.github`.

**Spec:** [Icod.Widgets 1.0 design](../specs/2026-10-09-icod-widgets-1.0-design.md).

## Global constraints

- Managed C# 13; target `net8.0;net9.0;net10.0`; AnyCPU.
- Exactly one direct production package dependency: `Icod.DCurses`, proposed baseline `2.3.1`, verified as restorable at W1001.
- No direct Terminal/TermInfo package references, raw terminal encoding, input reader, alternate capability database, or replacement DCurses router.
- Library `LGPL-3.0-or-later`; samples/tests `GPL-3.0-or-later`; applicable notices on authored source/project files; GPL code excluded from DLL/package.
- One root `Icod.Widgets.sln`, root `Icod.Widgets.csproj`, library source under `src/`, separate non-packable projects under `samples/` and `tests/`.
- Tooling uses .NET, CMD, shell and PowerShell 5.1; no Python or native helper.
- Applications own event reading, scheduling, command execution, validation, persistence, game state, file opening/writing, and session lifecycle.
- Implementation previews `1.0.0-alpha.N`; RC after milestone gates; stable `1.0.0` with assembly version `1.0.0.0`.

## Review focus

1. A focused control is removed while an overlay is open: restoration selects an eligible surviving control and releases capture (W1002/W1007).
2. Text contains combining sequences, wide characters and control-character paste: edits/clipping preserve legal boundaries and safe cells (W1003).
3. Lists reorder/disable items while a multi-selection exists: stable selection keys and deterministic cursor/action behavior remain coherent (W1004).
4. Two browser requests complete backwards, or a picker closes during validation: stale completion cannot change UI or accept a selection (W1008/W1010).
5. A tiny viewport intersects a deep menu/dialog or an immediate raster view: bounds stay valid, cancellation works, and examples use an explicit composable presentation path (W1006/W1012).

---

## Execution conventions

This is a staged initial-release plan; follow the tranche gates in the
[development roadmap](../../../Icod.Widgets-1.0.0-Development-Roadmap.md).
W1001 confirms the public dependency mapping and proposed signatures before
implementation. Record exact finalized signatures in `docs/Public-API-Candidate-1.0.md`;
later tasks must update both candidate and spec when a contract changes.

For each behavioral task, add the named assertions below, run the focused tests
and confirm failure from missing behavior, implement that task's signatures, run
the tests again, and commit only after the acceptance checks pass. Test names
below are precise behavioral requirements; implementations may split a test into
smaller cases. Documentation/configuration checks require direct validation,
not tests that merely repeat literals. Record fresh commands/results and the
exact commit in `docs/Wxxxx-<topic>.md`.

Use this command pattern for focused tests after W1001 scaffolding:

```text
dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Staging -f net10.0 --filter FullyQualifiedName~WidgetHostTests
```

Replace the filter with the task's named test class. Expected RED: missing API or
the specified invariant fails, not an unrelated compile/environment error.
Expected GREEN: zero failed tests. Run all target frameworks at milestone closure;
run broader checks only for new changes or unresolved concerns.

## Planned file responsibilities

| Location | Responsibility |
|---|---|
| `Icod.Widgets.csproj`, `Directory.Build.props`, `Icod.Widgets.sln` | DLL identity, versions, explicit library sources, shared configurations |
| `src/Core/` | Host, lifecycle, actions, theme, DCurses adapter and limits |
| `src/Layout/` | Containers/stack/grid and arranged/clipped geometry |
| `src/Controls/` | Basic forms, lists and drop-down behaviors |
| `src/Menus/` | Menu items, bars, popup/submenu state |
| `src/Dialogs/` | Results, message/wizard dialogs and modal leases |
| `src/FileSystem/` | Provider, snapshots, request/result types and loader |
| `src/Pickers/` | Shared browser, folder/file policy, selection basket |
| `src/GameUi/` | Domain-neutral inventory/actions, messages, status/help/prompts |
| `tests/Icod.Widgets.Tests/` | GPL deterministic contracts, integration and qualification tests |
| `samples/Icod.Widgets.*.Sample/` | GPL public-only gallery/editor/roguelike consumers |

One main type per file; keep behavior tests beside their topic folders. Avoid
spreading provider policy into menus/lists or application game state into controls.

## W1001 — repository/tooling and public-contract feasibility

**Files:** create `Icod.Widgets.sln`, `Icod.Widgets.csproj`, `Directory.Build.props`,
`tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj`,
`tests/Icod.Widgets.Tests/Core/DependencyBoundaryTests.cs`,
`docs/Public-API-Candidate-1.0.md`, `CHANGELOG.md`; import/adapt `.editorconfig`,
`build.cmd`, `build.sh`, `main.json`, `packaging/`, `.github/workflows/` from the
current `uniblab/.github` templates with notices preserved.

**Interfaces:** consumes design section 4 and published DCurses region/scope,
command, input, panel and text/geometry APIs. Produces the compilable candidate
API ledger, one packable DLL and a test harness for later tasks.

- [x] Restore DCurses 2.3.1 from NuGet in an isolated consumer; inspect public types and map every host operation to an actual DCurses member. Record exact package/hash and any changed baseline explicitly.
- [x] Scaffold the single solution/library/test project. Use explicit `src/**/*.cs` inclusion; disable default compile globbing; declare GPL test metadata and non-packability. Import current tooling only after reading its setup requirements.
- [x] Implement `DependencyBoundaryTests.LibraryDoesNotCompileGplSources` and `CandidateHostUsesOnlyPublicDcursesApi`: assert assembly/project inclusion excludes samples/tests and compile the public DCurses mapping without private reflection or terminal bytes.
- [x] Record proposed core, list, menu, dialog, provider and picker signatures/enums/defaults in the candidate; resolve feasibility gaps before functional implementation. Add the first W1002 missing-behavior tests and confirm meaningful RED.
- [x] Run restore/build/pack with the adapted tooling; inspect nuspec license/direct dependencies and DLL payload. Commit `build: establish Widgets library and public contract baseline`; record W1001 evidence. Do not claim unimplemented widgets work.

## W1002 — host, lifecycle, layout and theme (M1)

**Files:** create `src/Core/{Widget,WidgetHost,WidgetAction,WidgetDispatchResult,WidgetTheme,WidgetLimits,WidgetDcursesAdapter}.cs`,
`src/Layout/{WidgetContainer,WidgetStack,WidgetGrid}.cs`,
`tests/Icod.Widgets.Tests/Core/WidgetHostTests.cs`,
`tests/Icod.Widgets.Tests/Layout/WidgetLayoutTests.cs`.

**Interfaces:** consumes W1001 DCurses mapping. Produces the section-4 host
constructor and `Attach`, `Detach`, `Arrange`, `Process`, `DrainActions`, `Render`,
`Dispose` signatures, widget state, theme roles and layout containers.

- [x] Add `AttachRollbackLeavesNoRegions` (capacity failure leaves no registrations), `FocusRemovedWhileOverlayPending` (eligible successor), `ActionsDrainOnce` (detached snapshots), `ClockCannotMoveBackwards` (reject before mutation), and `DisposedHostKeepsBorrowedSessionAlive`.
- [x] Add layout assertions: zero-size rectangle registers no visible hits; wide/combining text clips legally; shrink/grow updates bounds before hit testing; limits accept 1,024 widgets and reject 1,025 before attachment.
- [x] Confirm focused RED; implement host/adapter and layout using DCurses regions/scopes/geometry. Guard 4,096 queued actions and roll back mutations that would overflow; never refresh/read input implicitly.
- [x] Run both focused classes; record rollback/capture/registration counts and M1 partial evidence. Commit `feat: add widget host layout and lifecycle`.

## W1003 — basic form controls (M1 closure)

**Files:** create `src/Controls/{WidgetLabel,WidgetButton,WidgetTextInput,WidgetCheckBox,WidgetRadioGroup,WidgetBorder,WidgetScrollIndicator}.cs`,
`tests/Icod.Widgets.Tests/Controls/WidgetFormTests.cs`,
`samples/Icod.Widgets.Gallery.Sample/{Icod.Widgets.Gallery.Sample.csproj,Program.cs,README.md}`.

**Interfaces:** consumes W1002 host/actions/layout. Produces labeled form controls;
text `Value`/`MaxTextElements` (default 4,096); caller-supplied button command IDs;
typed value-change actions and pointer press/release behavior.

- [x] Add `UnicodeEditsWholeTextElements`, `PasteNormalizesControls`, `LimitDoesNotSplitTextElement`, `DisabledButtonCannotInvoke`, `PointerReleaseElsewhereDoesNotInvoke`, and `RadioSelectionMovesWithinGroup` with design-section-5 assertions.
- [x] Confirm RED; implement controls using public text layout/geometry. No setter executes application commands; each value transition emits once.
- [x] Extend the public gallery with forms, focus/keyboard hints and compact resize behavior. Add GPL notices to code/assets/project and set non-packability.
- [ ] Run form tests and all tests on net8/net9/net10; run gallery keyboard/mouse/resize acceptance. Commit `feat: add basic terminal form controls`; record M1 closure.

## W1004 — single/multiple list selection

**Files:** create `src/Controls/{WidgetListItem,WidgetList,WidgetSelectionMode}.cs`,
`tests/Icod.Widgets.Tests/Controls/WidgetListTests.cs`; modify gallery and candidate.

**Interfaces:** consumes W1002 actions and W1003 presentation. Produces
`WidgetList.SetItems(IReadOnlyList<WidgetListItem>)`, stable-key cursor/selection,
and immutable `WidgetSelectionChangedAction.Keys` in current item order.

- [x] Add `MoveCursorPreservesMultipleSelection`, `DisabledItemsCannotSelect`, `RangeUsesAnchor`, `ReplacementPreservesEnabledKeys`, `DuplicateKeysRejectBeforeMutation`, and `SearchExpiresAt750Milliseconds` (explicit app timestamps).
- [x] Add `VisibleRenderingDoesNotVisitWhole100000ItemList`: instrument rendering, scroll one row and assert visited content is limited to visible rows; 100,001 items reject without changing prior state.
- [x] Confirm RED; implement single commit, Space toggle, Ctrl+A, Shift range, Home/End/Page movement and ordinal case-insensitive wrapping search.
- [ ] Run list tests, update gallery multi-selection/search instructions, and commit `feat: add stable-key single and multiple selection lists`.

## W1005 — drop-down selection

**Files:** create `src/Controls/WidgetDropDown.cs`,
`tests/Icod.Widgets.Tests/Controls/WidgetDropDownTests.cs`; modify gallery.

**Interfaces:** consumes W1004 list behavior and W1002 scope/panel adapter.
Produces `WidgetDropDown` committed selection, popup open state and commit/cancel actions.

- [x] Add `CancelRestoresSelectionAndFocus`, `EnterCommitsOnce`, `OutsideDismissRestoresSelection`, and `PopupChoosesAvailableAboveOrBelowSpace` including zero-height space.
- [x] Confirm RED; implement saved-selection preview semantics and clipped/scrolled popup using the shared host overlay primitives.
- [ ] Run tests and gallery at top/bottom screen edges; commit `feat: add commit and cancel drop-down selection`.

## W1006 — menus and submenus

**Files:** create `src/Menus/{WidgetMenuItem,WidgetMenuBar,WidgetPopupMenu}.cs`,
`tests/Icod.Widgets.Tests/Menus/WidgetMenuTests.cs`; modify gallery.

**Interfaces:** consumes host overlays/list navigation. Produces command,
checkable, separator and submenu item kinds; `WidgetInvokedAction` and
`WidgetCheckedInvokedAction` exactly once per activation.

- [x] Add `DisabledAndSeparatorsAreInert`, `CheckableCommandReportsNewStateOnce`, `ShortcutRespectsActiveScope`, `DuplicateShortcutRejects`, and `SubmenuCloseRestoresParentFocus`.
- [x] Add `DeepMenuClipsOnTinyViewport`: 16 levels allowed, 17 rejected before mutation; Escape unwinds from compact/zero viewport; bounds never exceed arranged screen.
- [x] Confirm RED; implement bar/popup navigation, shortcut precedence and menu-chain close. Render/hit regions share clipped geometry.
- [ ] Run menu tests and edge-position gallery acceptance; commit `feat: add terminal menus and bounded submenus`.

## W1007 — dialogs, modal restoration and wizard shell (M2 closure)

**Files:** create `src/Dialogs/{WidgetDialog,WidgetDialogOutcome,WidgetDialogResult,WidgetMessageDialog,WidgetWizardDialog,WidgetModalLease}.cs`,
`tests/Icod.Widgets.Tests/Dialogs/WidgetDialogTests.cs`; modify host and gallery.

**Interfaces:** consumes W1002 overlay primitives and W1003 forms. Produces
`WidgetHost.ShowModal(WidgetDialog)`, modal leases, typed completion actions,
Yes/No and OK/Cancel factories, and wizard navigation requests.

- [x] Add `NoDiffersFromCancel`, `SafeDefaultActions`, `EscapeCancelsOnce`, `NestedCloseRestoresSurvivingFocus`, `ParentCloseCancelsDescendantsOnce`, and `ModalBlocksBackgroundShortcuts`.
- [x] Add `WizardValidationRejectionKeepsPage`: Previous disabled initially, Next requests transition, application rejection retains page/focus/error; test modal depth 16/17 boundary.
- [x] Confirm RED; implement wrapped content, LIFO leases, modeless results and application-applied wizard transitions. Host disposal remains silent cleanup.
- [ ] Run dialog/all-framework tests and every M2 gallery workflow; commit `feat: add dialogs and application-owned wizard navigation`; record M2 closure.

## W1008 — filesystem provider and common browser

**Files:** create `src/FileSystem/{IWidgetFileSystemProvider,WidgetFileSystemEntry,WidgetDirectorySnapshot,WidgetBrowserRequest,WidgetBrowserLoadResult,WidgetBrowserLoader,WidgetBrowserError,WidgetFileSelectionKind,WidgetFileSelectionValidation,LocalWidgetFileSystemProvider}.cs`,
`src/Pickers/WidgetBrowser.cs`,
`tests/Icod.Widgets.Tests/FileSystem/{WidgetBrowserTests,LocalProviderTests,FakeWidgetFileSystemProvider}.cs`.

**Interfaces:** consumes W1004 lists/W1007 overlays. Produces section-8 provider,
loader, request/application methods and immutable snapshot/error types; controller
identities/generations bridge async I/O to caller-owned UI loops.

- [x] Add `OutOfOrderResultsCannotReplaceLatest`, `ForeignBrowserResultRejected`, `DisposedBrowserIgnoresCompletion`, `PermissionFailureRetainsListing`, `RootsHaveNoParent`, and `TooManyEntriesHasExplicitError` at 100,001 entries.
- [ ] Add real temporary-directory tests for Unicode/spaces, platform case comparison, links where available and deletion; fake provider covers links/permission cases on every platform.
- [x] Confirm RED; implement local provider with System.IO, directory-first listing, absolute logical paths, root/parent metadata and no recursive scans. Catch only expected I/O errors; never mutate controls on a worker thread.
- [x] Run provider tests and record which real-OS cases were available. Commit `feat: add provider-backed terminal filesystem browser`.

## W1009 — folder picker

**Files:** create `src/Pickers/WidgetFolderPicker.cs`,
`tests/Icod.Widgets.Tests/Pickers/WidgetFolderPickerTests.cs`; modify gallery/provider candidate.

**Interfaces:** consumes W1008 browser/provider and typed dialog completion.
Produces current-folder selection yielding one absolute path only after matching
generation validation; parent/root/start-folder behavior.

- [x] Add `SelectCurrentFolderAtRoot`, `InvalidStartShowsRecoverableError`, `DeletedFolderCannotAccept`, `CancelDuringValidationCannotAccept`, and `FileEntryCannotBecomeFolderResult`.
- [x] Confirm RED; implement selection policy and caller-driven provider validation; retain dialog/error on failure and ignore stale success.
- [ ] Run picker tests and gallery folder navigation on Windows and Unix; commit `feat: add directory selection dialog`.

## W1010 — single/multiple file picker (M3 closure)

**Files:** create `src/Pickers/{WidgetFilePicker,WidgetFileFilter,WidgetFileSelectionBasket}.cs`,
`tests/Icod.Widgets.Tests/Pickers/WidgetFilePickerTests.cs`; modify gallery.

**Interfaces:** consumes W1008 provider comparison/validation and W1009 async
acceptance pattern. Produces existing-file single/multiple modes, named suffix
filters/all-files choice and immutable ordered accepted path payloads.

- [x] Add `SingleReturnsOneExistingFile`, `BasketSurvivesDirectoryAndFilterChanges`, `ComparerDeduplicatesPaths`, `FilterNeverHidesDirectories`, `ReturnedPathsDoNotAliasBasket`, and `EmptyBasketCannotAccept`.
- [x] Add `DeletedSelectionKeepsDialogOpen` and `StaleValidationCannotAcceptAfterNavigationOrCancel`: report failed paths and preserve explicit basket state.
- [x] Confirm RED; implement provider-case suffix filtering, ordered basket toggling/removal and matching-generation validation before Accepted.
- [ ] Run file picker/all-framework tests and real-OS gallery workflows; commit `feat: add single and multiple file selection dialogs`; record M3 closure.

## W1011 — roguelike conveniences

**Files:** create `src/GameUi/{WidgetInventoryList,WidgetActionList,WidgetMessageHistory,WidgetStatusBar,WidgetHelpOverlay,WidgetTextPrompt,WidgetNumberPrompt}.cs`,
`tests/Icod.Widgets.Tests/GameUi/WidgetGameUiTests.cs`; modify gallery.

**Interfaces:** consumes lists, dialogs, input, theme and binding discovery.
Produces domain-neutral caller-item/command wrappers, bounded messages (default
1,000), prioritized status fields, caller-labeled help and validated prompts.

- [x] Add `InventoryHasNoGameModelDependency`, `HistoryDropsOldestAt1001`, `StatusFitsCompactScreen`, `HelpReflectsEffectiveBindings`, and `NumberValidationUsesCallerCultureAndRange`.
- [x] Confirm RED; compose existing behaviors, retaining caller-owned data/validation. No actor, rules, map-generator or save-game types enter the DLL.
- [x] Run game-UI tests and gallery examples; commit `feat: add reusable inventory status help and prompt controls`.

## W1012 — application and package-only qualification

**Files:** create `samples/Icod.Widgets.Editor.Sample/{Icod.Widgets.Editor.Sample.csproj,Program.cs,README.md}`,
`samples/Icod.Widgets.Roguelike.Sample/{Icod.Widgets.Roguelike.Sample.csproj,Program.cs,README.md}`,
`tests/PackageConsumer/{PackageConsumer.csproj,Program.cs}`,
`tests/Icod.Widgets.Tests/Acceptance/WidgetConsumerTests.cs`, `samples/README.md`; finish gallery.

**Interfaces:** consumes the complete public candidate. Produces runnable
gallery, editor open-file and text roguelike inventory/help witnesses and a
consumer referencing the packed `.nupkg` only.

- [x] Add public input-replay acceptance assertions for each sample: cancellation leaves model unchanged; file open is application-owned; inventory action reaches caller once; help close restores focus.
- [x] Build sample loops with explicit Process/actions/Render/Refresh sequencing. Expose a repeatable `--smoke` path and documented Q/Escape exits. If adding raster demonstration, explicitly choose text during immediate-raster overlays.
- [x] Pack Release; restore/run PackageConsumer from `artifacts/packages` with no ProjectReference to the library; record exact package version/hash.
- [x] Run `dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0 -- --smoke` and corresponding editor/roguelike commands; expected exit 0 with named smoke checks passed. Run live workflows separately. Commit `test: qualify Widgets through public application consumers`.

## W1013 — hardening, API/docs/package freeze and RC (M4 closure)

**Files:** create `tests/Icod.Widgets.Tests/Qualification/{WidgetLifecycleTests,WidgetCapacityTests,WidgetPackageTests}.cs`,
`docs/Public-API-Baseline-1.0.md`, `docs/Public-API-Fingerprint-1.0.json`,
`docs/1.0-Release-Readiness.md`; modify README, CHANGELOG, sample docs, project versions.

**Interfaces:** consumes implemented candidate/evidence. Produces the frozen
contract, qualified package boundary and RC source, not publication.

- [x] Add repeated open/close/dispose/capture tests, action-queue limit/failure atomicity, 100,000-item work measurements, and stale provider tests after host shutdown. Confirm leaks/invalid focus are caught before fixing any failures.
- [x] Inspect source headers, explicit compile items, nuspec license/direct dependencies, package payload and public XML docs. Freeze fingerprint from built assembly; verify the package-only consumer against that same package.
- [x] Expand man-page-style README and full CHANGELOG; ensure every required widget has examples/keys/errors/limitations. Remove planning-only install status only when a usable package actually exists.
- [ ] Run full Staging tests across frameworks and Release pack/validate on all CI hosts; record evidence. Advance to `1.0.0-rc.N` only after M1–M4 automated/sample gates. Commit `docs: freeze Widgets API package and release candidate`.

## W1014 — live/stable-source readiness

**Files:** modify `docs/1.0-Release-Readiness.md`, README, CHANGELOG, ROADMAP,
development roadmap and package/assembly versions; add `docs/W1014-Stable-Qualification.md`.

**Interfaces:** consumes exact RC head and all tranche evidence. Produces a
reviewable stable-source qualification record and curated release notes.

- [ ] Record live Windows/macOS/Linux results for every required control: terminal/OS/architecture/runtime/head, keyboard/pointer, resize, Unicode, modal restoration, filesystem failure, clean exits. Include x64/ARM64 witnesses where applicable; missing live evidence stays explicitly unqualified.
- [ ] Resolve reproducible failures in their owning component and rerun affected acceptance. Do not replace missing operator evidence with synthetic test claims.
- [ ] Set exact stable `1.0.0` / assembly `1.0.0.0` only when gates pass; rerun full CI and pack/validate/package-only consumer at the resulting head. Verify curated notes, README and CHANGELOG agree.
- [ ] Commit `release: qualify Widgets 1.0.0 source`; report readiness with remaining limitations. Tag/publish only under a subsequent explicit release instruction. Verify published package separately after publication.

## Coverage and handoff

Design sections 1–4 map to W1001–W1002; section 5 to W1002–W1003; section 6 to
W1004–W1006; section 7 to W1007; section 8 to W1008–W1010; section 9 to
W1011–W1012; section 10 to W1013–W1014. Every review-focus case has named tests.

This plan is for review. Implementation starts after the user approves the
written design/development plan. Execution is sequential by the primary agent
unless the user explicitly chooses delegation; no parallel agents are implied.
