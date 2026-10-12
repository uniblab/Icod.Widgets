# Icod.Widgets 1.2 Commands and Status Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Execute natively without subagents.

**Goal:** Make menu command presentation context-sensitive and status fields actionable without changing mnemonic, shortcut, or inert-field compatibility.

**Architecture:** Menus retain immutable definitions and layer caller-applied state by stable item ID. Focused edit routing is explicit per menu command, with application fallback only when no focused edit target applies. Status fields retain their positional record shape and gain optional action metadata.

**Tech Stack:** C# 13, net8.0/net9.0/net10.0, Icod.DCurses 2.4.0, xUnit.

**Spec:** [1.2 Document Workspace design](../specs/2026-10-12-icod-widgets-1.2-document-workspace-design.md)

## Global Constraints

- Package `1.2.0`, assembly identity `1.0.0.0`, only direct runtime dependency DCurses `2.4.0`.
- Keep existing menu factories and `WidgetStatusField(string Name, string Text, int Priority)` construction/deconstruction and inert behavior.
- No callback during render; snapshots applied on caller loop; no duplicate command execution or hidden policy.
- Library LGPL-3.0-or-later; tests GPL-3.0-or-later; full source headers.

## Review Focus

- Active menu item disabled or removed by an update: next enabled sibling, then previous, then close level (Task 1).
- A checked command with controlled presentation: emit requested next value, do not silently change caller state (Task 1).
- A disabled focused edit target: do not fall back to an application command (Task 2).
- Primary press/release while pointer leaves a popup: at most one activation and no out-of-bounds index (Task 2).
- Focused status field disappears behind priority overflow: move focus to a visible actionable field or away (Task 3).

---

## File map and public contracts

Create `src/Menus/WidgetMenuItemPresentation.cs` with `public sealed record WidgetMenuItemPresentation(string ItemId, string Text, bool IsEnabled, bool IsChecked = false)`. Add `WidgetPopupMenu.SetPresentation(IReadOnlyList<WidgetMenuItemPresentation> presentation)`; an omitted item uses its definition defaults, an unknown/duplicate ID is rejected before mutation, and a supplied checkable state is controlled by the caller. Add `WidgetMenuItem.FocusedEditCommand { get; init; }` of type `WidgetEditCommand?` (factory-created items can use `with` only if converted; instead keep the sealed class and add optional trailing factory parameter on command/checkable overloads). Existing factories and semantics remain source-compatible.

Extend `WidgetStatusField` with non-positional init properties `string? CommandId` and `bool IsEnabled = true`. `WidgetStatusBar` adds `FocusedFieldName`, `VisibleFieldNames`, and emits `WidgetInvokedAction` using the chosen field's `CommandId`. Host registration must update when status fields gain or lose actionable focusability after attachment.

### Task 1: W1204 controlled menu snapshots

**Files:** Create `src/Menus/WidgetMenuItemPresentation.cs`, `tests/Icod.Widgets.Tests/Menus/WidgetDynamicMenuTests.cs`; modify `src/Menus/WidgetMenuItem.cs`, `src/Menus/WidgetPopupMenu.cs`; create `docs/W1204-Dynamic-Menus.md`.

**Interfaces:** Produces `SetPresentation` and `WidgetMenuItemPresentation`. Consumes existing stable `WidgetMenuItem.Id`; the old `IsChecked` self-toggle remains only for items without controlled state.

- [ ] **Step 1: Write failing tests.** `UpdateOpenMenuKeepsStableSelection` changes captions/enabled state while a submenu is open; `DisabledCurrentChoosesNextThenPreviousOrCloses` asserts deterministic selection; `ControlledCheckableEmitsIntentWithoutMutation` asserts `IsChecked` stays as supplied until next snapshot; `DuplicateUnknownAndOversizedStateRejectBeforeMutation` checks argument errors and unchanged menu; `MnemonicAndShortcutRemainSeparate` asserts Alt+V then plain F differs from Ctrl+F. Include the 1.1 no-snapshot self-toggle test unchanged.

  ```csharp
  menu.SetPresentation( [ new( "find", "Find", false ), new( "next", "Next", true ) ] );
  Assert.Equal( "next", menu.CurrentItem?.Id );
  Assert.Equal( "view.file", Assert.IsType<WidgetInvokedAction>( Assert.Single( mnemonicResult.Actions ) ).CommandId );
  ```
- [ ] **Step 2: Verify red.** Run `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release -f net10.0 --filter FullyQualifiedName~WidgetDynamicMenuTests`; expect missing type/method.
- [ ] **Step 3: Implement.** Add exact contracts above; validate unique IDs across the entire immutable menu tree at construction, preserve selected IDs rather than indexes on state refresh, and render/dispatch from one effective state snapshot. Bound snapshot by existing `WidgetLimits.MaximumItems`; reject unknown/duplicate IDs before assignment. Do not mutate user-supplied records.
- [ ] **Step 4: Verify green.** Run filtered tests and the full net10 suite; expect zero failures. Record W1204 evidence.
- [ ] **Step 5: Commit.** Stage named files and `git commit -m "feat: apply dynamic menu presentation state"`.

