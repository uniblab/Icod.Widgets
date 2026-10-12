# Icod.Widgets 1.2 Dialogs and Save Targets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Execute natively without subagents.

**Goal:** Add child-composable dialogs and a separate, non-writing save-target picker without weakening existing dialogs or existing-file selection.

**Architecture:** Existing `WidgetDialog` gains child composition through `WidgetContainer` while the host routes a modal's input to its scoped children. A general form dialog owns buttons/default/cancel and generation-guarded validation. The save picker adds a separate inspection capability and emits a proposed target, leaving every write and overwrite decision to the application.

**Tech Stack:** C# 13, net8.0/net9.0/net10.0, Icod.DCurses 2.4.0, xUnit.

**Spec:** [1.2 Document Workspace design](../specs/2026-10-12-icod-widgets-1.2-document-workspace-design.md)

## Global Constraints

- Package `1.2.0`; assembly `1.0.0.0`; sole direct runtime dependency DCurses `2.4.0`.
- Preserve existing message/wizard/folder/file picker constructors and `IWidgetFileSystemProvider` implementations.
- Escape always completes `Cancel`, including OK-only and Save/Don't Save/Cancel dialogs.
- Library never opens, creates, truncates, deletes, or writes a proposed file; no overwrite guarantee from validation.
- LGPL-3.0-or-later library, GPL-3.0-or-later tests, full prescribed headers.

## Review Focus

- Child widget ID collides with an attached background widget: modal attach fails before changing focus (Task 1).
- Nested modal closes while a child has pointer capture or pending clipboard: no background gesture/result leaks (Task 1).
- Enter during pending validation: cannot complete twice or bypass a disabled default (Task 2).
- Save target name changes while inspection is in flight: old result is ignored (Task 3).
- Existing-file provider lacks save capability: picker reports unsupported, never assumes path syntax or writes (Task 3).

---

## File map and public contracts

Modify `src/Dialogs/WidgetDialog.cs` to inherit `WidgetContainer` while preserving its existing constructor and frame behavior. Modify host modal input/render paths to focus/render descendants on the dialog panel, clip descendants to the content bounds, and block background interaction. Existing dialogs with no children remain unchanged.

Create `src/Dialogs/WidgetFormDialog.cs` and `src/Dialogs/WidgetDialogAction.cs`. `WidgetDialogAction` is `public sealed record WidgetDialogAction(string Id, string Label, WidgetDialogOutcome Outcome)` with init properties `IsDefault`, `IsCancel`, `RequiresValidation`, `IsEnabled = true`, `Value`. `WidgetFormDialog(string id, string title, IReadOnlyList<WidgetDialogAction> actions, string? initialFocusId = null)` owns a set of `Widget` children inherited from `WidgetContainer`; `SetActionEnabled(string actionId, bool enabled)` updates state. Add `WidgetDialogOutcome.Save` and `DontSave` at the end of the enum (do not renumber older values). `WidgetFormDialog.SaveDontSaveCancel(string id, string title, string message)` is a convenience factory with safe default Cancel and three distinct outcomes.

Create `WidgetDialogValidationRequest(Guid DialogId, long Generation, string ActionId)` and `WidgetDialogValidationResult(WidgetDialogValidationRequest Request, bool IsValid, string? Error = null)` in `src/Dialogs/WidgetDialogValidation.cs`; add `WidgetDialogValidationRequestedAction(string WidgetId, WidgetDialogValidationRequest Request)` in `src/Core/WidgetAction.cs`. `WidgetFormDialog.ApplyValidationResult(...)` accepts only the latest generation for the open dialog. Closed dialogs reject all late results.

Create `src/FileSystem/IWidgetSaveTargetProvider.cs` with `ValueTask<WidgetSaveTargetObservation> InspectTargetAsync(string directory, string fileName, CancellationToken token)`; `WidgetSaveTargetObservation(string TargetPath, bool Exists, bool IsValid, string? Error = null)` is an observation, not an authorization to overwrite. Create `src/Pickers/WidgetSaveTargetPicker.cs`, request/result/selection types in `src/Pickers/WidgetSaveTargetRequest.cs`, and a typed inspection-request action. Constructor: `WidgetSaveTargetPicker(string id, IWidgetFileSystemProvider browserProvider, IWidgetSaveTargetProvider? targetProvider, string startLocation, string suggestedName = "")`. Explicit null capability leaves Accept unavailable with an explanatory error; the caller may still cancel. `RequestAccept()` and `ApplyInspectionResult(...)` follow existing picker generation conventions.

