# Icod.Widgets 1.1 Editing Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. The user-selected execution method is sequential native work; do not delegate to subagents.

**Goal:** Deliver reusable, data-preserving single-document editing and compatible single-line field improvements for Icod.Widgets 1.1.0.

**Architecture:** Separate document/history, per-view state, and the editing control. Reuse DCurses 2.4 public geometry, routing, clipboard, and cursor mechanisms; the application owns async operations and the loop. Repair host input/focus seams before composing editor behavior.

**Tech Stack:** Managed C# 13, .NET 8/9/10, Icod.DCurses 2.4.0, existing xUnit and public sample/package-consumer projects.

**Spec:** [2026-10-11-icod-widgets-1.1-design.md](../specs/2026-10-11-icod-widgets-1.1-design.md); [delivery roadmap](../../../Icod.Widgets-1.1.0-Development-Roadmap.md).

## Global Constraints

- C# 13; `net8.0;net9.0;net10.0`; AnyCPU; managed implementation.
- Sole direct runtime dependency: `Icod.DCurses 2.4.0`; Terminal 1.29.0 is transitive.
- Stable 1.0 public API compatibility; `AssemblyVersion` remains `1.0.0.0`.
- Library LGPL-3.0-or-later; samples/tests GPL-3.0-or-later with full prescribed headers.
- Application-owned input reader, event loop, async completion, and monotonic time.
- No terminal protocol strings, native/OS clipboard calls, or Widgets file writes.
- Preserve screen-only `WidgetHost` construction and existing session overload.
- Escape remains Cancel in dialogs, including OK-only dialogs; mnemonics remain distinct from shortcuts.
- Sequential implementation without subagent delegation; owner controls merge/tag/publication.

## Review Focus

- Chunk boundaries inside CRLF/surrogate/combining sequences must not corrupt original text or the line index (W1104).
- Saved checkpoint identity must survive undo branching and history eviction without incorrectly clearing modified state (W1105).
- One enormous line or grapheme must not force whole-line layout or freeze each input iteration (W1106).
- A late clipboard completion after selection/focus/attachment reuse must not paste into or cut the wrong target (W1109).
- Action-capacity rejection and capture cancellation must precede partial mutations, including external custom controls (W1103/W1107).

---

## How to execute and track

This is an implementation contract proposal, not shipped API documentation. New
names below are concrete starting interfaces. W1101 confirms them and records
measured resource defaults before dependent tasks begin. If a signature must
change, amend the spec/plan and affected consumers together; do not let separate
tasks invent incompatible APIs. Private store choice remains a measured gate.

For each tranche: write its named regression tests, show they fail for the intended
missing behavior, implement the minimal change, run its focused tests and the full
existing suite, review the diff, record evidence, then commit only that tranche's
files. No failing test or unapproved design change is carried to the next tranche.

Focused test command (replace `<Class>` with the named class in the task):

```text
dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Staging --filter FullyQualifiedName~<Class>
```

Expected red: a missing proposed member at compile time or the named behavior
assertion fails, not a restore/SDK error. Expected green: zero failures on all
three TFMs, followed by the unfiltered command with zero failures. Do not use
`--no-build` during red/green development. Record unavailable runtimes honestly.

## File and contract map

| Area | Planned files | Boundary |
| --- | --- | --- |
| Host | Existing `src/Core/WidgetInput.cs`, `Widget.cs`, `WidgetHost*.cs`; new `WidgetEditCommand.cs` | Lossless normalized input, focus/capture/commands/ticks, safe external hooks |
| Text | New `src/Text/WidgetTextDocument.cs`, `WidgetTextSnapshot.cs`, `WidgetTextLine.cs`, `WidgetTextEdit.cs`, `WidgetTextDocumentOptions.cs`, `WidgetTextHistory.cs` | Exact source, indexed lines, revisions, atomic edits, bounded history |
| Internal storage | New `src/Text/Storage/` files selected by W1101 | Private text/index implementation; never exposed as public API |
| View | New `src/Editing/WidgetTextViewState.cs`, `WidgetTextProjection.cs`, `WidgetEditorOptions.cs` | Per-view anchors and bounded Unicode/cell projection |
| Controls | New `src/Controls/WidgetTextEditor.cs`, `WidgetScrollBar.cs`; existing `WidgetTextInput.cs` | Public multiline editing and field parity; keep `WidgetScrollIndicator` |
| Services | New `src/Core/WidgetClipboardRequest.cs`, `WidgetClipboardResult.cs`, `WidgetCaretIntent.cs`, `WidgetHost.Editing.cs` | Typed requests/results and presentation intent, no hidden session ownership |
| Tests | New Text/Editing tests; extend Core/Controls/Qualification/Acceptance; `tests/PackageConsumer` | Models, retained cells, external/binary/package consumers |
| Samples/docs | Existing sample programs/shared loop; new focused support files and W11xx records | Public-API witnesses, measured evidence, release qualification |

