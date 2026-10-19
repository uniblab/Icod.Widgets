# Editor Scrollbar and Viewport Scrolling Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an opt-in, auto-hidden, themed vertical editor scrollbar and displayed-row viewport scrolling with caret edge clamping under word wrap.

**Architecture:** `WidgetTextEditor` remains the single owner of projection and input. An internal logical-line/wrapped-row anchor and bounded row map extend the existing view/projection model; shared scrollbar geometry gives the standalone control and integrated editor chrome the same vertical glyphs and pointer semantics. Dedit later consumes the published option without owning scrolling logic.

**Tech Stack:** C# 13; .NET 8, 9, and 10; Icod.DCurses 2.5.2; xUnit; existing sample, API-fingerprint, and packaging infrastructure.

**Spec:** `docs/superpowers/specs/2026-10-19-editor-scrollbar-and-viewport-scrolling-design.md`

## Global Constraints

- Release as Icod.Widgets `1.9.0`; preserve `AssemblyVersion` `1.0.0.0`, Icod.DCurses `2.5.2`, AnyCPU, and the .NET 8/9/10 targets.
- Add only `public bool WidgetEditorOptions.ShowVerticalScrollBar { get; init; }`; keep projection rows, viewport anchors, and row maps internal.
- Keep `WidgetTextViewState.VerticalOffset` and public `SetScroll(long, long)` logical-line based; `SetScroll` resets the wrapped-row component to zero.
- Use `▲`, `▼`, `│`, and `┃` for vertical scrollbar chrome and the existing `ScrollTrack`, `ScrollThumb`, `ScrollFocusedThumb`, and `Disabled` roles.
- Hide the integrated scrollbar when the document fits at full editor width or when fewer than two rows are available.
- Measure every vertical input in displayed rows; wheel input retains its existing three-row step.
- Clamp the caret for Ctrl, wheel, arrow-button, track, and thumb scrolling only when it would leave the viewport; preserve preferred visual column and collapse selection only when clamping moves the caret.
- Keep projection and row-map work bounded by existing editor limits; do not scan or allocate for every document row in one operation.
- Keep the root README concise; put operational detail in the editor sample guide and full history in the changelog and curated `1.9.0` notes.
- Do not merge or publish.

## Review Focus

- A document fitting at full width but wrapping after a scrollbar column is reserved must remain scrollbar-free without visibility oscillation; Task 3 adds the full-width probe test.
- A single logical line spanning more rows than the viewport must support exact one-row stepping in both directions without rereading the entire line; Task 2 adds long wrapped-line tests and bounded-work assertions.
- Edits before a cached viewport anchor must invalidate prefix totals and preserve a valid visible anchor; Task 2 adds edit/undo/redo invalidation tests.
- Thumb dragging across an inexact wrapped range must stay monotonic and reach exact document boundaries at both limits; Task 4 adds range-refinement and endpoint tests.
- Pointer capture interrupted by focus loss, modal entry, detach, or resize must not leave the editor in drag state; Task 4 adds cancellation-path tests.

## File Structure

- Modify `src/Editing/WidgetEditorOptions.cs`: additive public opt-in.
- Create `src/Editing/WidgetTextViewportAnchor.cs`: internal logical-line and wrapped-row value.
- Create `src/Editing/WidgetTextRowMap.cs`: bounded, width-keyed displayed-row counts and prefix estimates.
- Modify `src/Editing/WidgetTextViewState.cs`: retain the internal wrapped-row component while preserving public logical offsets.
- Modify `src/Editing/WidgetTextProjection.cs`: project from a wrapped-row anchor and resolve nearby displayed-row movement.
- Create `src/Controls/WidgetScrollBarGeometry.cs`: shared button, track, thumb, range, and hit-test calculations.
- Modify `src/Controls/WidgetScrollBar.cs`: standard vertical glyphs and step buttons through shared geometry.
- Modify `src/Editing/WidgetEditorViewport.cs`: optional right scrollbar rectangle in addition to gutter/text rectangles.
- Modify `src/Controls/WidgetTextEditor.cs`: auto-hide, render, input, drag capture, range synchronization, and caret edge clamping.
- Extend editor, projection, caret, pointer, scrollbar, qualification, palette, release, API, sample, and package-consumer tests.
- Update `Icod.Widgets.csproj`, concise README material, editor/sample guides, `CHANGELOG.md`, `ROADMAP.md`, curated notes, and public API records.

