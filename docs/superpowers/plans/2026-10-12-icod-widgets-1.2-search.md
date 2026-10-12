# Icod.Widgets 1.2 Search and Navigation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Execute natively without subagents.

**Goal:** Collect typed Find, Replace, Replace All, and Go To Line intent without embedding a search engine or editing application documents.

**Architecture:** Forms own bounded text/option fields and emit versioned semantic requests. The application executes search/regex/replacement against its chosen snapshot and decides wrap/not-found behavior. The form accepts a result only while its owner, view, revision, and generation still match; applying a result changes form display only.

**Tech Stack:** C# 13, net8.0/net9.0/net10.0, Icod.DCurses 2.4.0, xUnit.

**Spec:** [1.2 Document Workspace design](../specs/2026-10-12-icod-widgets-1.2-document-workspace-design.md)

## Global Constraints

- Package `1.2.0`; assembly `1.0.0.0`; sole direct runtime dependency DCurses `2.4.0`.
- Search/regex execution, snapshot choice, replacement transactions, and global last-query state stay in the application.
- Application-loop requests/results only; no worker, timer, file I/O, or hidden document edit.
- Preserve 1.1 text field, editor, menu, and Escape behavior; LGPL library/GPL tests with full headers.

## Review Focus

- Query changed while a result is in flight: reject old result and retain new text (Task 2).
- Same revision number in another document/view: identity mismatch still rejects result (Task 2).
- Replace All on read-only document: form emits intent; application must refuse mutation, not library bypass (Task 1).
- Invalid or overflowing one-based line number: keep Go To open with visible error (Task 3).
- Ctrl+F while text field is focused: menu shortcut must not insert `f` into query or dispatch twice (Task 1).

---

## File map and public contracts

Create `src/Search/WidgetSearchRequest.cs` with `WidgetSearchDirection { Forward, Backward }`, `WidgetSearchIntent { Find, ReplaceOne, ReplaceAll }`, `WidgetSearchOptions(bool MatchCase = false, bool WholeWord = false, bool UseRegularExpression = false, WidgetSearchDirection Direction = Forward)`, `WidgetSearchRequest(Guid RequestId, Guid FormId, long Generation, Guid DocumentId, Guid ViewId, long Revision, WidgetSearchIntent Intent, string Query, string? Replacement, WidgetSearchOptions Options)`, and `WidgetSearchResult(WidgetSearchRequest Request, bool Found, long? MatchStart = null, long MatchLength = 0, string? Error = null)`. Add `WidgetSearchRequestedAction(string WidgetId, WidgetSearchRequest Request)` in `src/Core/WidgetAction.cs`. The match span is application-computed in UTF-16 offsets for the request's revision; the form does not apply it.

Create `src/Search/WidgetSearchForm.cs` as `WidgetDialog` with `WidgetSearchForm(string id, string title, bool allowReplace)`, read-only public `QueryInput`, optional `ReplacementInput`, `Options`, and `Error`; `SetContext(Guid documentId, Guid viewId, long revision)`, `SetOptions(WidgetSearchOptions options)`, and `bool ApplyResult(WidgetSearchResult result)`. The form never moves an editor selection or edits text. Submit yields an action; close yields Cancel. Query limit: 4,096 UTF-16 code units; replacement limit: 65,536 units; reject beyond limit instead of silent truncation.

Create `src/Search/WidgetGoToLineDialog.cs` with `WidgetGoToLineDialog(string id, string title)`, `SetContext(Guid documentId, Guid viewId, long revision)`, and typed `WidgetGoToLineRequest(Guid RequestId, Guid DocumentId, Guid ViewId, long Revision, long LineNumber)` returned as `WidgetDialogResult.Value` on Accepted. The line number is one-based and positive; the application bounds it against its snapshot and chooses navigation behavior.

### Task 1: W1208 search and replace form submissions

**Files:** Create `src/Search/WidgetSearchRequest.cs`, `src/Search/WidgetSearchForm.cs`, `tests/Icod.Widgets.Tests/Search/WidgetSearchFormTests.cs`, `docs/W1208-Search-Navigation.md`; modify `src/Core/WidgetAction.cs`.

**Interfaces:** Produces exact search types and form methods in file map. Requires W1206 dialog child composition. Application owns search execution and last-query state.

- [ ] **Step 1: Write failing tests.** `FindAndReplaceEmitTypedRequests` asserts intent, query, replacement, options, document/view/revision IDs; `ReplaceUnavailableInFindOnlyMode`; `ReadonlyApplicationMayRejectReplaceWithoutWidgetMutation` asserts no document edit from submission; `EscapeCancelsWithoutRequest`; `QueryAndReplacementLimitsRejectWithoutTruncation`; `CtrlFShortcutDoesNotTypeOrDuplicate` checks menu precedence and one action. Use Unicode/CRLF query text and empty-query validation.

  ```csharp
  WidgetSearchRequest request = Assert.IsType<WidgetSearchRequestedAction>( Assert.Single( result.Actions ) ).Request;
  Assert.Equal( WidgetSearchIntent.ReplaceAll, request.Intent );
  Assert.Equal( document.Id, request.DocumentId );
  Assert.Equal( originalText, document.Read( 0, originalText.Length ) );
  ```