### W1101 — Baseline, storage experiment, and contract gate

**Files:** Modify `Icod.Widgets.csproj`, `tests/PackageConsumer/PackageConsumer.csproj`,
`tests/Icod.Widgets.Tests/Core/DependencyBoundaryTests.cs`; create
`docs/W1101-Contract-and-Storage-Decision.md`, `tests/EditorMeasurements/EditorMeasurements.csproj`,
`tests/EditorMeasurements/Program.cs`, `tests/EditorMeasurements/StorageCandidates.cs`.
Keep the GPL measurement executable non-packable; no production dependency on it.

**Interfaces:** Consumes the stable API fingerprint and released DCurses 2.4.
Produces the storage decision, exact default/max resource table, workload ceilings,
and reviewed public signatures used below. Candidates share an internal benchmark
interface for length, bounded read, replacement, snapshot, and line lookup; it is
not a production/public abstraction commitment.

- [x] Add `SoleDirectDependencyIsDCurses240` asserting one direct PackageReference
  at 2.4.0, and capture a consumer compiled against the released 1.0 package.
- [x] Run the focused boundary test and record the expected old-dependency failure.
- [x] Set implementation `Version`/`PackageVersion` to `1.1.0-alpha.1`, DCurses to
  2.4.0, and the new package consumer to the same preview. Preserve assembly 1.0.0.0.
- [x] Run existing tests and the old binary consumer with the upgraded dependency;
  investigate actual compatibility failures before introducing new APIs.
- [x] Implement/measure gap-buffer and piece-table/tree experiments with identical
  seeded edit traces and exact string-oracle checks. Use a small 4 Ki-unit fixture,
  100,000-line fixture, 10,000,000-unit single line, mixed Unicode/EOL, local typing,
  distant alternating edits, retained snapshots, and history trim. Fixtures are
  workloads, not supported-size promises.
- [x] Record cold/warm time, allocation and retained-memory results, logical-line
  index work, fragmentation, and visible-slice work. Choose a store; define count
  versus byte budgets and concrete default/maximum/rejection behavior for every
  limit listed in the spec. Freeze word categories, indent width/tab policy,
  default inserted newline, typing-group timeout, and projection work ceilings.
- [x] Review the decision with the owner before W1104–W1108. Record any proven
  DCurses gap with a minimal reproducer; otherwise retain the released dependency.
- [x] Verify build/test/pack and matching package-consumer version; commit
  `build: establish Widgets 1.1 editing baseline and measured contracts`.

### W1102 — Preserve normalized input fidelity

**Files:** Modify `src/Core/WidgetInput.cs`, `src/Menus/WidgetMenuItem.cs`;
create `tests/Icod.Widgets.Tests/Core/WidgetInputTests.cs`; record
`docs/W1102-Rich-Input.md`.

**Interfaces:** Add init metadata `int? FunctionKeyNumber`,
`CursesKeyEventPhase? KeyPhase`, `CursesFocusState? FocusState`,
`CursesPastePhase? PastePhase`, plus `CursesKeyModifiers SourceModifiers` to
preserve flags not represented in the old enum. Preserve existing factories.
Add `WidgetInput.FunctionKey(int number, WidgetInputModifiers modifiers = None)`,
`WidgetInput.PasteChunk(CursesPastePhase phase, string? text = null)`, and
`WidgetInput.FocusChanged(CursesFocusState state)`. Existing `Paste(string)`
with null PastePhase denotes a complete paste. Extend shortcut function identity
with a non-positional member; keep the old constructor/deconstruction.

