# Editor Line-Number Gutter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an opt-in, themed, noninteractive line-number gutter to `WidgetTextEditor` that remains correct under wrapping, scrolling, resizing, Unicode layout, and pointer input.

**Architecture:** `WidgetTextEditor` continues to own projection and interaction. A small internal viewport value computes the gutter and text rectangles; the editor passes only the text rectangle to projection, renders line labels from projected-row metadata, and translates caret, scrolling, and pointer coordinates through the same geometry. No projection or callback API becomes public.

**Tech Stack:** C# 13; .NET 8, 9, and 10; Icod.DCurses 2.5.2; xUnit; existing packaging and sample infrastructure.

**Spec:** `docs/superpowers/specs/2026-10-17-editor-line-number-gutter-design.md`

## Global Constraints

- Keep `Version` and `PackageVersion` at `1.7.0`, `AssemblyVersion` at `1.0.0.0`, and the Icod.DCurses dependency at `2.5.2`.
- Add only `WidgetEditorOptions.ShowLineNumbers` and the appended `WidgetStyleRole.EditorGutter` and `WidgetStyleRole.EditorGutterCurrentLine` public members.
- `ShowLineNumbers` defaults to `false`; the disabled path must preserve existing layout and input behavior.
- Use one-based, invariant ASCII decimal labels, right-aligned to document-wide digit width, followed by one spacer column.
- Suppress the gutter when it would consume the final text column; never truncate a label.
- A gutter-originated pointer sequence is decorative, while wheel scrolling and text-originated drags continue to work.
- Keep projection work bounded and do not publish projection rows, decoration callbacks, or a general gutter framework.
- Keep the root README concise; put operational detail in the editing guide/sample and full history in the changelog and curated `1.7.0` notes.
- Do not merge or publish.

## Review Focus

- A one-column or otherwise undersized editor must suppress the gutter and retain one editable column; Task 2 adds the resize/suppression test.
- Crossing 9/10 or 99/100 lines must change the inset and invalidate width-keyed projection without stale text or caret geometry; Task 2 adds the threshold-edit test.
- A press starting in the gutter and moving into text must leave selection unchanged for the whole captured sequence; Task 3 adds the capture-sequence test.
- A drag starting in text and crossing the gutter must continue selection and left autoscroll; Task 3 adds the origin-sensitive drag test.
- Direct-RGB, indexed, limited-color, and attribute-only terminals must receive supported, readable gutter styles; Task 1 extends palette capability tests.

## File Structure

- Create `src/Editing/WidgetEditorViewport.cs`: internal calculation of gutter and text rectangles.
- Modify `src/Editing/WidgetEditorOptions.cs`: opt-in public flag.
- Modify `src/Core/WidgetStyles.cs`: append the two public style roles.
- Modify `src/Core/WidgetPalette.cs`: capability-aware default styles.
- Modify `src/Controls/WidgetTextEditor.cs`: rendering, caret, scrolling, hit testing, and pointer-sequence integration.
- Modify editor/theme/caret/pointer tests under `tests/Icod.Widgets.Tests/`.
- Modify the editor sample, package consumer, concise README, editing/sample guides, changelog, curated notes, and release-identity assertions.

---

### Task 1: Public option and themed gutter roles

**Files:**
- Modify: `tests/Icod.Widgets.Tests/Core/WidgetColorThemeTests.cs`
- Modify: `src/Editing/WidgetEditorOptions.cs`
- Modify: `src/Core/WidgetStyles.cs`
- Modify: `src/Core/WidgetPalette.cs`

**Interfaces:**
- Consumes: existing `WidgetEditorOptions`, `WidgetStyleRole`, `WidgetStyles`, and `WidgetPalette.Create(CursesPresentationCapabilities, WidgetPaletteVariant)`.
- Produces: `public bool WidgetEditorOptions.ShowLineNumbers { get; init; }`, `WidgetStyleRole.EditorGutter`, and `WidgetStyleRole.EditorGutterCurrentLine`.