### Task 2: W1204 pointer and focused edit routing

**Files:** Modify `src/Menus/WidgetMenuItem.cs`, `src/Menus/WidgetPopupMenu.cs`, `src/Menus/WidgetMenuBar.cs`, `src/Core/WidgetHost.cs`, `src/Core/WidgetHost.Popups.cs`; create `tests/Icod.Widgets.Tests/Menus/WidgetMenuRoutingTests.cs`; update `docs/W1204-Dynamic-Menus.md`.

**Interfaces:** `WidgetMenuItem.Command` and `Checkable` factory overloads gain optional final `WidgetEditCommand? focusedEditCommand = null`; `WidgetMenuItem.FocusedEditCommand` is get-only. Internal host dispatch: `WidgetCommandResult DispatchFocusedEditOrApplication(WidgetMenuItem item, string sourceWidgetId, TimeSpan timestamp)`. If edit result is Executed or Disabled, consume; only NotApplicable emits one application `WidgetInvokedAction`/`WidgetCheckedInvokedAction`. A disabled menu item never routes.

- [ ] **Step 1: Write failing tests.** `FocusedEditorReceivesCopyExactlyOnce`, `DisabledEditTargetDoesNotFallThrough`, `NoEditTargetEmitsApplicationCommandOnce`, `PointerSelectsSubmenuAndCommand`, and `PointerReleaseOutsideDoesNotInvoke` assert result/action counts. Cover popup border clicks, compact one-row popup, capture cancel, and opening from a menu-bar primary press.

  ```csharp
  Assert.Empty( disabledResult.Actions.OfType<WidgetInvokedAction>() );
  Assert.Single( noTargetResult.Actions.OfType<WidgetInvokedAction>() );
  Assert.Empty( pointerReleaseOutside.Actions );
  ```
- [ ] **Step 2: Verify red.** Run filtered `WidgetMenuRoutingTests`; expect new factory parameter/routing behavior failure.
- [ ] **Step 3: Implement.** Add trailing optional parameter to every command/checkable factory overload without altering existing argument order; route through host once. Add pointer hit testing using clipped `RenderBounds` and visible row offset, including menu-bar hit testing. Retain Alt+mnemonic and Ctrl+shortcut routing, prevent menu pointer capture from delivering an old release after dismissal.
- [ ] **Step 4: Verify green.** Run filtered and full net10 tests; expect zero failures. Update W1204 evidence.
- [ ] **Step 5: Commit.** Stage named files and `git commit -m "feat: route menu pointer and edit commands"`.

### Task 3: W1205 actionable status

**Files:** Modify `src/GameUi/WidgetStatusBar.cs`, `src/Core/WidgetHost.cs`; create `tests/Icod.Widgets.Tests/GameUi/WidgetActionableStatusTests.cs`, `docs/W1205-Actionable-Status.md`.

**Interfaces:** Consumes existing `WidgetInvokedAction`. Produces optional `WidgetStatusField.CommandId`, `IsEnabled`, and bar `FocusedFieldName`/`VisibleFieldNames`. Source-compatible positional constructor and deconstruction are required.

- [ ] **Step 1: Write failing tests.** `OldThreeArgumentFieldRemainsInert`, `LateActionableFieldCanReceiveFocus`, `KeyboardAndPointerInvokeVisibleFieldOnce`, `OverflowDropsLowPriorityActionAndRepairsFocus`, `DuplicateNamesRejectBeforeMutation`, and `WidthOneNeverIndexesOutsideScreen`. Assert old record deconstruction still yields exactly three values.

  ```csharp
  WidgetStatusField field = new( "encoding", "UTF-8", 10 ) { CommandId = "encoding.choose" };
  Assert.Contains( "encoding", bar.VisibleFieldNames );
  Assert.Equal( "encoding.choose", Assert.IsType<WidgetInvokedAction>( Assert.Single( result.Actions ) ).CommandId );
  ```
- [ ] **Step 2: Verify red.** Run filtered `WidgetActionableStatusTests`; expect missing public members.
- [ ] **Step 3: Implement.** Lay out prioritized fields by DCurses measured columns, keep field name identity and bounded visible hit ranges, and emit only enabled action fields. Add narrowly scoped host synchronization for a widget whose `IsFocusable` becomes true/false after `SetFields`, using the existing interaction router and traversal rules. Never focus an invisible field; do not make an inert bar a new Tab stop.
- [ ] **Step 4: Verify green.** Run filtered and full net10 tests, plus roguelike `--smoke`; expect zero failures. Record W1205 evidence.
- [ ] **Step 5: Commit.** Stage named files and `git commit -m "feat: add actionable status fields"`.

## Handoff

Continue with [dialogs/save](2026-10-12-icod-widgets-1.2-dialogs-save.md). The menu/status plans must not change application command meaning or introduce render-time callbacks.
