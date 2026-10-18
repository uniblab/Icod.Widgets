# Generic Dialog Actions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Completed steps use checked (`- [x]`) task markers.

**Status:** Complete as of 2026-10-18. Implemented and qualified in
[PR #18](https://github.com/uniblab/Icod.Widgets/pull/18); intentionally left
unmerged for owner review and publication.

**Goal:** Release Icod.Widgets 1.8.0 with generic action-array message dialogs, independent captions/outcomes/mnemonics/defaults/cancellation, and presentation-only per-dialog styles.

**Architecture:** `WidgetDialogAction` remains the public button description. A new internal `WidgetDialogActionSet` validates and owns copied action state shared by message and form dialogs; each dialog keeps its own completion/validation policy while routing keyboard, mnemonic and pointer activation through one action procedure. Message rendering measures the same bracketed labels used for hit testing and overlays mnemonic emphasis without changing caller styles.

**Tech Stack:** C# 13, .NET 8/9/10, Icod.DCurses 2.5.2, xUnit, PowerShell 5.1-compatible packaging tools.

**Spec:** `docs/superpowers/specs/2026-10-18-generic-dialog-actions-design.md`

## Global Constraints

- Preserve `AssemblyVersion` `1.0.0.0`, AnyCPU and net8.0/net9.0/net10.0.
- Version and package version become exactly `1.8.0`; Icod.DCurses remains exactly `2.5.2`.
- `WidgetDialogAction.Label` controls presentation, `Outcome`/`Value` control the result, `Mnemonic` controls the shortcut, and `IsDefault`/`IsCancel` control interaction.
- Style means presentation only; no style may change default/cancel selection, mnemonics, navigation, hit testing, results or application policy.
- Retain the legacy `YesNo(string, string, string)` signature and behavior, mark it warning-level obsolete using the exact approved formatting, and do not add another YesNo-specific factory.
- Every action renders as `[Label]`; all width, truncation and hit testing use terminal display columns.
- Plain case-insensitive mnemonics activate; modified input and key releases do not.
- Escape activates the declared cancel action or returns synthetic Cancel when none exists.
- README stays concise and version-neutral; CHANGELOG and `docs/releases/1.8.0.md` carry release history.
- Do not merge, tag or publish.

## Review Focus

- A wide or combining Unicode caption must produce the same rendered columns and pointer hit region; Task 2 adds `UnicodeCaptionUsesDisplayColumnsForRenderAndHitTest`.
- Text and key representations of one physical mnemonic must not double-complete, and releases must not activate; Task 2 adds `MnemonicCompletesOnceForTextOrPressedKeyAndIgnoresRelease`.
- Disabling the selected/default action must move to an enabled action while disabling the last enabled action fails without corrupting state; Task 1 adds `DisablingSelectionAdvancesAndFinalEnabledActionIsRequired`.
- A caption truncated by a narrow viewport must not move another action's hit region or bind a hidden glyph; Task 2 adds `NarrowDialogTruncatesWithoutChangingActionIdentity`.
- Pointer capture loss or a press/release across different buttons must not activate either action; Task 2 extends the pointer regression with both cases.

---

### Task 1: Shared action contract and compatibility metadata

**Files:**
- Create: `src/Dialogs/WidgetDialogActionSet.cs`
- Modify: `src/Dialogs/WidgetDialogAction.cs`
- Modify: `src/Dialogs/WidgetMessageDialog.cs`
- Test: `tests/Icod.Widgets.Tests/Dialogs/WidgetDialogActionTests.cs`
- Test: `tests/Icod.Widgets.Tests/Dialogs/WidgetDialogTests.cs`

**Interfaces:**
- Consumes: existing `WidgetDialogAction`, `WidgetDialogOutcome`, `WidgetInput` and `WidgetLimits.MaximumItems`.
- Produces: `WidgetDialogAction.Mnemonic`; public `WidgetMessageDialog(string id, string title, string message, IReadOnlyList<WidgetDialogAction> actions)`; public `IReadOnlyList<WidgetDialogAction> Actions`; public `void SetActionEnabled(string actionId, bool enabled)`; internal `WidgetDialogActionSet` selection and mnemonic services used by Tasks 2–3.

- [x] **Step 1: Write failing public-contract tests.** Add `GenericConstructorCopiesAndExposesReadOnlyActions`, `CompletesWithConfiguredOutcomeAndValue`, `RejectsInvalidActionArrays`, `DisablingSelectionAdvancesAndFinalEnabledActionIsRequired`, and `LegacyYesNoIsWarningObsoleteAndBehaviorCompatible`. Assert the exact constructor/property/method signatures, copied storage, unique IDs and case-insensitive mnemonics, exactly one enabled default, at most one cancel, forbidden message validation, legacy Yes/No/No-default/Escape behavior, and the exact `ObsoleteAttribute.Message` with `IsError == false`.

- [x] **Step 2: Run the focused tests and verify RED.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Debug -f net10.0 --filter "FullyQualifiedName~WidgetDialogActionTests|FullyQualifiedName~WidgetDialogTests"`. Expected: new tests fail because `Mnemonic`, the constructor, `Actions`, `SetActionEnabled` and obsolete metadata do not exist.

- [x] **Step 3: Add the public mnemonic and internal action set.** Add `public char? Mnemonic { get; init; }` to `WidgetDialogAction`. Implement `WidgetDialogActionSet(IReadOnlyList<WidgetDialogAction> actions, bool allowValidation)` with copied read-only actions, `int SelectedIndex`, `WidgetDialogAction Selected`, `WidgetDialogAction? CancelAction`, `void SetEnabled(string actionId, bool enabled)`, `bool Move(int direction, bool wrap)` and `bool TryGetMnemonic(WidgetInput input, out WidgetDialogAction action)`. Enforce every invariant from the spec before attachment.

- [x] **Step 4: Add the message-dialog public surface and legacy metadata.** Replace the private tuple storage with `WidgetDialogActionSet`; add the exact constructor, `Actions` and `SetActionEnabled`; rebuild `Ok`, `OkCancel` and legacy `YesNo` with actions. Apply exactly:

```csharp
[Obsolete(
	"Use the WidgetMessageDialog action-array constructor so captions, "
	+ "mnemonics, default, and cancel behavior are explicit."
	, false
)]
```

- [x] **Step 5: Run the focused tests and verify GREEN.** Repeat the Step 2 command. Expected: all selected tests pass with zero warnings/errors from production; legacy-use test suppresses CS0618 only around the compatibility call.

- [x] **Step 6: Commit.** Run `git add src/Dialogs/WidgetDialogAction.cs src/Dialogs/WidgetDialogActionSet.cs src/Dialogs/WidgetMessageDialog.cs tests/Icod.Widgets.Tests/Dialogs/WidgetDialogActionTests.cs tests/Icod.Widgets.Tests/Dialogs/WidgetDialogTests.cs && git commit -m "feat: add generic dialog action arrays"`.

### Task 2: Message-dialog keyboard, pointer and presentation behavior

**Files:**
- Modify: `src/Dialogs/WidgetMessageDialog.cs`
- Test: `tests/Icod.Widgets.Tests/Dialogs/WidgetDialogTests.cs`
- Test: `tests/Icod.Widgets.Tests/Dialogs/WidgetMessageDialogPointerTests.cs`
- Test: `tests/Icod.Widgets.Tests/Core/WidgetColorThemeTests.cs`

**Interfaces:**
- Consumes: Task 1 `WidgetDialogActionSet`, `Actions` and `SetActionEnabled`.
- Produces: one action activation path for Enter, Escape, mnemonic and pointer; bracketed display-column-correct buttons; caller-style-preserving mnemonic emphasis.

- [x] **Step 1: Write failing interaction tests.** Add `MnemonicActivatesConfiguredOutcomeCaseInsensitively`, `ModifiedMnemonicDoesNotActivate`, `MnemonicCompletesOnceForTextOrPressedKeyAndIgnoresRelease`, `EscapeUsesDeclaredCancelActionAndValue`, `ArrowsAndTabSkipDisabledActions`, and `EnterUsesExplicitDefault`. Use Yes/Don't Save/Cancel with Y/N/C and Cancel default.

- [x] **Step 2: Write failing rendering and pointer tests.** Add `AllActionsRenderAsBracketedButtons`, `MnemonicUnderlinesFirstCaptionMatchWithoutChangingColors`, `CustomStylesReachEveryDialogPartWithoutChangingBehavior`, `UnicodeCaptionUsesDisplayColumnsForRenderAndHitTest`, `NarrowDialogTruncatesWithoutChangingActionIdentity`, and pointer cases for same-action activation, cross-action release, disabled actions, drag-away and capture loss. Assert the completion outcome/value, not merely closure; cover surface/text/title/border/action styles with an instance-local override.

- [x] **Step 3: Run tests and verify RED.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Debug -f net10.0 --filter "FullyQualifiedName~WidgetDialogTests|FullyQualifiedName~WidgetMessageDialogPointerTests|FullyQualifiedName~WidgetColorThemeTests"`. Expected: mnemonic, all-bracket, Unicode hit testing and declared-cancel cases fail against the legacy tuple renderer/input path.

- [x] **Step 4: Implement unified activation.** In `WidgetMessageDialog`, resolve Enter, Escape, mnemonic and pointer release to a `WidgetDialogAction` and pass it to `Activate(WidgetHost host, WidgetDialogAction action)`. That method checks enabled state, reserves one action, and finishes once with `new WidgetDialogResult(action.Outcome, action.Value)`. Synthetic Escape completion remains only when there is no cancel action.

- [x] **Step 5: Implement measured button presentation.** Render every sanitized caption as `[Label]`, derive each column/width and pointer region from `CursesText.MeasureColumns`, and reuse those measurements for hit testing. Overlay underline on the first case-insensitive matching caption text element while preserving the resolved foreground, background and other attributes. Truncation must never change semantic indexes.

- [x] **Step 6: Run focused and full dialog tests.** Repeat Step 3, then run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Debug -f net10.0 --filter "FullyQualifiedName~Dialogs"`. Expected: all selected tests pass with zero failures.

- [x] **Step 7: Commit.** Run `git add src/Dialogs/WidgetMessageDialog.cs tests/Icod.Widgets.Tests/Dialogs/WidgetDialogTests.cs tests/Icod.Widgets.Tests/Dialogs/WidgetMessageDialogPointerTests.cs tests/Icod.Widgets.Tests/Core/WidgetColorThemeTests.cs && git commit -m "feat: route generic dialog actions"`.

### Task 3: Form-dialog mnemonic parity

**Files:**
- Modify: `src/Dialogs/WidgetFormDialog.cs`
- Test: `tests/Icod.Widgets.Tests/Dialogs/WidgetFormDialogTests.cs`

**Interfaces:**
- Consumes: Task 1 `WidgetDialogActionSet` and Task 2 mnemonic/activation rules.
- Produces: form actions with the same mnemonic, enabled-selection, Escape and display-column button behavior while retaining generation-safe validation.

- [x] **Step 1: Write failing tests.** Add `MnemonicUsesSameActionPathAsEnter`, `ValidationMnemonicEmitsOneCurrentRequest`, `FormButtonsRenderBracketedUnicodeCaptions`, and `EscapeActivatesDeclaredCancelAction`. Assert a mnemonic on a validation action emits a request rather than completing, stale validation remains rejected, and pointer/Enter/mnemonic produce the same nonvalidation result.

- [x] **Step 2: Run and verify RED.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Debug -f net10.0 --filter FullyQualifiedName~WidgetFormDialogTests`. Expected: mnemonic and configured-cancel cases fail.

- [x] **Step 3: Adopt the shared action set.** Replace form-local array/selection mutation with `WidgetDialogActionSet`; route Enter, Escape, mnemonic and pointer to the existing validation-aware `Activate`; use display-column measurement for bracketed rendering and hit testing. Preserve `SetActionEnabled`, pending-generation invalidation, initial child focus and validation results.

- [x] **Step 4: Run focused and all dialog tests.** Repeat Step 2, then run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Debug -f net10.0 --filter FullyQualifiedName~Dialogs`. Expected: all dialog tests pass.

- [x] **Step 5: Commit.** Run `git add src/Dialogs/WidgetFormDialog.cs tests/Icod.Widgets.Tests/Dialogs/WidgetFormDialogTests.cs && git commit -m "feat: add form action mnemonics"`.

### Task 4: Gallery, public API record and 1.8.0 release identity

**Files:**
- Modify: `samples/Icod.Widgets.Gallery.Sample/Program.cs`
- Modify: `samples/Icod.Widgets.Gallery.Sample/README.md`
- Modify: `samples/README.md`
- Modify: `docs/Controls-and-Interaction.md`
- Modify: `docs/Public-API-Candidate-1.0.md`
- Create: `docs/Public-API-Baseline-1.8.md`
- Create: `docs/Public-API-Fingerprint-1.8.json`
- Create: `packaging/Get-PublicApiFingerprint.ps1`
- Modify: `tests/PackageConsumer/Program.cs`
- Modify: `tests/PackageConsumer/PackageConsumer.csproj`
- Modify: `tests/Icod.Widgets.Tests/Core/ReleaseIdentityTests.cs`
- Modify: `Icod.Widgets.csproj`
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Create: `docs/releases/1.8.0.md`

**Interfaces:**
- Consumes: Tasks 1–3 public constructor, action properties and interaction behavior.
- Produces: a public sample/migration example, reproducible API snapshot, exact 1.8.0 package identity and curated release documentation for Dedit to consume.

- [x] **Step 1: Write failing release/sample contracts.** Update `ReleaseIdentityTests` to require version/package `1.8.0`, consumer `1.8.0`, curated `1.8.0.md`, action-array/mnemonic/default/cancel/style wording, README version-neutral guidance, and the new fingerprint/baseline. Update gallery smoke expectations to open the generic Yes/Don't Save/Cancel dialog and exercise N, Cancel-default Enter, and a pointer choice.

- [x] **Step 2: Run and verify RED.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Debug -f net10.0 --filter "FullyQualifiedName~ReleaseIdentityTests|FullyQualifiedName~WidgetLivePathTests"`. Expected: 1.7.2 identity and old gallery behavior fail the new assertions.

- [x] **Step 3: Update gallery and consumer.** Replace in-repository legacy `YesNo` calls with action-array construction so Staging warnings remain clean. Demonstrate custom captions, Y/N/C, Cancel default and instance-only dialog `Styles`; keep style presentation-only. Update the package consumer to compile and run the new API.

- [x] **Step 4: Add reproducible API snapshot tooling.** Create PS 5.1-compatible `Get-PublicApiFingerprint.ps1` that loads each built target assembly, records sorted public declared type/member signatures in the established JSON shape, and writes `docs/Public-API-Fingerprint-1.8.json`. Build Release first, run the script in generation mode, and document that exact command plus the additive compatibility result in `Public-API-Baseline-1.8.md`; include `Mnemonic`, the constructor, `Actions`, `SetActionEnabled` and obsolete metadata expectations.

- [x] **Step 5: Update version and curated documentation.** Set `Version`/`PackageVersion` and package consumer to `1.8.0`; retain assembly `1.0.0.0` and DCurses `2.5.2`. Update the concise README/API guide, samples, candidate surface, CHANGELOG, package release notes and curated notes. Explain the obsolete two-button factory and generic migration without using release history as README content.

- [x] **Step 6: Run sample, API and full local validation.** Run `./build.sh`, then `pwsh -NoProfile -File packaging/Invoke-Build.ps1 -Section build -Configuration Release`, `dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0 --no-build -- --smoke`, and `pwsh -NoProfile -File packaging/Get-PublicApiFingerprint.ps1 -Configuration Release -Verify`. Expected: net8/9/10 tests, pack, package verification and package consumer pass; the Release build has zero warnings/errors; gallery PASS; fingerprint exact.

- [x] **Step 7: Commit.** Run `git add Icod.Widgets.csproj README.md CHANGELOG.md src tests samples docs packaging/Get-PublicApiFingerprint.ps1 && git commit -m "docs: prepare Widgets 1.8.0"`.

### Task 5: Remote qualification and owner handoff

**Files:**
- Modify: `docs/Public-API-Baseline-1.8.md` only if final measured evidence differs from the candidate record.

**Interfaces:**
- Consumes: Tasks 1–4 complete candidate.
- Produces: an open, unmerged Icod.Widgets 1.8.0 PR with three-OS evidence suitable for owner review and publication.

- [x] **Step 1: Push and open the PR.** Push `feature/1.8.0-generic-dialog-actions`, create a PR against `main`, and summarize generic actions, obsolete migration, presentation-only styles, gallery coverage and explicit non-merge status.

- [x] **Step 2: Require the full CI matrix.** Verify Linux, Windows and macOS restore/build/test/pack/package-consumer jobs complete successfully for net8.0/net9.0/net10.0 with zero warnings/errors. Inspect logs rather than relying only on job conclusions.

- [x] **Step 3: Run a whole-branch review.** Review from the merge base through the PR head with focus on binary compatibility, duplicate input completion, Unicode width/hit testing, disabled/default state, style/behavior separation and documentation accuracy. Fix Critical/Important findings test-first; record Minor findings for owner review.

- [x] **Step 4: Re-run final CI and report.** Require a green final-head matrix, refresh the PR validation section with exact run/test/package evidence, and stop for owner merge/publish approval. Do not merge, tag or publish.

