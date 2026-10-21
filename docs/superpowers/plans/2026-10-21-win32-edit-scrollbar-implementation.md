# Win32-Inspired Editor Scrollbars Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship Icod.Widgets 1.10.0 with one Win32-inspired scrollbar behavior shared by standalone horizontal/vertical controls and auto-hidden editor-owned bars.

**Architecture:** Replace the separate vertical and horizontal calculations with an axis-neutral geometry and interaction core. Keep `WidgetTextEditor` as the sole focus owner while it renders and applies the shared scrollbar commands; use auxiliary `WidgetPopupMenu` instances for themed context menus, and use a bounded cache to discover no-wrap horizontal extent.

**Tech Stack:** C# 13, .NET 8/9/10, xUnit, Icod.DCurses 2.5.2, PowerShell packaging scripts.

**Spec:** `docs/superpowers/specs/2026-10-21-win32-edit-scrollbar-design.md`

## Global Constraints

- Preserve `AssemblyVersion` `1.0.0.0`, Icod.DCurses `2.5.2`, and the net8.0/net9.0/net10.0 target set.
- Keep `ShowVerticalScrollBar` and the existing `WidgetScrollBar` constructor source/binary compatible; new editor options default to `false`.
- Do not change Icod.Terminal or Icod.DCurses; the dedicated Menu/Apps key remains out of scope.
- Use terminal display columns and grapheme boundaries, not UTF-16 indices, for horizontal behavior.
- Preserve bounded work (`MaximumWorkPerUpdate`) and overflow-safe `long` arithmetic.
- Keep the root README concise; complete history belongs in `CHANGELOG.md` and `docs/releases/1.10.0.md`.
- Do not merge, publish, or update Icod.Dedit from this branch.
- Local RED/GREEN cycles run net10.0 because this container lacks the .NET 8/9 runtimes; build all targets locally and require all-target tests in CI.

## Review Focus

- A range or editor layout changing during drag/repeat/context-menu display must cancel safely without stale capture or mutation (Task 2 and Task 3 tests).
- Unicode wide/combining text, tabs, and extreme `long` ranges must preserve display-column and overflow-safe mapping (Task 1 and Task 5 tests).
- Adding one editor bar can create overflow on the other axis; layout must converge without visibility oscillation (Task 4 tests).
- Inexact horizontal discovery must never hide a nonzero offset or block input, and must become exact without unbounded work (Task 5 tests).
- Popup clipping, dismissal, focus restoration, disabled commands, and action capacity must remain correct at screen edges and lifecycle boundaries (Task 3 tests).

---

### Task 1: Axis-Neutral Geometry and Symmetric Rendering

**Files:**
- Modify: `src/Controls/WidgetScrollBarGeometry.cs`
- Modify: `src/Controls/WidgetScrollBar.cs`
- Modify: `tests/Icod.Widgets.Tests/Controls/WidgetScrollBarTests.cs`

**Interfaces:**
- Produces: `WidgetScrollBarGeometry.Create( int length, long extent, long viewport, long offset )`.
- Produces: `WidgetScrollBarGeometry.OffsetForDrag( int originCoordinate, long originOffset, int coordinate )`.
- Produces: identical button/track/thumb hit targets for either orientation; orientation remains a rendering/input-axis concern of `WidgetScrollBar`.
- Consumes: existing `WidgetScrollRange`, `WidgetScrollOrientation`, and `WidgetScrollChangedAction` contracts.

- [ ] **Step 1: Write failing geometry and rendering tests**

  Add focused tests named `GeometryIsAxisNeutralForShortAndNormalTracks`, `HorizontalScrollbarRendersButtonsTrackAndThumb`, and `BothOrientationsMapLongMaxWithoutOverflow`. Assert lengths 0/1 are inert, length 2 has two buttons, `◄/─/━/►` render horizontally, `▲/│/┃/▼` remain vertical, and drag mapping reaches both boundaries.

- [ ] **Step 2: Run the focused tests and verify RED**

  Run: `DOTNET_CLI_USE_MSBUILD_SERVER=0 /workspace/scratch/f0e348930601/.dotnet/dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --no-restore -m:1 --disable-build-servers --filter FullyQualifiedName~WidgetScrollBarTests`

  Expected: FAIL because `Create` and horizontal end-button rendering do not exist.

- [ ] **Step 3: Implement the minimal shared geometry and renderer**

  Replace `CreateVertical` with `Create`; reserve end cells uniformly and use `BigInteger` scaling. Delete the separate horizontal thumb math from `WidgetScrollBar` and render both axes from shared geometry with the approved glyphs.

- [ ] **Step 4: Run focused tests and the full net10.0 suite**

  Expected: focused tests PASS; full suite reports 0 failures and no warnings.

- [ ] **Step 5: Commit**

  Commit: `feat: unify scrollbar geometry and rendering`