### Task 1: W1206 modal child composition

**Files:** Modify `src/Dialogs/WidgetDialog.cs`, `src/Core/WidgetHost.cs`, `src/Core/WidgetHost.Modals.cs`, `src/Core/WidgetHost.Presentation.cs`; create `tests/Icod.Widgets.Tests/Dialogs/WidgetComposableDialogTests.cs`, `docs/W1206-Composable-Dialogs.md`.

**Interfaces:** `WidgetDialog : WidgetContainer`; inherited `Add(Widget)` and `Children` become public composition. Host routes pointer/key/focus within active modal descendants, with the dialog itself handling Escape/default only after eligible child routing. Existing dialogs continue to operate with zero children.

- [ ] **Step 1: Write failing tests.** `ModalChildReceivesTextAndTabStaysInScope` checks text input/focus; `ChildRendersWithinFrameAndClipsAtTinyBounds` checks panel cells; `DuplicateChildIdRejectsBeforeFocusChange`; `NestedCloseCancelsChildCaptureAndClipboard`; `OldMessageAndWizardBehaviorUnchanged`. Add a package-only custom dialog child compile test.

  ```csharp
  dialog.Add( new WidgetTextInput( "name" ) );
  host.ShowModal( dialog );
  Assert.Equal( "x", Assert.IsType<WidgetTextInput>( dialog.Children.Single() ).Value );
  Assert.NotSame( background, host.FocusedWidget );
  ```
- [ ] **Step 2: Verify red.** Run filtered `WidgetComposableDialogTests`; expect inability to add/render/focus a child.
- [ ] **Step 3: Implement.** Change the base inheritance without altering public constructor; attach descendants with the dialog scope/panel, arrange inside `GetContentBounds`, route input to eligible focused/hit descendant, render descendants on dialog panel after frame, and retain Escape handling on the dialog. Reuse host validation/capture invalidation; do not expose panel or modal stack publicly.
- [ ] **Step 4: Verify green.** Run filtered and full net10 tests; expect zero failures, including old message/picker/wizard tests. Record W1206 evidence.
- [ ] **Step 5: Commit.** Stage named files and `git commit -m "feat: compose controls in scoped dialogs"`.

### Task 2: W1206 form actions and decision outcomes

**Files:** Create `src/Dialogs/WidgetFormDialog.cs`, `src/Dialogs/WidgetDialogAction.cs`, `src/Dialogs/WidgetDialogValidation.cs`, `tests/Icod.Widgets.Tests/Dialogs/WidgetFormDialogTests.cs`; modify `src/Dialogs/WidgetDialogResult.cs`, `src/Core/WidgetAction.cs`; update `docs/W1206-Composable-Dialogs.md`.

**Interfaces:** Produces the exact form/action/validation contracts in the file map. `WidgetFormDialog.ApplyValidationResult(WidgetDialogValidationResult result)` returns bool; its completed result is existing `WidgetDialogCompletedAction` with the selected action's `Outcome` and `Value`.

- [ ] **Step 1: Write failing tests.** `EscapeCancelsOkOnlyAndDecision` asserts Cancel; `SaveDontSaveCancelAreDistinct` asserts all three outcomes without file side effects; `DisabledDefaultCannotComplete`; `ValidationRejectThenAcceptsLatestOnly` checks dialog ID/generation, error and one completion; `CompactDialogRetainsKeyboardCancel`; `InvalidDuplicateActionsRejectBeforeOpen`. Assert initial focus resolves only to a visible enabled descendant.

  ```csharp
  WidgetFormDialog dialog = WidgetFormDialog.SaveDontSaveCancel( "close", "Close", "Save changes?" );
  Assert.Equal( WidgetDialogOutcome.Cancel, Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( escape.Actions ) ).Result.Outcome );
  Assert.False( dialog.ApplyValidationResult( staleResult ) );
  ```