- [ ] **Step 1: Write the failing API and palette tests.** Add `EditorLineNumberGutterApiDefaultsOffAndCanOptIn` and `EditorLineNumberGutterPaletteIsCapabilityAware`. Assert default `false`, explicit `true`, both enum values follow `EditorCurrentLine`, the direct-RGB regular gutter uses a subdued foreground over `EditorSurface`, the current gutter background matches `EditorCurrentLine`, indexed styles use only advertised indices, and unsupported axes/attributes are removed.
- [ ] **Step 2: Run the focused tests and verify red.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --filter FullyQualifiedName~WidgetColorThemeTests`; expect compile failures for the missing option and roles.
- [ ] **Step 3: Add the public members.** Add `ShowLineNumbers` after `HighlightCurrentLine` with XML documentation; append both roles after `EditorCurrentLine` without changing prior numeric values.
- [ ] **Step 4: Add palette defaults.** Define `EditorGutter` from the palette's subdued foreground and editor background, and `EditorGutterCurrentLine` from the current-line foreground/background with supported emphasis. Route both through the existing `Style` capability filter.
- [ ] **Step 5: Run the focused tests and verify green.** Run the Step 2 command; expect all selected tests to pass on `net10.0`.
- [ ] **Step 6: Commit.** Commit the four files with `feat: define editor line-number gutter API`.

### Task 2: Shared viewport geometry and gutter rendering

**Files:**
- Create: `src/Editing/WidgetEditorViewport.cs`
- Modify: `src/Controls/WidgetTextEditor.cs`
- Modify: `tests/Icod.Widgets.Tests/Controls/WidgetTextEditorTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Core/WidgetColorThemeTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Qualification/WidgetEditorQualificationTests.cs`

**Interfaces:**
- Consumes: Task 1's option and roles; `WidgetProjectedRow.LogicalLineIndex` and `VisualLineIndex`.
- Produces: internal `WidgetEditorViewport.Create(CursesRectangle area, long lineCount, bool showLineNumbers)`, with `CursesRectangle Gutter`, `CursesRectangle Text`, and `int GutterColumns`.

- [ ] **Step 1: Write failing geometry/rendering tests.** Add `LineNumberGutterDisabledPreservesExistingTextCoordinates`, `LineNumberGutterUsesDocumentDigitWidthAndStaysFixedDuringHorizontalScroll`, `DigitThresholdReflowsTextWithoutStaleProjection`, `WrappedLineNumberAppearsOnlyOnFirstVisualRow`, `CurrentLineGutterStyleCoversWrappedRows`, `CurrentLineGutterStyleIsIndependentOfTextBand`, `DisabledEditorUsesDisabledStyleAcrossGutter`, `LineNumberGutterUsesAsciiDigitsUnderNonLatinCulture`, `LineNumberGutterSuppressesAndRestoresWhenTextNeedsTheFinalColumn`, and `LineNumberGutterWorkRemainsBoundedForLargeDocument`. Pin a ten-line document to two digit columns plus one spacer, assert text begins at column three, assert continuation rows have blank labels, prove the current gutter role applies while `HighlightCurrentLine` is false, assert Arabic culture still renders ASCII digits, assert resizing below and above the natural threshold hides and restores the gutter, and assert rendering reads/allocates only the established bounded viewport budget.
- [ ] **Step 2: Run focused rendering tests and verify red.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --filter "FullyQualifiedName~WidgetTextEditorTests|FullyQualifiedName~WidgetColorThemeTests"`; expect gutter assertions to fail while existing editor tests remain green.
- [ ] **Step 3: Implement `WidgetEditorViewport`.** Make `Create` compute invariant decimal digit width from `lineCount`, reserve one spacer, and return an empty gutter plus the original area when disabled or when reservation would leave fewer than one text column.
- [ ] **Step 4: Render through the text viewport.** In `WidgetTextEditor.OnRender`, fill the complete editor surface, project only `viewport.Text.Rows/Columns`, present text at `viewport.Text.Column`, render each first visual row's one-based label right-aligned in the digit columns, and fill all visible gutter rows with regular/current/disabled styles as specified. Keep selection last and confined to text.
- [ ] **Step 5: Use text width for scroll ranges.** Pass `viewport.Text.Columns` to `UpdateScrollRanges`; make `RefreshScrollRanges` compute horizontal page/extent from the current text viewport while keeping vertical calculations unchanged.
- [ ] **Step 6: Run focused rendering tests and verify green.** Run the Step 2 command; expect all selected tests to pass.
- [ ] **Step 7: Commit.** Commit the viewport, editor, and tests with `feat: render editor line-number gutter`.

### Task 3: Caret, pointer, and autoscroll coordinate integration

**Files:**
- Modify: `src/Controls/WidgetTextEditor.cs`
- Modify: `tests/Icod.Widgets.Tests/Editing/WidgetCaretTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Editing/WidgetEditorPointerTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Controls/WidgetTextEditorTests.cs`

**Interfaces:**
- Consumes: Task 2's `WidgetEditorViewport.Create` and text rectangle.
- Produces: no additional public API; private state tracks whether the captured primary-button sequence began in the gutter.