---

### Task 2: Shared Direct Interaction, Hold Repeat, and Visual States

**Files:**
- Create: `src/Controls/WidgetScrollBarInteraction.cs`
- Modify: `src/Controls/WidgetScrollBar.cs`
- Modify: `src/Core/WidgetStyles.cs`
- Modify: `src/Core/WidgetTheme.cs`
- Modify: `src/Core/WidgetPalette.cs`
- Modify: `tests/Icod.Widgets.Tests/Controls/WidgetScrollBarTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Core/WidgetColorThemeTests.cs`

**Interfaces:**
- Consumes: Task 1 `WidgetScrollBarGeometry.Create` and drag mapping.
- Produces: internal semantic commands `UnitDecrement`, `UnitIncrement`, `PageDecrement`, `PageIncrement`, `Start`, `End`, and `ThumbTrack`.
- Produces: internal interaction operations for press/move/release/update/cancel and hover/pressed target state.
- Produces: new optional style roles for scroll buttons, hover, pressed state, and corner, each resolving through an existing-role fallback.

- [ ] **Step 1: Write failing direct-input and keyboard tests**

  Add tests `ModifiedTrackClicksPageLikeUnmodifiedTrackClicks`, `DirectionalKeyPairsMatchWin32Vocabulary`, `TrackAndButtonsUseSameSemanticCommandsOnBothAxes`, and `BoundaryCommandsAreHandledWithoutActions`. Assert track clicks page regardless of key modifiers, and both Up/Left and Down/Right work on a focused standalone bar.

- [ ] **Step 2: Run focused tests and verify RED**

  Expected: FAIL on cross-axis directional keys and any modifier-specific track behavior.

- [ ] **Step 3: Implement semantic commands and direct interaction**

  Create the state/command types and route standalone key, wheel, click, and drag paths through them. Reserve action capacity before offset mutation and suppress redundant actions at boundaries.

- [ ] **Step 4: Write failing hold-repeat and cancellation tests**

  Add `ButtonHoldRepeatsAfterDelayAtBoundedCadence`, `TrackHoldStopsWhenThumbReachesPointer`, and `RepeatCancelsAcrossReleaseFocusArrangeModalDisableAndDetach`. Assert no repeat before 400 ms, 50 ms cadence afterward, and at most `MaximumWorkPerUpdate` catch-up steps.

- [ ] **Step 5: Run repeat tests and verify RED**

  Expected: FAIL because `OnUpdate` does not repeat scrollbar operations.

- [ ] **Step 6: Implement minimal repeat lifecycle**

  Use host monotonic time, the approved 400 ms/50 ms timing, target re-hit for track stopping, and every cancellation route from the spec.

- [ ] **Step 7: Write failing theme-state tests, then implement roles and fallbacks**

  Tests assert existing themes resolve all new roles, custom roles alter only presentation, and hover/pressed styles do not change offsets or hit targets. Implement palette styles and renderer state selection after observing RED.

- [ ] **Step 8: Run focused tests and full net10.0 suite**

  Expected: 0 failures and no warnings.

- [ ] **Step 9: Commit**

  Commit: `feat: add Win32 scrollbar interaction states`

---

### Task 3: Anchored Popup Menus and Scrollbar Context Commands

**Files:**
- Modify: `src/Menus/WidgetPopupMenu.cs`
- Modify: `src/Menus/WidgetPopupMenu.Rendering.cs`
- Modify: `src/Core/WidgetHost.Popups.cs`
- Modify: `src/Controls/WidgetScrollBar.cs`
- Create: `tests/Icod.Widgets.Tests/Menus/WidgetAnchoredPopupMenuTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Controls/WidgetScrollBarTests.cs`

**Interfaces:**
- Consumes: Task 2 semantic command application and target enablement.
- Produces: public `WidgetPopupMenu.OpenAt( int row, int column )`.
- Produces: an internal popup command sink used by built-in control menus; consumer menus keep existing action dispatch.
- Produces: private auxiliary menu ownership that attaches/detaches with its scrollbar and does not become a persistent layout child or tab stop.

- [ ] **Step 1: Write failing anchored-popup tests**

  Add `OpenAtMeasuresAndClampsRootPopup`, `AnchoredPopupRetainsKeyboardPointerAndDismissalBehavior`, and `ExistingOpenUsesArrangedBoundsUnchanged`. Test all four screen edges and focus restoration.

- [ ] **Step 2: Run popup tests and verify RED**

  Expected: FAIL because `OpenAt` does not exist.

- [ ] **Step 3: Implement anchored popup geometry without changing existing `Open()`**

  Add anchor state, content-measured root bounds, screen clamping, and cleanup on close. Keep submenu overlay rules and existing bounds-based opening intact.

