# Icod.Widgets 1.2 Workspace Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. The owner chose native execution without subagents.

**Goal:** Add a reusable tab strip and replaceable active-content host without transferring document lifetime or file policy to Widgets.

**Architecture:** The tab strip accepts an ordered stable-key snapshot and emits activation/close requests. A separate content host swaps caller-owned editor widgets, detaching old content to invalidate capture and clipboard while preserving each view object. No mutable editor rebinding.

**Tech Stack:** C# 13, net8.0/net9.0/net10.0, Icod.DCurses 2.4.0, xUnit.

**Spec:** [1.2 Document Workspace design](../specs/2026-10-12-icod-widgets-1.2-document-workspace-design.md)

## Global Constraints

- Package `1.2.0`; assembly identity `1.0.0.0`; sole direct runtime dependency `Icod.DCurses 2.4.0`.
- Preserve 1.0/1.1 public API, menu factories, dialog results, and existing providers.
- No library file writes, input reader, hidden worker/timer, save/close policy, or disposal of caller-owned documents/views.
- Preserve LGPL-3.0-or-later library and GPL-3.0-or-later tests/samples with full prescribed headers.
- All mutations and completion application occur on the caller's application loop.

## Review Focus

- Duplicate tab keys in a replacement snapshot: reject before changing the visible set (Task 2).
- Active tab removed while overflowing: choose a deterministic surviving key and keep it visible (Task 2).
- Pointer release after content replacement: cannot activate or select in the new editor (Task 3).
- Late paste/clipboard result from a detached editor: cannot edit its replacement (Task 3).
- Host at 1,024 widgets when replacing content: failed attach leaves the previous content and focus intact (Task 3).

---

## File map and public contracts

Create `src/Workspace/WidgetTab.cs` for `public sealed record WidgetTab(string Key, string Caption, bool IsModified = false, bool IsEnabled = true, bool IsClosable = true)`. Create `src/Workspace/WidgetTabStrip.cs` for `public sealed class WidgetTabStrip : Widget`, with `SetTabs(IReadOnlyList<WidgetTab> tabs, string? activeKey = null)`, `Tabs`, `ActiveKey`, and `VisibleKeys`. Null active key means preserve the old key if it survives, otherwise choose the next item at its old index (or final previous item); an initially nonempty list chooses its first enabled item. An explicit unknown/disabled active key is rejected. Empty or wholly disabled lists have null active key. Bounded maximum is `WidgetLimits.MaximumTabs = 1024`; no caption is treated as identity.

Create `src/Workspace/WidgetContentHost.cs` for `public sealed class WidgetContentHost : WidgetContainer` with `Widget? Content` and `void SetContent(Widget? content)`. One child maximum; old content is detached, not disposed; preserve editor focus on an eligible replacement. If validation/attachment fails, retain or restore the old content and focus. `WidgetHost` must provide a narrow internal preflight and rollback for this atomic replacement rather than expose its mutable registration tables.

Add `WidgetTabActivationRequestedAction(string WidgetId, string TabKey)` and `WidgetTabCloseRequestedAction(string WidgetId, string TabKey)` to `src/Core/WidgetAction.cs`. These are requests; the application calls `SetTabs`/`SetContent` to accept them. `WidgetTabStrip` never alters documents or tab membership in response to close.

### Task 1: W1201 contract and resource baseline

**Files:** Modify `Icod.Widgets.csproj`, `src/Core/WidgetLimits.cs`; create `tests/Icod.Widgets.Tests/Workspace/WidgetWorkspaceContractTests.cs`, `docs/W1201-Workspace-Contract.md`. Test project already includes recursive C# files.

**Interfaces:** Produces `WidgetLimits.MaximumTabs = 1024`; validates that the 1.1 assembly identity and DCurses dependency remain unchanged. Set implementation source `Version`/`PackageVersion` to `1.2.0-alpha.1` here and align source-version assertions; no prerelease upload. A public-only custom `WidgetContainer`/action-emitting control is the extension seam witness; any discovered gap is fixed in W1201 before dependent work. No runtime behavior changes beyond the limit.

- [ ] **Step 1: Write failing contract test.** Assert `WidgetLimits.MaximumTabs == 1024`, `typeof(WidgetHost).Assembly.GetName().Version == new Version(1,0,0,0)`, and the pre-existing 1.1 public signature witness compiles unmodified. Add a public-only custom container that arranges a child, emits an action, and handles capture cancellation; make its compile/runtime check part of W1201. Record exact baseline.

  ```csharp
  Assert.Equal( 1024, WidgetLimits.MaximumTabs );
  Assert.Equal( new Version( 1, 0, 0, 0 ), typeof( WidgetHost ).Assembly.GetName().Version );
  ```