- [x] Add `FromPreservesRichMetadata`, `LegacyFactoriesKeepTheirMeaning`, and
  `FunctionShortcutsRemainDistinct`; assert F6 differs from F10, Begin/Data/End
  is retained, Focused differs from Unfocused, and release/repeat/associated text
  and all source modifier bits survive. Use public decoder/replay inputs where
  DCurses factories are internal, not reflection or InternalsVisibleTo.
- [x] Run WidgetInputTests red.
- [x] Implement additive conversion/factories and function-shortcut matching;
  do not reinterpret legacy enum values or append positional constructor args.
- [x] Run WidgetInputTests green, menu tests, and the full suite; confirm existing
  single-string paste still works in forms. Commit `feat: preserve rich widget input`.

### W1103 — Focus, commands, capture, ticks, and external hooks

**Files:** Modify `src/Core/Widget.cs`, `WidgetHost.cs`, `WidgetHost.Modals.cs`,
`WidgetHost.Popups.cs`; create `src/Core/WidgetEditCommand.cs`,
`tests/Icod.Widgets.Tests/Core/WidgetEditingRoutingTests.cs`; extend
`tests/PackageConsumer/Program.cs`; record `docs/W1103-Editing-Routing.md`.

**Interfaces:** Add `WidgetHost.Update(TimeSpan timestamp): WidgetDispatchResult`,
`WidgetHost.MoveFocus(CursesFocusDirection direction): bool`, and
`WidgetHost.ExecuteEditCommand(WidgetEditCommand command, TimeSpan timestamp): WidgetCommandResult`.
`WidgetCommandResult` is NotApplicable/Disabled/Executed; Disabled consumes a
recognized focused command rather than falling through to a background command.
`WidgetEditCommand` covers Copy/Cut/Paste/SelectAll/Undo/Redo.
`IWidgetEditTarget` exposes `CanExecuteEditCommand(command): bool` and
`ExecuteEditCommand(command, timestamp): WidgetCommandResult`.
Add `Widget.AcceptsTab: bool` (virtual, default false), protected
`EnsureActionCapacity(int count)`, `EmitAction(WidgetAction action)`,
`OnCaptureCanceled()`, and `OnUpdate(TimeSpan timestamp)` hooks. Preserve the
existing internal capture callback as a bridge, not a changed binary signature.

- [x] Add `FocusedEditorGetsTabButFieldTraverses`, `DisabledEditCommandDoesNotFallThrough`,
  `CapturedMovesStayWithOwner`, `CancellationEndsCaptureExactlyOnce`,
  `BackwardTickDoesNotMutate`, `ReleaseCannotActivate`, and
  `ExternalActionCapacityFailureIsAtomic` using tiny custom test controls.
- [x] Run WidgetEditingRoutingTests red.
- [x] Implement overlay-first routing, focused edit-command precedence, explicit
  focus movement, release suppression, capture of move/release through DCurses,
  and cancellation on loss/resize/hide/disable/detach/disposal. Validate ownership,
  WidgetId, and capacity before external action publication. A foreign or detached
  control cannot emit. Update uses the same monotonic clock validation as Process.
- [x] Compile a separate public consumer subclass that emits an action and receives
  update/cancel hooks. Check it without internal/friend access.
- [x] Run focused/core/modal/menu/lifecycle tests and full suite green; commit
  `feat: add scoped editing routing and caller-driven widget updates`.

### W1104 — Exact document, line index, and snapshots

**Files:** Create `src/Text/WidgetTextDocument.cs`, `WidgetTextDocumentOptions.cs`,
`WidgetTextLine.cs`, `WidgetTextSnapshot.cs`, storage files selected at W1101,
`tests/Icod.Widgets.Tests/Text/WidgetTextDocumentTests.cs`; record
`docs/W1104-Document-and-Snapshots.md`.