- [ ] **Step 4: Write failing scrollbar context-menu tests**

  Add `SecondaryReleaseOpensOrientationSpecificCommands`, `ContextCommandsUseSharedScrollBehavior`, `ContextItemsDisableAtBoundaries`, `ShiftF10AnchorsStandaloneMenuAtThumb`, and `ContextSequenceCancelsAcrossLifecycleChanges`. Assert exact captions/order/separators and shared command behavior.

- [ ] **Step 5: Run context tests and verify RED**

  Expected: FAIL because scrollbars have no context menu or internal sink.

- [ ] **Step 6: Implement auxiliary popup ownership and command routing**

  Add orientation-specific item definitions, secondary press/release sequencing, Shift+F10, disabled-state snapshots, and command sink dispatch through Task 2 semantics. Ensure close restores the scrollbar and emits no application command.

- [ ] **Step 7: Run menu/scrollbar tests and full net10.0 suite**

  Expected: 0 failures and no warnings.

- [ ] **Step 8: Commit**

  Commit: `feat: add scrollbar context menus`

---

### Task 4: Two-Axis Editor Viewport and Integrated Scrollbars

**Files:**
- Modify: `src/Editing/WidgetEditorOptions.cs`
- Modify: `src/Editing/WidgetEditorViewport.cs`
- Modify: `src/Controls/WidgetTextEditor.cs`
- Modify: `tests/Icod.Widgets.Tests/Editing/WidgetEditorPointerTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Controls/WidgetTextEditorTests.cs`

**Interfaces:**
- Consumes: Task 1 geometry/rendering and Task 2 semantic interaction operations.
- Produces: public `WidgetEditorOptions.ShowHorizontalScrollBar` defaulting to `false`.
- Produces: viewport rectangles `Gutter`, `Text`, `VerticalScrollBar`, `HorizontalScrollBar`, and `ScrollCorner`.
- Produces: monotonic fixed-point editor layout and integrated bar hit testing/rendering for both axes.

- [ ] **Step 1: Write failing viewport/API tests**

  Add `HorizontalOptionDefaultsFalse`, `HorizontalBarAutoHidesAndUsesApprovedGlyphs`, `WrappingSuppressesHorizontalBarAndNormalizesOffset`, `BothBarsReserveOneCornerCell`, and `OneBarCanCauseOverflowOnTheOtherAxisWithoutOscillation`.

- [ ] **Step 2: Run focused editor tests and verify RED**

  Expected: compile/test failure because the option and viewport members do not exist.

- [ ] **Step 3: Implement option and monotonic viewport layout**

  Extend the viewport record, add at-most-two-bar fixed-point reservation, minimum-size suppression, inert corner rendering, and wrap-mode normalization.

- [ ] **Step 4: Write failing editor interaction tests**

  Add tests for horizontal arrows, track paging, drag, wheel, repeat, right-click commands, corner inertness, vertical regression, and cancellation. Assert editor-owned bars never take focus.

- [ ] **Step 5: Run editor interaction tests and verify RED**

  Expected: FAIL because horizontal hit/render/apply paths are absent.

- [ ] **Step 6: Apply the shared interaction core inside `WidgetTextEditor`**

  Keep the editor as focus owner; maintain separate vertical/horizontal interaction states and auxiliary menus; route both axes through one editor command application method.

- [ ] **Step 7: Run focused tests and full net10.0 suite**

  Expected: 0 failures and no warnings.

- [ ] **Step 8: Commit**

  Commit: `feat: add editor horizontal scrollbar`

---

### Task 5: Bounded Horizontal Extent and Horizontal Caret Clamping

**Files:**
- Create: `src/Editing/WidgetHorizontalExtentCache.cs`
- Modify: `src/Controls/WidgetTextEditor.cs`
- Modify: `src/Editing/WidgetTextViewState.cs` only if a transactional viewport/selection primitive is required
- Modify: `tests/Icod.Widgets.Tests/Editing/WidgetEditorPointerTests.cs`
- Modify: `tests/Icod.Widgets.Tests/Qualification/WidgetEditorQualificationTests.cs`

**Interfaces:**
- Consumes: Task 4 horizontal viewport and command application.
- Produces: bounded extent discovery keyed by document id/revision, tab width, wrap mode, and width-provider identity.
- Produces: exactness/lower-bound updates for `HorizontalScrollRange`.
- Produces: nearest-visible grapheme clamping for caret positions stranded left/right by viewport scrolling.

- [ ] **Step 1: Write failing extent-cache tests**

  Add `VisibleLinesExposeOverflowImmediately`, `DiscoveryIsBoundedAndEventuallyExact`, `DocumentEditInvalidatesExactWidth`, `NonzeroOffsetKeepsInexactBarVisible`, and `ExactShrinkClampsOffsetTransactionally`. Include a document larger than one update budget.