- [ ] **Step 1: Write failing coordinate/input tests.** Add `LineNumberGutterOffsetsCaretAndTextHitTesting`, `DecorativeGutterPressSequenceDoesNotChangeSelection`, `TextOriginatedDragCrossingGutterStillExtendsSelectionAndAutoscrolls`, `WheelOverGutterStillScrolls`, and `HorizontalScrollRangeUsesTextViewportWidth`. Assert a gutter click followed by move/release in text preserves anchor/caret; a text-originated drag through the gutter still extends; and caret/text clicks are shifted by exactly `GutterColumns`.
- [ ] **Step 2: Run focused tests and verify red.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --filter "FullyQualifiedName~WidgetCaretTests|FullyQualifiedName~WidgetEditorPointerTests|FullyQualifiedName~WidgetTextEditorTests"`; expect new coordinate and gutter-sequence assertions to fail.
- [ ] **Step 3: Translate caret and movement projection.** Use the text viewport for `GetCaretIntent`, `MoveVertical`, pointer projection, and horizontal page-size calculations; add the text viewport column to the projected caret's screen column.
- [ ] **Step 4: Implement origin-sensitive pointer handling.** Process wheel gestures first. On a primary press in the gutter, mark the gutter-originated sequence and consume it without selection. Consume its moves/releases until release or capture cancellation. Preserve the existing `pointerSelecting` path for drags that began in text, using the text rectangle's left/right edges for horizontal autoscroll.
- [ ] **Step 5: Clear gutter pointer state on every cancellation path.** Reset it from release, `OnCaptureCanceled`, detach, modal/focus cancellation, and resize-driven capture cancellation alongside existing selection-drag state.
- [ ] **Step 6: Run focused tests and verify green.** Run the Step 2 command; expect all selected tests to pass.
- [ ] **Step 7: Run the complete unit suite.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0`; expect zero failures.
- [ ] **Step 8: Commit.** Commit editor and tests with `fix: align editor input with line-number gutter`.

### Task 4: Sample, package consumer, and release documentation

**Files:**
- Modify: `samples/Icod.Widgets.Editor.Sample/Program.cs`
- Modify: `samples/Icod.Widgets.Editor.Sample/README.md`
- Modify: `samples/README.md`
- Modify: `tests/PackageConsumer/Program.cs`
- Modify: `tests/Icod.Widgets.Tests/Core/ReleaseIdentityTests.cs`
- Modify: `README.md`
- Modify: `docs/Editing-Guide.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/releases/1.7.0.md`

**Interfaces:**
- Consumes: Tasks 1-3's complete public feature.
- Produces: a live example and package-only compile/runtime proof of `ShowLineNumbers`, `EditorGutter`, and `EditorGutterCurrentLine`.

- [ ] **Step 1: Write failing release/package assertions.** Extend `CandidateHasCuratedCurrentLineNotes` to require `ShowLineNumbers`, both gutter roles, decorative pointer behavior, wrapping behavior, and the no-general-decoration non-goal. Extend the package consumer to construct an editor with both options and resolve custom styles for both roles.
- [ ] **Step 2: Run source tests and verify red documentation assertions.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --filter FullyQualifiedName~ReleaseIdentityTests`; expect the new release-note assertions to fail.
- [ ] **Step 3: Enable the sample.** Set `ShowLineNumbers = true` beside `HighlightCurrentLine = true`; extend its smoke/live checks so line labels, wrap continuation, resize suppression, text clicks, decorative gutter clicks, and current-line movement are demonstrable.
- [ ] **Step 4: Update documentation.** Add one concise README sentence; document geometry, theme roles, wrapping, pointer behavior, and narrow suppression in the editing guide and editor sample README; add a complete `1.7.0` changelog bullet and curated release-note section. Do not add version history to the README.
- [ ] **Step 5: Run release assertions and verify green.** Run the Step 2 command; expect all selected tests to pass.
- [ ] **Step 6: Commit.** Commit sample, consumer, tests, and documentation with `docs: qualify editor line-number gutter`.

### Task 5: Cross-target, package, and live qualification

**Files:**
- Modify only if evidence is recorded: `docs/releases/1.7.0.md`
- Modify only if existing workflow expectations require it: PR description

**Interfaces:**
- Consumes: complete Widgets implementation from Tasks 1-4.
- Produces: a green, unmerged `1.7.0` PR ready for owner testing and publication.

- [ ] **Step 1: Run repository formatting and diff checks.** Run `git diff --check`; expect no errors and confirm no unrelated files changed.
- [ ] **Step 2: Run all target-framework tests.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release`; expect zero failures on .NET 8, 9, and 10.
- [ ] **Step 3: Run the complete local build/package pipeline.** Run `build.cmd` on Windows or `./build.sh` on Unix-like systems; expect clean restore/build/test/sample-smoke/pack/validation and exact `Icod.Widgets.1.7.0` package acceptance.
- [ ] **Step 4: Run the package-only consumer.** Use the repository's generated local NuGet configuration and package-consumer command from the build output; expect compilation and exit code zero using only the packed `1.7.0` API.
- [ ] **Step 5: Exercise the editor sample.** Run `dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0`; verify line numbers remain fixed, wrapped continuations are blank, current-line styling follows the caret, selection stays dominant, text clicks remain accurate, gutter clicks are decorative, and narrow resize restores the text column.
- [ ] **Step 6: Push every completed commit to PR #15 and wait for CI.** Require Linux, Windows, and macOS build/test/sample/package jobs to pass; record the run URL and exact test/package evidence in the PR description or curated notes.
- [ ] **Step 7: Commit any evidence-only documentation update.** If Step 6 adds evidence, commit it as `docs: record line-number gutter qualification`, push, and require the resulting CI run to pass. Do not merge or publish.