**Interfaces:** `WidgetTextDocument(IEnumerable<ReadOnlyMemory<char>> chunks,
WidgetTextDocumentOptions? options = null)` owns copied/retained immutable source,
not borrowed mutable input buffers. Expose `Guid Id`, `long Length`, `long LineCount`,
`long Revision`, `long StateId`, `Read(long offset, int length): string`,
`GetLine(long index): WidgetTextLine`, and `CreateSnapshot(): WidgetTextSnapshot`.
`WidgetTextLine` contains long Start/ContentLength and int LineEndingLength.
Snapshot implements IDisposable and exposes matching identity/revision/state,
length/line count, bounded Read and GetLine. Options contain W1101 frozen limits.

- [x] Add `ChunkBoundariesPreserveExactText`, `MixedEndingsIndexCorrectly`,
  `EmptyAndFinalNewlineHaveCorrectLines`, `SnapshotDisposeReleasesRetention`,
  `SnapshotLimitRejectsWithoutEviction`, and `MutableInputIsNotBorrowed`.
  For `"a\r\nb\nc\rd"`, assert starts 0,3,5,7 and ending lengths 2,1,1,0;
  `"x\n"` has two lines; empty text has one. Split each fixture at every offset.
- [x] Run WidgetTextDocumentTests red.
- [x] Implement the selected store/index with overflow-safe long arithmetic,
  explicit ownership and disposable snapshots, and no whole-text public getter.
  Source controls/invalid UTF-16 remain byte-for-byte equivalent as UTF-16 units.
- [x] Run tests green plus seeded random read/index comparisons. Measure initial
  loading and snapshot retention against W1101 limits; commit
  `feat: add indexed exact-text documents and immutable snapshots`.

### W1105 — Transactions, undo/redo, and saved checkpoints

**Files:** Create `src/Text/WidgetTextEdit.cs`, `WidgetTextHistory.cs`,
`tests/Icod.Widgets.Tests/Text/WidgetTextHistoryTests.cs`; modify document/snapshot;
record `docs/W1105-Transactions-and-History.md`.

**Interfaces:** `WidgetTextEdit(long Start, long DeleteLength, string InsertText)`;
`WidgetTextDocument.Apply(IReadOnlyList<WidgetTextEdit> edits, long expectedRevision,
WidgetTextEditContext context): WidgetTextEditResult`; `Undo(): WidgetTextEditResult`;
`Redo(): WidgetTextEditResult`; `MarkSaved(WidgetTextSnapshot snapshot): void`;
`IsModified: bool`. Context contains view ID, before/after anchor/caret offsets,
edit kind, caller timestamp, and explicit coalescing choice. Result contains status
(Applied/NoChange/Stale/LimitExceeded/InvalidRange), revision/state, and optional
invoking-view selection restoration. Add change notification carrying replaced
ranges and revision so views can reconcile; callbacks observe committed state.

- [x] Add `BatchIsAtomic`, `OnePasteOneUndo`, `UndoBranchDoesNotReuseStateId`,
  `UndoToCheckpointClearsModified`, `LateSaveDoesNotCleanNewerText`,
  `TrimDoesNotCleanDocument`, `OversizedUndoGroupRejectsBeforeMutation`, and
  `BoundaryFailureDoesNotSplitClusterOrCrLf`. Add randomized edit/undo/redo oracle
  traces with reachable checkpoint tracking, including snapshots retained across trim.
  Add `SnapshotSurvivesLaterEditsAndUndo` here, once Apply/Undo exist; assert that
  a retained snapshot still reads its original text while the document changes.
- [x] Run WidgetTextHistoryTests red.
- [x] Implement prevalidation and atomic commit, whole-group eviction, redo
  invalidation, revision versus state identity, snapshot identity validation, and
  saved-state bookkeeping. Apply replacements in an order that preserves the
  common source revision; reject overlap. Newline edits only change addressed text.
- [x] Implement explicit typing coalescence using W1101 policy; break on view
  changes and checkpoints. Do not silently drop undo for a large edit.