---

### Task 1: Public option and shared vertical scrollbar chrome

**Files:**
- Modify: `src/Editing/WidgetEditorOptions.cs`
- Create: `src/Controls/WidgetScrollBarGeometry.cs`
- Modify: `src/Controls/WidgetScrollBar.cs`
- Modify: `tests/Icod.Widgets.Tests/Controls/WidgetScrollBarTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Core/WidgetColorThemeTests.cs`

**Interfaces:**
- Consumes: existing `WidgetScrollRange`, `WidgetScrollOrientation`, `WidgetStyleRole.ScrollTrack`, `ScrollThumb`, and `ScrollFocusedThumb`.
- Produces: `public bool WidgetEditorOptions.ShowVerticalScrollBar { get; init; }`; internal `WidgetScrollBarGeometry.CreateVertical(int length, long extent, long viewport, long offset)` with button/track/thumb coordinates and pointer target helpers.

- [ ] **Step 1: Write the failing option and vertical-chrome tests.** Add `VerticalScrollbarOptionDefaultsOffAndCanOptIn`, `VerticalScrollbarRendersArrowTrackAndThumbGlyphs`, `ArrowButtonsStepOneUnit`, `TrackClicksPage`, `VerticalScrollbarUsesExistingThemeRoles`, and `TwoRowVerticalScrollbarRendersBothButtonsWithoutTrack`. Assert exact `▲`, `▼`, `│`, and `┃` cells and unchanged horizontal rendering.
- [ ] **Step 2: Run focused tests and verify red.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --filter "FullyQualifiedName~WidgetScrollBarTests|FullyQualifiedName~WidgetColorThemeTests"`; expect compile failure for the option and rendering/input failures for the old vertical bar.
- [ ] **Step 3: Add `ShowVerticalScrollBar`.** Add the documented Boolean property after `ShowLineNumbers`; leave its default `false` and existing validation unchanged.
- [ ] **Step 4: Implement shared vertical geometry.** Make `CreateVertical` reserve row zero and the final row for buttons when length is at least two, expose the interior track, scale a minimum-one-cell thumb with `BigInteger`, and classify pointer coordinates as decrement button, increment button, page-before, thumb, or page-after.
- [ ] **Step 5: Refactor `WidgetScrollBar` vertical rendering/input.** Render the four specified glyphs, make button presses step exactly one range unit, keep track paging and thumb drag saturation, suppress vertical chrome below two rows, and leave horizontal behavior unchanged.
- [ ] **Step 6: Run focused tests and verify green.** Run the Step 2 command; expect all selected tests to pass.
- [ ] **Step 7: Commit.** Commit the option, geometry, control, and tests with `feat: define editor scrollbar chrome`.

### Task 2: Wrapped-row viewport anchor and bounded row map

**Files:**
- Create: `src/Editing/WidgetTextViewportAnchor.cs`
- Create: `src/Editing/WidgetTextRowMap.cs`
- Modify: `src/Editing/WidgetTextViewState.cs`
- Modify: `src/Editing/WidgetTextProjection.cs`
- Modify: `tests/Icod.Widgets.Tests/Editing/WidgetTextProjectionTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Controls/WidgetTextEditorTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Qualification/WidgetEditorQualificationTests.cs`

**Interfaces:**
- Consumes: document line APIs, `CursesTextLayout`, existing projection cache keys, and `WidgetEditorOptions` work/cache limits.
- Produces: internal `WidgetTextViewportAnchor(long LogicalLineIndex, int VisualRowIndex)`; `WidgetTextViewState.ViewportAnchor`; `void WidgetTextViewState.SetViewport(long horizontalOffset, WidgetTextViewportAnchor anchor)`; `WidgetTextViewportAnchor WidgetTextRowMap.Move(WidgetTextViewState view, WidgetTextViewportAnchor origin, long displayedRows, ICursesTextWidthProvider widthProvider, int columns)`; and `WidgetScrollRange WidgetTextRowMap.GetRange(WidgetTextViewState view, WidgetTextViewportAnchor origin, ICursesTextWidthProvider widthProvider, int columns, int viewportRows)`.

- [ ] **Step 1: Write failing viewport-anchor tests.** Add `PublicSetScrollStartsAtFirstWrappedRow`, `ProjectionStartsAtWrappedContinuation`, `MoveViewportAdvancesOneDisplayedRowWithinLogicalLine`, `MoveViewportRetreatsAcrossLogicalBoundary`, `NoWrapAnchorAlwaysUsesVisualRowZero`, and `WrappedScrollRangeUsesDisplayedRowsAndReportsExactness`.
- [ ] **Step 2: Write failing invalidation and bounded-work tests.** Add `RowMapRekeysAfterWidthWrapTabAndProviderChanges`, `EditUndoAndRedoInvalidateAffectedPrefix`, `MillionUnitWrappedLineScrollUsesBoundedWork`, and `LargeDocumentDoesNotAllocateOneEntryPerLine`. Assert the established source-unit/cache/work limits rather than a whole-document scan.
- [ ] **Step 3: Run focused tests and verify red.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --filter "FullyQualifiedName~WidgetTextProjectionTests|FullyQualifiedName~WidgetTextEditorTests|FullyQualifiedName~WidgetEditorQualificationTests"`; expect missing anchor/map members and wrapped-row assertions to fail.
- [ ] **Step 4: Add the internal viewport anchor to view state.** Keep public `VerticalOffset` equal to `ViewportAnchor.LogicalLineIndex`; make public `SetScroll` validate as before and reset `VisualRowIndex` to zero; make internal `SetViewport` validate both components and increment `ViewRevision` only on change.
- [ ] **Step 5: Implement the bounded row map.** Cache measured displayed-row counts by document revision and layout key, retain conservative one-row estimates for unknown lines, invalidate changed prefixes, refine extent/offset monotonically, and resolve absolute beginning/end exactly without unchecked arithmetic.
- [ ] **Step 6: Project from the viewport anchor.** Skip `VisualRowIndex` rows only in the first logical line, continue across lines until the viewport is full, and preserve projected logical/visual metadata, caret hit testing, selections, continuation state, and source-read budgets.
- [ ] **Step 7: Implement displayed-row movement.** Make `Move` traverse measured wrapped rows forward/backward, request only the local measurements required, clamp at document boundaries, and return the unchanged anchor when no movement is possible.
- [ ] **Step 8: Run focused tests and verify green.** Run the Step 3 command; expect all selected tests to pass.
- [ ] **Step 9: Commit.** Commit the anchor, map, projection, view state, and tests with `feat: scroll editor by displayed rows`.