- [ ] **Step 2: Run focused tests and verify RED**

  Expected: FAIL because horizontal range is derived only from projected rows/current offset.

- [ ] **Step 3: Implement minimal bounded width cache**

  Measure display columns with the active width provider and tab width; update visible lower bounds during render and scan at most `MaximumWorkPerUpdate` logical lines per update. Invalidate on key changes and publish exactness only after full traversal.

- [ ] **Step 4: Write failing Unicode and caret tests**

  Add `HorizontalScrollClampsCaretToVisibleGrapheme`, `VisibleSelectionSurvivesHorizontalScroll`, and `TabsWideGlyphsCombiningMarksAndSurrogatesUseDisplayColumns`. Exercise track paging, drag, repeat, context, and wheel paths through shared parameterized cases.

- [ ] **Step 5: Run caret tests and verify RED**

  Expected: FAIL because horizontal scrolling can strand the caret and current extents do not account for all display-width cases.

- [ ] **Step 6: Implement transactional horizontal caret policy**

  After viewport movement, project the visible row, clamp only an off-screen caret to the nearest grapheme boundary, collapse selection only when clamping, reserve action capacity first, and emit exactly one selection action.

- [ ] **Step 7: Run qualification tests and full net10.0 suite**

  Expected: 0 failures, bounded-work assertions pass, and no warnings.

- [ ] **Step 8: Commit**

  Commit: `feat: bound editor horizontal scrolling`

---

### Task 6: API Surface, Sample, Documentation, and Release Qualification

**Files:**
- Modify: `Icod.Widgets.csproj`
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Create: `docs/releases/1.10.0.md`
- Create: `docs/Public-API-Fingerprint-1.10.json`
- Modify: `samples/Icod.Widgets.Editor.Sample/Program.cs`
- Modify: `samples/Icod.Widgets.Editor.Sample/README.md`
- Modify: `samples/README.md`
- Modify: `tests/PackageConsumer/Program.cs`
- Modify: `tests/PackageConsumer/PackageConsumer.csproj`
- Modify: packaging metadata only where exact validation requires it

**Interfaces:**
- Consumes: all preceding public APIs and behavior.
- Produces: package version/release identity `1.10.0`, updated public API fingerprint, sample smoke coverage, package-consumer coverage, curated notes, and concise user documentation.

- [ ] **Step 1: Write failing package-consumer and sample smoke assertions**

  Enable both editor options, exercise horizontal buttons/track paging and the two-bar corner in the editor smoke, and make the package consumer require `ShowHorizontalScrollBar` plus `WidgetPopupMenu.OpenAt`.

- [ ] **Step 2: Run sample/consumer checks and verify RED**

  Expected: sample assertions fail until the sample enables the new option; package consumer fails against the old package version/API.

- [ ] **Step 3: Update sample and public API release metadata**

  Set project/package version to `1.10.0`, preserve assembly/dependencies/targets, update exact package release notes, generate `docs/Public-API-Fingerprint-1.10.json`, and update the package consumer to `1.10.0` for packaged validation.

- [ ] **Step 4: Update concise README, sample guides, changelog, and curated notes**

  Keep README to one short two-axis example and a sample link. Put complete behavior, compatibility, verification placeholders, dependency identity, and Dedit follow-up contract in the changelog/release document.

- [ ] **Step 5: Run local verification**

  Run all-target Release build, full net10.0 tests, all sample smokes, public API fingerprint validation, release-note assertion, pack, exact package verification, package-only consumer, and binary compatibility consumer. Use `-m:1`/disabled build servers in this environment.

  Expected: every available local check passes with zero warnings; net8.0/net9.0 assemblies build. Record that their test hosts require CI because those runtimes are absent locally.

- [ ] **Step 6: Commit**

  Commit: `docs: prepare Widgets 1.10.0 scrollbar release`

- [ ] **Step 7: Run CI and finalize curated verification evidence**

  Push the feature branch and open a PR only when authorized by the user or existing workflow scope. Require Windows/Linux/macOS all-target green results before filling run links/hashes; do not merge or publish.

---

## Plan Self-Review

- Spec coverage: every approved behavior maps to Tasks 1–6; Terminal/DCurses and Dedit remain explicitly out of scope.
- Type consistency: Task 1 geometry feeds Tasks 2/4; Task 2 semantics feed Tasks 3/4; Task 4 viewport feeds Task 5; Task 6 consumes only completed public surface.
- TDD: every production behavior begins with a named failing test and a focused RED run before implementation.
- Review focus: lifecycle cancellation, Unicode/extreme ranges, fixed-point layout, inexact discovery, and popup/action-capacity failure modes each have named owning tests.
- Proportion: tasks define interfaces, observable assertions, and verification commands without prescribing method bodies beyond spec-fixed algorithms.