- [x] Run history/document tests and full suite green; rerun snapshot/history
  measurements; commit `feat: add bounded text transactions and checkpoint-aware undo`.

### W1106 — View state and bounded projection

**Files:** Create `src/Editing/WidgetTextViewState.cs`, `WidgetTextProjection.cs`,
`WidgetEditorOptions.cs`, `tests/Icod.Widgets.Tests/Editing/WidgetTextProjectionTests.cs`;
record `docs/W1106-View-and-Projection.md`.

**Interfaces:** `WidgetTextViewState(WidgetTextDocument document)` implements
IDisposable (unsubscribe, do not dispose document); exposes `Guid Id`, document,
caret/anchor UTF-16 offsets, selection epoch, preferred column, wrap/scroll state;
`SetSelection(long anchor, long caret): void`. Proposed internal projection takes
view, CursesScreen width provider, viewport rectangle, and budget; returns visible
rows, source mapping, caret cell, selection spans, and exact/estimated extent.
Do not expose private storage/layout cache as public data.

- [x] Add `TwoViewsKeepIndependentState`, `ViewChangeDoesNotDirtyDocument`,
  `DocumentEditReconcilesAnchors`, `PreferredColumnSurvivesShortLine`,
  `CacheInvalidatesOnlyAffectedRevisionAndGeometry`, `LongLineHasBoundedSlices`,
  `HugeClusterHasSafeBoundedFallback`, and `TabsUseTrueSourceColumn`.
- [x] Run WidgetTextProjectionTests red.
- [x] Implement directional selection and revision reconciliation, then bounded
  DCurses slices/hit-testing/selection spans. Carry Unicode and tab continuation;
  retain no full-file pad/layout/string per render. Cap work/cache to W1101 limits,
  exposing incomplete/estimated wrapped extent and continuation honestly.
- [x] Run retained-cell/hit-test round trips for combining/wide/ZWJ/control text,
  zero/one-cell viewports and far horizontal offsets. Verify warm edit/scroll work
  counters do not grow with unrelated file size; run measured cold seeks separately.
- [x] Run focused/full tests green; commit `feat: project bounded editable text views`.

### W1107 — Multiline editor and atomic paste

**Files:** Create `src/Controls/WidgetTextEditor.cs`,
`tests/Icod.Widgets.Tests/Controls/WidgetTextEditorTests.cs`; extend
`src/Editing/WidgetEditorOptions.cs`, `src/Core/WidgetAction.cs`; record
`docs/W1107-Multiline-Editor.md`.

**Interfaces:** `WidgetTextEditor(string id, WidgetTextViewState view,
WidgetEditorOptions? options = null)` borrows view/document; implements
IWidgetEditTarget and AcceptsTab. Expose `IsReadOnly`, `IsOverwriteMode`, `View`.
Options specify W1101 newline/indent/tab/wrap/grouping/paste budgets. Add
`WidgetTextEditedAction` carrying document ID/revision/state and
`WidgetTextSelectionChangedAction` carrying view ID/anchor/caret; neither carries
the entire document. Copy/Cut/Paste produce service requests in W1109, not I/O here.

- [x] Add `MovementAndDeletionRespectTextElements`, `WordAndPageNavigation`,
  `SelectionReplacementIsOneTransaction`, `IndentAndOutdentAreUndoable`,
  `OverwriteDoesNotSplitClusterOrCrossLineEnd`, `ReadOnlyCannotMutate`,
  `AssociatedTextInsertedOnce`, `BracketedPasteCommitsOnlyAtEnd`,
  `InterruptedOrOversizedPasteDoesNotMutate`, and `FullActionQueueLeavesEditUnchanged`.
- [x] Run WidgetTextEditorTests red.
- [x] Implement editor commands with shared document transactions and bounded
  projection. Newline insertion follows options; pasted text keeps its original
  endings/tabs. Home/End distinguishes line versus Ctrl document boundary;
  Shift extends selection; Ctrl+A selects all. Validate action capacity before edits.
- [x] Implement bounded Begin/Data/End accumulation and cancellation on target
  invalidation; legacy complete Paste is one transaction. Release never types.