### Task 3: Integrated auto-hidden editor scrollbar

**Files:**
- Modify: `src/Editing/WidgetEditorViewport.cs`
- Modify: `src/Controls/WidgetTextEditor.cs`
- Modify: `tests/Icod.Widgets.Tests/Controls/WidgetTextEditorTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Core/WidgetColorThemeTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Editing/WidgetCaretTests.cs`

**Interfaces:**
- Consumes: Task 1's option/geometry and Task 2's viewport anchor/row map.
- Produces: `WidgetEditorViewport.Create(CursesRectangle area, long lineCount, bool showLineNumbers, bool showVerticalScrollBar)` with `Gutter`, `Text`, and `VerticalScrollBar`; synchronized `WidgetTextEditor.VerticalScrollRange` in displayed-row units.

- [ ] **Step 1: Write failing visibility and geometry tests.** Add `FittingDocumentDoesNotRenderOrReserveScrollbar`, `OverflowingDocumentUsesRightmostColumn`, `FullWidthFitProbeDoesNotOscillateWhenReservedColumnWouldWrap`, `EditAndResizeHideAndRestoreScrollbar`, `ScrollbarComposesWithLeftLineNumberGutter`, `TwoRowEditorShowsOnlyArrowButtons`, and `OneRowEditorSuppressesScrollbar`.
- [ ] **Step 2: Write failing rendering/caret tests.** Add `IntegratedScrollbarUsesExactGlyphsAndThemeRoles`, `SelectionAndCurrentLineDoNotPaintScrollbar`, `CaretCoordinatesUseReducedTextViewport`, and `DisabledEditorUsesDisabledScrollbarStyle`.
- [ ] **Step 3: Run focused tests and verify red.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --filter "FullyQualifiedName~WidgetTextEditorTests|FullyQualifiedName~WidgetColorThemeTests|FullyQualifiedName~WidgetCaretTests"`; expect missing viewport geometry and absent scrollbar cells.
- [ ] **Step 4: Extend viewport geometry.** Probe fit at full post-gutter text width, reserve the extreme-right column only when overflow is proven or still conservatively unresolved, and return a stable middle text rectangle without consuming the final text column.
- [ ] **Step 5: Render integrated scrollbar chrome.** Fill/render text and gutter only inside their rectangles, render shared scrollbar geometry last in its rectangle, and resolve track/thumb/focused/disabled styles through the editor's existing theme/scoped-style chain.
- [ ] **Step 6: Synchronize scroll ranges.** Publish displayed-row extent, viewport, and offset from the row map after projection; keep no-wrap values exact and clamp stale anchors after document/layout changes.
- [ ] **Step 7: Run focused tests and verify green.** Run the Step 3 command; expect all selected tests to pass.
- [ ] **Step 8: Commit.** Commit viewport/editor integration and tests with `feat: add auto-hidden editor scrollbar`.

### Task 4: Scroll input and caret edge clamping

**Files:**
- Modify: `src/Controls/WidgetTextEditor.cs`
- Modify: `tests/Icod.Widgets.Tests/Editing/WidgetEditorPointerTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Editing/WidgetCaretTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Controls/WidgetTextEditorTests.cs`

**Interfaces:**
- Consumes: Tasks 1-3 geometry, displayed-row movement, projection, and scroll ranges.
- Produces: private editor scroll routes for relative/page/proportional movement and caret clamping; no additional public API.

- [ ] **Step 1: Write failing Ctrl-scroll tests.** Add `ControlDownScrollsOneDisplayedRowWithoutMovingVisibleCaret`, `ControlDownPullsCaretAlongTopEdge`, `ControlUpPullsCaretAlongBottomEdge`, `ControlScrollPreservesPreferredVisualColumnAcrossWrappedRows`, `ControlShiftDoesNotExtendSelection`, and `OrdinaryVerticalMovementRemainsCaretNavigation`.
- [ ] **Step 2: Write failing pointer-scroll tests.** Add `WheelScrollsThreeDisplayedRowsAndClampsCaret`, `ArrowClicksScrollOneDisplayedRow`, `TrackClicksPageAndClampCaret`, `ThumbDragUsesDisplayedRowsAndReachesBothBoundaries`, `InexactThumbMovementIsMonotonic`, `CaretClampCollapsesSelectionAndEmitsOneSelectionAction`, and `VisibleCaretProducesNoSelectionAction`.
- [ ] **Step 3: Write failing capture/capacity tests.** Add `ScrollbarDragCancelsOnReleaseFocusLossDetachModalAndResize`, `RapidWheelAndDragStayInRange`, and `SelectionActionCapacityIsReservedBeforeCaretClamp`.
- [ ] **Step 4: Run focused tests and verify red.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --filter "FullyQualifiedName~WidgetEditorPointerTests|FullyQualifiedName~WidgetCaretTests|FullyQualifiedName~WidgetTextEditorTests"`; expect Ctrl keys to move the caret and scrollbar pointer tests to be unhandled.
- [ ] **Step 5: Route Ctrl and wheel input through displayed-row scrolling.** Recognize Control with no Alt before ordinary Up/Down handling, ignore Shift for the scroll command, keep the wheel's three-row step, and consume boundary attempts without changing caret or selection.
- [ ] **Step 6: Implement integrated scrollbar pointer handling.** Process arrow presses, track page presses, thumb press/move/release, pointer capture, saturation, and cancellation through shared geometry without focusing a separate child widget.
- [ ] **Step 7: Clamp the caret after every vertical scroll route.** Reproject the destination viewport; leave visible caret/selection state untouched, otherwise hit-test the top row when moving toward the end or bottom row when moving toward the beginning at `PreferredColumn`, collapse selection, preserve preferred column/affinity, reserve action capacity, and emit one `WidgetTextSelectionChangedAction`.
- [ ] **Step 8: Run focused tests and verify green.** Run the Step 4 command; expect all selected tests to pass.
- [ ] **Step 9: Commit.** Commit input, clamping, capture, and tests with `feat: clamp caret during viewport scrolling`.