- [ ] **Step 2: Verify red.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --filter FullyQualifiedName~WidgetWorkspaceContractTests`; expect compile failure for `MaximumTabs`.
- [ ] **Step 3: Implement.** Add `public const int MaximumTabs = 1024` in `src/Core/WidgetLimits.cs`; set `Version` and `PackageVersion` to `1.2.0-alpha.1` in `Icod.Widgets.csproj`; do not alter existing constants, assembly identity, or dependency versions.
- [ ] **Step 4: Verify green.** Run the same filtered test, then `dotnet build Icod.Widgets.sln -c Release`; expect zero failures/errors. Update W1201 with exact commands/outcomes.
- [ ] **Step 5: Commit.** Stage `Icod.Widgets.csproj`, `src/Core/WidgetLimits.cs`, the contract test, and W1201 evidence; `git commit -m "feat: establish 1.2 workspace contract"`.

### Task 2: W1202 stable-key tabs

**Files:** Create `src/Workspace/WidgetTab.cs`, `src/Workspace/WidgetTabStrip.cs`, `tests/Icod.Widgets.Tests/Workspace/WidgetTabStripTests.cs`; modify `src/Core/WidgetAction.cs`; create `docs/W1202-Tab-Strip.md`.

**Interfaces:** Consumes `MaximumTabs`; produces the public contracts in the file map. Actions include only stable key, not index/caption. `SetTabs` is caller-controlled and validates a complete new snapshot before committing.

- [ ] **Step 1: Write failing tests.** `DuplicateKeysLeavePreviousSnapshotIntact` checks old state after `ArgumentException`; `RemovedActiveSelectsNearestSurvivor`; `OverflowKeepsActiveVisible`; `PointerAndKeyboardEmitRequestsOnce`; `EmptyAndDisabledTabsRemainCancelable`. Include wide glyph captions and screen width 1.

  ```csharp
  Assert.Throws<ArgumentException>( () => strip.SetTabs( [ new( "a", "A" ), new( "a", "Again" ) ] ) );
  Assert.Equal( "a", strip.ActiveKey );
  Assert.Contains( strip.ActiveKey!, strip.VisibleKeys );
  Assert.Single( result.Actions.OfType<WidgetTabCloseRequestedAction>() );
  ```
- [ ] **Step 2: Verify red.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --filter FullyQualifiedName~WidgetTabStripTests`; expect missing types.
- [ ] **Step 3: Implement.** Add the exact records/members/actions above. Render only clipped visible tabs with DCurses cell-width measurement; support Left/Right, Home/End, Ctrl+Tab/Ctrl+Shift+Tab, Enter, primary press, and a close affordance. Compute overflow deterministically; avoid layout work proportional to document text. Bound snapshot size and action capacity before emitting.
- [ ] **Step 4: Verify green.** Run the same filtered test and `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0`; expect zero failures. Record W1202 evidence.
- [ ] **Step 5: Commit.** Stage the five named files and `git commit -m "feat: add stable-key tab strip"`.

### Task 3: W1203 replaceable content and cancellation

**Files:** Create `src/Workspace/WidgetContentHost.cs`, `tests/Icod.Widgets.Tests/Workspace/WidgetContentHostTests.cs`; modify `src/Core/WidgetHost.cs`, `src/Layout/WidgetContainer.cs` only for atomic preflight/replace coordination; create `docs/W1203-Active-Content.md`.

**Interfaces:** Consumes caller-owned `Widget` instances, including `WidgetTextEditor`. Produces `WidgetContentHost.SetContent(Widget? content)` and `Content`. Never disposes widgets, documents, or views. Old content is detached before new content receives input; preflight checks ownership, duplicate IDs, and capacity before removing old content.

- [ ] **Step 1: Write failing tests.** `ThreeViewsRetainCaretSelectionScrollAndUndo` swaps three editors while asserting their `View` references and state survive; `LateClipboardAndPasteCannotEditReplacement` starts old paste/clipboard, swaps, then asserts replacement document text unchanged; `CapturedReleaseCannotReachReplacement` swaps during pointer capture; `FailedReplacementPreservesOldContentAndFocus` fills host to 1,024 and tries an oversized subtree; `SameIdReattachCannotReviveOldRequest` checks attachment generations; `NullContentShowsEmptyWorkspace` checks zero child/focus fallback.

  ```csharp
  content.SetContent( secondEditor );
  Assert.Same( secondEditor, content.Content );
  Assert.False( host.ApplyClipboardResult( stalePasteResult ) );
  Assert.Equal( originalText, secondDocument.Read( 0, originalText.Length ) );
  ```
- [ ] **Step 2: Verify red.** Run filtered `WidgetContentHostTests`; expect missing type/member.
- [ ] **Step 3: Implement.** Add `WidgetContentHost(string id)` and `SetContent`. Introduce `internal void ValidateReplacement(Widget? oldContent, Widget? newContent)` in host for count/ownership/ID preflight; use existing detach invalidation and capture cancellation; arrange new child to host bounds. If attach unexpectedly fails after detachment, reattach the old subtree before rethrowing; if focus belonged to old content, restore it on rollback or focus the first eligible replacement descendant on success. Do not change `WidgetTextEditor.View` or call `Dispose` on views.
- [ ] **Step 4: Verify green.** Run filtered tests and the entire net10 suite; expect zero failures. Run editor `--smoke` to check 1.1 behavior. Record W1203 evidence.
- [ ] **Step 5: Commit.** Stage named implementation/test/evidence files and `git commit -m "feat: switch caller-owned workspace content safely"`.

## Handoff

Next use the [commands/status plan](2026-10-12-icod-widgets-1.2-commands-status.md), followed by dialogs/save, search, and integration. Check `git status --short` between tasks. Never mark a W12xx record complete on documentation alone.