- [x] Run input replay, retained render, read-only, capacity and full tests green;
  commit `feat: add Unicode multiline text editing`.

### W1108 — Scrollbars, capture, and caller-clock autoscroll

**Files:** Create `src/Controls/WidgetScrollBar.cs`,
`tests/Icod.Widgets.Tests/Controls/WidgetScrollBarTests.cs`,
`tests/Icod.Widgets.Tests/Editing/WidgetEditorPointerTests.cs`; modify editor/view;
record `docs/W1108-Scrolling-and-Selection.md`.

**Interfaces:** `WidgetScrollBar(string id, WidgetScrollOrientation orientation)`;
`SetRange(long extent, long viewport, long offset): void`; read-only long Offset;
`WidgetScrollChangedAction(string WidgetId, long Offset)`. Orientation is
Horizontal/Vertical. Keep old WidgetScrollIndicator unchanged. Editor uses host
capture/update hooks and exposes scrollbar ranges from its projection state.

- [x] Add `ThumbMapsLargeRangeWithoutOverflow`, `ZeroAndShortTracksRemainUsable`,
  `TrackPagesAndKeyboardScrolls`, `ScrollbarDoesNotMoveCaret`,
  `DragOutsideExtendsSelection`, `TickAutoscrollIsBoundedAndMonotonic`, and
  `LossModalResizeDetachCancelDrag`. Include ranges near long.MaxValue and
  rapid alternating wheel/drag input; distinguish estimated wrapped ranges.
- [x] Run scrollbar and pointer classes red.
- [x] Implement range/thumb/page math, axis-specific wheel behavior, primary and
  Shift pointer selection, captured outside moves/releases, and capped autoscroll
  driven only by Update. Stop work immediately when capture becomes invalid.
- [x] Run tests green plus retained cells at tiny geometry; commit
  `feat: add interactive scrolling and captured text selection`.

### W1109 — Clipboard requests and guarded completion

**Files:** Create `src/Core/WidgetClipboardRequest.cs`, `WidgetClipboardResult.cs`,
`WidgetHost.Editing.cs`, `tests/Icod.Widgets.Tests/Editing/WidgetClipboardTests.cs`,
`samples/Common/WidgetSampleClipboardAdapter.cs`;
modify editor/command handling and sample project shared-source includes as needed;
record `docs/W1109-Clipboard.md`.

**Interfaces:** `WidgetClipboardOperation` is Copy/Cut/Paste.
`WidgetClipboardRequest` has Guid RequestId/ViewId/DocumentId, attachment/focus
generation, revision, selection epoch/range, operation, and bounded export text
for Copy/Cut. `WidgetClipboardRequestedAction(string WidgetId, WidgetClipboardRequest Request)`
derives from WidgetAction. `WidgetClipboardResult` has RequestId, status
(Success/Denied/Unsupported/TimedOut/Canceled/TooLarge/InvalidText/Failed), and
bounded paste text on Success. `WidgetHost.ApplyClipboardResult(WidgetClipboardResult result): bool`
returns whether a live request consumed the completion; false means stale/foreign.
The application drains completion actions afterward. Success is the application's
explicit acceptance of transport/fallback policy, not proof of remote acceptance.

- [x] Add `CopyHonorsUtf8ByteBudget`, `NoSilentClipboardTruncation`,
  `FailedCopyDoesNotCut`, `AcceptedCutDeletesExactlyOnce`,
  `LatePasteRejectsChangedSelectionRevisionFocusOrAttachment`,
  `ReadOnlyChangeInvalidatesRequest`, `NewRequestSupersedesOld`, and
  `PasteCompletionIsOneUndoGroup`. Include 65,536/65,537 UTF-8 byte boundaries,
  multibyte text, invalid UTF-8, denied/timeout/canceled/oversized responses, local
  fallback, duplicate completion, and detach/reuse of the same widget string ID.
- [x] Run WidgetClipboardTests red with fake caller completions, no terminal.
- [x] Implement bounded request capture and loop-only guarded application. Retain
  text until an accepted cut result; never query/read the clipboard at focus time.
  Reject changed targets and out-of-budget text before history/action mutation.