### Task 5: Sample, public contract, and release documentation

**Files:**
- Modify: `samples/Icod.Widgets.Editor.Sample/Program.cs`
- Modify: `samples/Icod.Widgets.Editor.Sample/README.md`
- Modify: `samples/README.md`
- Modify: `README.md`
- Modify: `ROADMAP.md`
- Modify: `CHANGELOG.md`
- Modify: `Icod.Widgets.csproj`
- Modify: `tests/PackageConsumer/Program.cs`
- Modify: `tests/PackageConsumer/PackageConsumer.csproj`
- Modify: `tests/Icod.Widgets.Tests/Core/ReleaseIdentityTests.cs`
- Create: `docs/releases/1.9.0.md`
- Create: `docs/Public-API-Baseline-1.9.md`
- Create: `docs/Public-API-Fingerprint-1.9.json`

**Interfaces:**
- Consumes: Tasks 1-4 completed behavior and `WidgetEditorOptions.ShowVerticalScrollBar`.
- Produces: Icod.Widgets 1.9.0 package identity, curated notes, reproducible API record, and an executable consumer/sample contract.

- [ ] **Step 1: Write failing release/sample assertions.** Update release identity tests to require version/package `1.9.0`, AssemblyVersion `1.0.0.0`, DCurses `2.5.2`, curated notes containing scrollbar, auto-hide, wrapping, Ctrl, wheel, caret clamp, pointer, glyph, theme, and verification coverage, concise README option documentation without release history, and editor-sample instructions for live scrollbar qualification.
- [ ] **Step 2: Extend package-consumer and sample smoke tests.** Compile a consumer with `ShowVerticalScrollBar = true`; make the editor sample demonstrate fitting/overflowing documents, wrapped-row Ctrl scrolling, wheel clamping, arrow clicks, auto-hide, line-number gutter composition, and resize while preserving its existing editing/clipboard checks.
- [ ] **Step 3: Run release and sample tests and verify red.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --filter FullyQualifiedName~ReleaseIdentityTests` and `dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0 -- --smoke`; expect old identity/documentation and missing smoke behavior to fail.
- [ ] **Step 4: Update package identity and curated notes.** Set `Version` and `PackageVersion` to `1.9.0`, keep `AssemblyVersion` and dependencies fixed, write concise package release notes, add the curated release document, and record the feature in changelog/roadmap.
- [ ] **Step 5: Update concise and operational documentation.** Add one README option/example section without version history; put key-by-key, mouse, wrapping, glyph, theme, compact-layout, and auto-hide details in the editor/sample guides.
- [ ] **Step 6: Generate and verify public API records.** Generate `docs/Public-API-Fingerprint-1.9.json`, write the matching baseline summary, and assert that the only new public member is `WidgetEditorOptions.ShowVerticalScrollBar` while all 1.8 members remain.
- [ ] **Step 7: Run release and sample tests and verify green.** Run the Step 3 commands; expect both to pass.
- [ ] **Step 8: Commit.** Commit release identity, sample, documentation, API records, and tests with `docs: prepare Widgets 1.9.0`.

### Task 6: Full qualification and handoff

**Files:**
- Modify: `docs/releases/1.9.0.md`
- Modify: `docs/superpowers/plans/2026-10-19-editor-scrollbar-and-viewport-scrolling.md`

**Interfaces:**
- Consumes: complete Tasks 1-5 implementation and repository build scripts.
- Produces: independently verifiable Widgets 1.9.0 candidate and exact Dedit integration contract.

- [ ] **Step 1: Run the complete local matrix.** Run the repository build/pack/validate flow for Debug and Release plus all tests for net8.0, net9.0, and net10.0 in single-node mode where required; require zero failures and exact package validation.
- [ ] **Step 2: Run samples and compatibility consumers.** Run editor and gallery smoke modes, package consumer, binary compatibility consumer, API compatibility checks, and release-note validation; require zero failures.
- [ ] **Step 3: Inspect package contents and metadata.** Verify one DLL package plus symbols package, exact version `1.9.0`, AssemblyVersion `1.0.0.0`, README/license/icon/source-link metadata, dependency `Icod.DCurses 2.5.2`, and no build/test artifacts.
- [ ] **Step 4: Push and inspect CI without merging.** Push every completed tranche to the feature PR, wait for Linux/macOS/Windows jobs and package validation, and correct any candidate-only failures.
- [ ] **Step 5: Curate final verification evidence.** Record exact per-framework test counts, sample outcomes, package lifecycle results, CI run, commit, and the follow-on Dedit contract in `docs/releases/1.9.0.md`.
- [ ] **Step 6: Mark the plan complete and commit.** Replace every remaining checkbox with completed state, add `**Status:** Complete`, and commit with `docs: complete Widgets 1.9.0 handoff`.