- [ ] **Step 2: Verify red.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --filter FullyQualifiedName~WidgetSearchFormTests`; expect missing types.
- [ ] **Step 3: Implement.** Compose existing `WidgetTextInput` fields and option controls; validate before emitting one `WidgetSearchRequestedAction` per accepted gesture; add exact public types. For Find-only mode, replacement controls/actions cannot be activated. Do not call document `Apply`, regex APIs, or background services.
- [ ] **Step 4: Verify green.** Run filtered and full net10 tests; expect zero failures. Record W1208 evidence.
- [ ] **Step 5: Commit.** Stage named files and `git commit -m "feat: emit typed search and replace intent"`.

### Task 2: W1208 result generation guards

**Files:** Modify `src/Search/WidgetSearchForm.cs`; create `tests/Icod.Widgets.Tests/Search/WidgetSearchResultTests.cs`; update `docs/W1208-Search-Navigation.md`.

**Interfaces:** Consumes `WidgetSearchResult`; `ApplyResult` returns false for closed, replaced, changed query/options, foreign document/view, stale revision, or non-current request. It may update `Error`/match feedback only for a current request; application applies selection/edits separately.

- [ ] **Step 1: Write failing tests.** `QueryEditInvalidatesOldResult`, `SameRevisionForeignDocumentRejected`, `ViewSwitchInvalidatesResult`, `ClosedFormRejectsResult`, `LatestResultUpdatesFeedbackOnly`, and `ChangingOptionsInvalidatesResult`; assert both documents remain unchanged and stale result returns false.

  ```csharp
  Assert.False( form.ApplyResult( staleResult ) );
  Assert.True( form.ApplyResult( currentResult ) );
  Assert.Equal( originalText, document.Read( 0, originalText.Length ) );
  ```
- [ ] **Step 2: Verify red.** Run filtered `WidgetSearchResultTests`; expect stale result acceptance or missing behavior.
- [ ] **Step 3: Implement.** Assign a per-form GUID and checked monotonically increasing generation on each submission and every state invalidation; remember only the latest `RequestId`. Compare all request identity/revision/generation fields on completion. Invalidate on close, context change, query/replacement/options change; do not retain a document snapshot in the form.
- [ ] **Step 4: Verify green.** Run filtered/full net10 tests; expect zero failures. Update W1208 evidence.
- [ ] **Step 5: Commit.** Stage named files and `git commit -m "feat: reject stale search form results"`.

### Task 3: W1208 Go To Line dialog

**Files:** Create `src/Search/WidgetGoToLineDialog.cs`, `tests/Icod.Widgets.Tests/Search/WidgetGoToLineDialogTests.cs`; update `docs/W1208-Search-Navigation.md`.

**Interfaces:** Produces `WidgetGoToLineRequest` as typed `WidgetDialogResult.Value` on Accepted. The application validates the requested line against its chosen document revision before moving a view.

- [ ] **Step 1: Write failing tests.** `PositiveOneBasedLineCompletesWithContext`, `ZeroNegativeOverflowAndNonDigitsKeepDialogOpen`, `EscapeReturnsCancel`, `ContextSwitchRejectsOldFieldState`, and `CompactViewportKeepsCancelReachable`. Assert no editor selection change on completion.

  ```csharp
  WidgetGoToLineRequest request = Assert.IsType<WidgetGoToLineRequest>( completion.Result.Value );
  Assert.Equal( 42, request.LineNumber );
  Assert.Equal( view.Id, request.ViewId );
  Assert.Equal( oldCaret, view.Caret );
  ```
- [ ] **Step 2: Verify red.** Run filtered `WidgetGoToLineDialogTests`; expect missing type.
- [ ] **Step 3: Implement.** Compose `WidgetTextInput` with bounded numeric parsing using `long.TryParse` invariant culture and explicit `LineNumber > 0`; create a fresh request ID at acceptance. Error remains visible until edited. Do not clamp to a document's unknown line count or navigate the editor.
- [ ] **Step 4: Verify green.** Run filtered/full net10 tests; expect zero failures. Update W1208 evidence.
- [ ] **Step 5: Commit.** Stage named files and `git commit -m "feat: collect typed go-to-line requests"`.

## Handoff

Continue with [integration and qualification](2026-10-12-icod-widgets-1.2-integration.md). The application must compare request context with its current document before applying any result, even if the form accepted that result for display.