- [x] Add a sample adapter over released CursesSession ReadClipboardAsync and
  WriteClipboardAsync. Strictly decode UTF-8; preserve unknown failures as failures;
  keep 65,536-byte defaults and explain independent up-to-16-MiB session options.
- [x] Run fake-service, editor and full tests green; commit
  `feat: add explicit bounded clipboard requests for editing targets`.

### W1110 — Caret intent and reversible session integration

**Files:** Create `src/Core/WidgetCaretIntent.cs`,
`tests/Icod.Widgets.Tests/Editing/WidgetCaretTests.cs`,
`samples/Common/WidgetSampleCaretCoordinator.cs`; modify host presentation/editor
and `samples/Common/WidgetSampleApplication.cs` plus sample project shared-source
includes as needed; record `docs/W1110-Caret.md`.

**Interfaces:** `WidgetCaretIntent` contains visibility, screen row/column,
`CursesCursorStyle? PreferredStyle`, and focus generation. Add
`WidgetHost.GetCaretIntent(): WidgetCaretIntent?`; null means no eligible target.
Add a protected `Widget.GetCaretIntent(): WidgetCaretIntent?` default-null hook.
Sample coordinator owns optional CursesCursorStyleLease and asynchronous disposal;
the core host owns no session lease or async terminal calls.

- [x] Add `CaretMatchesUnicodeGeometry`, `HiddenOffscreenOrModalCaretIsAbsent`,
  `InsertAndOverwriteHaveDistinctStyleIntent`, `StaleStyleCompletionIsDisposed`,
  `UnsupportedObservationDoesNotEmitGuessedReset`, and `CleanupRestoresOnce`.
- [x] Run WidgetCaretTests red.
- [x] Implement clipped intent from projection and focused host scope; keep
  cursor visibility distinct from style. In the sample serialize style requests,
  revalidate focus generation, and dispose old/stale leases in valid LIFO order.
- [x] Run fake-session/output tests, screen-only tests and full suite green;
  commit `feat: expose focused caret intent and sample lease coordination`.

### W1111 — Single-line field editing parity

**Files:** Modify `src/Controls/WidgetTextInput.cs`; create
`tests/Icod.Widgets.Tests/Controls/WidgetTextInputEditingTests.cs`; extend
`samples/Icod.Widgets.Gallery.Sample/Program.cs`; record `docs/W1111-Text-Fields.md`.

**Interfaces:** Preserve constructor, Value, MaxTextElements and old actions.
Add directional UTF-16 selection access and
`SetSelection(int anchor, int caret): void`; implement IWidgetEditTarget using
the shared transaction/clipboard contracts behind a bounded single-line adapter.
Expose caret through W1110. Keep the default 4,096 and maximum 100,000 text-element
limits, existing whitespace/control normalization, and Tab traversal.

- [x] Add `SelectionReplacementAndUndo`, `PasteRemainsSingleLine`,
  `ExistingLimitsAndValueActionRemainCompatible`, `FocusedFieldWinsCopyCommand`,
  `FieldTabStillTraverses`, and `LimitReductionClampsSelectionAndInvalidatesPaste`.
  Compare old normalization fixtures with the 1.0 behavior; multiline exact paste
  and single-line normalized paste must deliberately differ.
- [x] Run WidgetTextInputEditingTests red.
- [x] Implement selection/history/commands and caret using shared primitives;
  avoid copying editor-only wrapping/indentation behavior into fields.
- [x] Run form/prompt/picker regressions and the full suite green; commit
  `feat: add selection clipboard and undo to single-line fields`.

### W1112 — Public witnesses, performance audit, compatibility, and RC

**Files:** Modify sample programs/shared loop, `samples/README.md`, root README,
`tests/PackageConsumer/Program.cs` and `.csproj`, `CHANGELOG.md`; create
`docs/Editing-Guide.md`, `docs/Public-API-Baseline-1.1.md`,
`docs/Public-API-Fingerprint-1.1.json`, `docs/W1112-RC-Qualification.md`;
extend measurement harness and add `tests/Icod.Widgets.Tests/Qualification/WidgetEditorQualificationTests.cs`.