- [ ] **Step 2: Verify red.** Run filtered `WidgetFormDialogTests`; expect missing types/factory.
- [ ] **Step 3: Implement.** Validate action IDs, exactly one default and at most one explicit cancel; Escape always completes Cancel even without a visible Cancel button. Use a per-instance GUID and checked generation increments on submit, edits that invalidate pending state, and close. A validation-required action emits a request rather than completing; `ApplyValidationResult` completes only for matching latest request. Disabled actions cannot activate by key or pointer. Layout actions within clipped frame.
- [ ] **Step 4: Verify green.** Run filtered/full net10 tests and gallery `--smoke`; expect zero failures. Update W1206 evidence.
- [ ] **Step 5: Commit.** Stage named files and `git commit -m "feat: add validated form and save decision dialogs"`.

### Task 3: W1207 save-target inspection and picker

**Files:** Create `src/FileSystem/IWidgetSaveTargetProvider.cs`, `src/Pickers/WidgetSaveTargetRequest.cs`, `src/Pickers/WidgetSaveTargetPicker.cs`, `tests/Icod.Widgets.Tests/Pickers/WidgetSaveTargetPickerTests.cs`, `docs/W1207-Save-Target-Picker.md`; modify `src/Core/WidgetAction.cs`; add an application-owned local inspection adapter only to the new workspace sample in W1209.

**Interfaces:** `WidgetSaveTargetRequest(Guid PickerId, long Generation, string Directory, string FileName)`; `WidgetSaveTargetInspectionResult(WidgetSaveTargetRequest Request, WidgetSaveTargetObservation Observation)`; `WidgetSaveTargetSelection(string TargetPath, bool ExistedAtInspection)`; `WidgetSaveTargetInspectionRequestedAction(string WidgetId, WidgetSaveTargetRequest Request)`. `WidgetSaveTargetPicker.RequestDirectory(string location)`, `ApplyLoadResult(WidgetBrowserLoadResult result)`, `RequestAccept()`, `ApplyInspectionResult(WidgetSaveTargetInspectionResult result)` mirror existing picker conventions.

- [ ] **Step 1: Write failing tests.** `NewAndExistingTargetsReturnObservationsNotWrites` uses a fake provider that counts calls and asserts no write API; `NameChangeInvalidatesOldInspection`, `NavigationAndCloseInvalidatePending`, `UnsupportedProviderReportsExplicitError`, `ProviderFailureStaysApplicationOwned`, `ExistingFilePickerAndThirdPartyProviderStillCompile`, and `CancelAlwaysReturnsCancel`. Include invalid blank/overlong names and path comparer case variants.

  ```csharp
  WidgetSaveTargetRequest request = picker.RequestAccept();
  Assert.False( picker.ApplyInspectionResult( oldResultAfterNameChange ) );
  Assert.Equal( 0, fakeProvider.WriteCount );
  Assert.Equal( "draft.txt", request.FileName );
  ```
- [ ] **Step 2: Verify red.** Run filtered `WidgetSaveTargetPickerTests`; expect missing type/interface.
- [ ] **Step 3: Implement.** Keep `IWidgetFileSystemProvider` unchanged. Compose browser listing and a `WidgetTextInput` target name inside the dialog. Use the separate optional capability to compose/inspect path; neither widget nor library default provider constructs a target from untrusted syntax. Emit request on Accept; verify GUID/generation/current directory/current name before completing with `WidgetSaveTargetSelection`. Show invalid observation errors and keep dialog open. No filesystem write methods are added.
- [ ] **Step 4: Verify green.** Run filtered/full net10 tests plus existing picker tests; expect zero failures. Record W1207 evidence and race caveat.
- [ ] **Step 5: Commit.** Stage named files and `git commit -m "feat: select inspected save targets without writes"`.

## Handoff

Continue with [search forms](2026-10-12-icod-widgets-1.2-search.md). The application must recheck the target when it writes; an `Exists` observation is never an overwrite grant.