**Interfaces:** Consumes only the public W1102–W1111 surfaces. Produces measured
workload evidence, additive API freeze, reproducible sample commands, and exact RC
artifacts. Parameterize package consumer by actual artifact version and run it
on net8.0/net9.0/net10.0; do not accidentally restore the existing 1.0 package.

- [x] Add public scenario tests for type/select/replace/undo/checkpoint, clipboard
  denial/stale completion, form/editor command scope, and repeated attach/resize/
  modal/shutdown. Include complete mixed-EOL round-trip without source normalization.
- [x] Run new acceptance tests red; then convert the current read/browse editor
  sample to real in-memory editing using public APIs. Sample file reading remains
  application code and bounded; never overwrite user files. Display modified and
  clipboard outcomes, provide F6/Shift+F6 focus escape, and keep smoke mode.
- [x] Run gallery/editor/roguelike smoke and tests, then W1101 measurements on the
  actual document/view stack. Report cold/warm performance and retained memory,
  including long lines/clusters, repeated pastes/undo, retained snapshots and trims.
  Meet recorded limits or return to the owner with evidence before loosening them.
- [x] Verify 1.0 compiled consumer and new external/custom/package-only consumers;
  compare additive public API per TFM and frozen enum/record signatures. Freeze
  reviewed APIs without deleting the historical 1.0 baseline.
- [x] Write usage/ownership/limits guidance and real sample commands; update
  package metadata and changelog only for delivered features. Run the roadmap's
  Staging/Release build-test-pack commands and artifact verifier, with CI across
  supported hosts. Confirm README external links and license/source headers.
- [x] Prepare matching `1.1.0-rc.N` project/consumer metadata after all feature
  gates pass; commit `release: qualify Widgets 1.1 editing candidate`.

### W1113 — Stable source and owner release handoff

**Files:** Modify `Icod.Widgets.csproj`, package-consumer version, README,
CHANGELOG, this roadmap/plan status; create `docs/releases/1.1.0.md` and
`docs/W1113-Stable-Qualification.md`.

**Interfaces:** Produces stable-source 1.1.0 artifact, API/package evidence,
environment-specific live record, and owner merge/tag checklist. No publication
or merge is performed by completing this task.

- [x] Run the roadmap's live scenarios on available terminals; record exact
  SHA/package/OS/architecture/terminal/runtime and Pass/Fail/Unsupported/Not tested.
  Keep unavailable macOS/Linux live environments explicit; obtain acceptance of
  this release's remaining limitations without calling CI a visual test.
- [x] Set Version and PackageVersion to 1.1.0, retain AssemblyVersion 1.0.0.0,
  DCurses 2.4.0, and align package consumer, notes, README, and baseline evidence.
- [x] Run clean matching-config build/test/pack, sample smoke, artifact validation,
  and package-only consumers on the exact stable source. Inspect nuspec and archive
  contents; ensure no alpha/rc package is selected as stable and no sample/test
  source is compiled into the library.
- [x] Verify the diff and all automated gates, record remaining limitations and
  commands/results, then commit `release: prepare Icod.Widgets 1.1.0`.
- [ ] Hand off for owner review/merge. Verify main CI and exact merged artifact
  before the owner creates v1.1.0; tag-driven NuGet/GitHub publication remains
  owner-controlled. A green planning/implementation PR alone is not publication.

## Plan self-review and handoff

Scope coverage: document/snapshot/history W1104–W1105; input/focus/public hooks
W1102–W1103; geometry/budgets W1101/W1106; editor W1107; scroll/capture W1108;
clipboard W1109; caret W1110; fields W1111; consumers/performance/release W1112–W1113.
Each Review Focus item has a named owning test above. Task interfaces share
document/view/request identities and distinguish text-state from revision.

The resource/storage/word-policy decision is deliberately gated at W1101, not
left for dependent implementers to guess. Implemented work is checked above;
the final owner merge/tag/publication handoff remains intentionally open.
