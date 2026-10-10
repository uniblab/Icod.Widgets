# Icod.Widgets 1.0 Design

**Date:** 2026-10-09 (America/New_York)\
**Status:** approach/scope approved; written design submitted for review\
**Owner:** Timothy J. Bruce\
**Target:** first stable `1.0.0`

## 1. Intent and success

Create a separate reusable TUI-control library above DCurses for general .NET
applications and roguelikes. The user explicitly requires menus; Yes/No,
OK/Cancel and Previous/Next/Cancel dialogs; drop-down and multi-select lists;
folder selection; and single/multiple file selection. All belong in the first
stable release. Supporting form controls and inventory/help/status/message
controls make those workflows usable.

Success means consistent keyboard-only operation, public-only application
integration, deterministic completion/cancellation, reusable filesystem
navigation, and realistic sample acceptance across Windows/macOS/Linux.
The four approved milestones are foundation/forms, selection/dialogs,
filesystem pickers, and roguelike/consumer qualification.

## 2. Global constraints

- Managed C# 13; target `net8.0;net9.0;net10.0`; AnyCPU.
- Exactly one direct production package dependency: `Icod.DCurses`, proposed
  baseline `2.3.1`, verified as restorable at W1001.
- No direct Terminal/TermInfo package references, raw terminal encoding, input
  reader, alternate capability database, or replacement DCurses router.
- Library `LGPL-3.0-or-later`; samples/tests `GPL-3.0-or-later`; applicable
  notices on authored source/project files; GPL code excluded from DLL/package.
- One root `Icod.Widgets.sln`, root `Icod.Widgets.csproj`, library source under
  `src/`, separate non-packable projects under `samples/` and `tests/`.
- Tooling uses .NET, CMD, shell and PowerShell 5.1; no Python or native helper.
- Applications own event reading, scheduling, command execution, validation,
  persistence, game state, file opening/writing, and session lifecycle.
- Implementation previews `1.0.0-alpha.N`; RC after milestone gates; stable
  `1.0.0` with assembly version `1.0.0.0`.

## 3. Architecture and ownership

An application feeds one normalized DCurses input at a time to `WidgetHost` and
receives immutable semantic actions. The host manages attached controls,
DCurses regions/scopes/panels, layout, theme and modal focus restoration. Controls
maintain local presentation/interaction state; they do not execute application
commands. Applications apply returned actions, perform asynchronous work, request
rendering, and refresh the DCurses session themselves.

The host borrows its `CursesSession`; it never reads input, refreshes implicitly,
or disposes that session. A control can be attached to one host only. Removing a
control invalidates its regions/capture and closes descendant overlays; disposal
is idempotent. Disposing a host releases its registrations/panels and detaches
controls. Caller models/provider objects remain caller-owned.

Reuse `CursesInteractionRouter`, region registration, scopes/leases, command
bindings, `CursesTextLayout`, geometry/track helpers, retained windows and panels.
The host routes each input once, then applies the matched control behavior.
Expose a deterministic application-level unhandled result so game/editor input
can continue when an overlay has not consumed it. Active modal input never
falls through to underlying application shortcuts.

No general capture/bubble event tree, callback dispatcher, background UI thread,
or automatically scheduled timer is required for 1.0. Host mutation/rendering
occurs on the application loop; asynchronous provider operations return immutable
results for explicit application on that loop.

## 4. Proposed contract seams

These are implementable design signatures, not an existing or frozen public API.
W1001 validates them against the published dependency and records the resulting
candidate; subsequent changes update design and contract evidence together.

```csharp
WidgetHost(CursesSession session, WidgetTheme theme);
void WidgetHost.Attach(Widget root);
void WidgetHost.Detach(Widget root);
void WidgetHost.Arrange(CursesRectangle bounds);
WidgetDispatchResult WidgetHost.Process(CursesInputEvent input, TimeSpan timestamp);
IReadOnlyList<WidgetAction> WidgetHost.DrainActions();
void WidgetHost.Render(CursesWindow target);
WidgetModalLease WidgetHost.ShowModal(WidgetDialog dialog);

WidgetDispatchResult(bool Handled, bool RequiresRender,
    IReadOnlyList<WidgetAction> Actions);
```

`timestamp` is nonnegative monotonic application time; the host rejects backwards
time before mutation. Keyboard-search prefixes expire after 750 ms of inactivity
when the next event arrives, requiring no timer. Each returned action collection
is a detached immutable snapshot. `WidgetAction` has `SourceId`; concrete actions
include `WidgetInvokedAction(CommandId)`, `WidgetValueChangedAction<T>(Value)`,
`WidgetSelectionChangedAction(Keys)`, and `WidgetDialogCompletedAction<T>(Result)`.
Changes emit once per transition; programmatic setters do not invoke commands.
The host queues semantic actions from programmatic transitions and overlay
cleanup. `Process` returns and drains that queue; `DrainActions` permits explicit
polling after application-driven changes without manufacturing an input event.
Each action is delivered by one drain only. Disposing the host is silent cleanup;
applications close dialogs/drain completion actions before host disposal.

`Widget` exposes a unique ordinal `Id`, `Bounds`, `IsVisible`, `IsEnabled`,
`IsFocusable`, and disposal state. `WidgetContainer` owns child arrangement, not
application data. `WidgetStack` supports horizontal/vertical tracks;
`WidgetGrid` uses DCurses fixed/weighted tracks. `WidgetTheme` is an immutable
mapping of normal/focused/disabled/selected/error/border roles to `CursesStyle`;
labels/commands are supplied by the caller for localization.

Limits: 1,024 attached controls per host, 16 modal levels, 16 submenu levels,
100,000 items in one list/browser snapshot, and 4,096 pending semantic actions.
Reject queue overflow before the transition that would emit an action; callers
must drain regularly. Reject duplicates, incompatible
ownership and limit violations before mutation. A host may encounter a stricter
DCurses region/scope capacity; return a clear registration failure and roll back
the partial attachment. Do not assume the control count equals region count.

## 5. Shared presentation and interaction

Measure and render using terminal text elements and columns, including wide
characters. Clip controls and hit regions to arranged visible bounds. Zero-size
viewports render/register no visible interaction; resizing recomputes layout,
regions and popup geometry before the next input. Compact dialogs scroll their
content and actions; an extremely small screen presents the focused control
where possible and retains Escape cancellation without out-of-bounds writes.

Tab/Shift+Tab traverse eligible visible/enabled controls in document order.
Arrows navigate within lists/menus/radio groups; Home/End and PageUp/PageDown
navigate lists. Enter activates; Escape cancels the top dismissible overlay.
Space toggles checkbox/checkable/multi-select items. Pointer press focuses an
eligible control; activation requires release on the same control. Capture is
released on cancellation, removal, disable/hide, and host disposal. Keyboard-only
operation must never depend on pointer support.

Input fields are single-line and use Unicode text-element caret/deletion.
Paste converts newline/tab runs to one space and removes other control
characters; enforce configurable `MaxTextElements` (default 4,096) without
splitting a text element. Number prompts parse caller-specified culture, type,
and range; invalid input displays an error and keeps the prompt open.

Rendering changes retained presentation only. A theme provides default-color
and monochrome-friendly states. Widgets does not infer terminal color/raster
capabilities or change the application's graphics backend.

## 6. Lists, drop-downs and menus

`WidgetListItem` has stable `Key`, `Text`, and `IsEnabled`.
`WidgetList.SetItems(IReadOnlyList<WidgetListItem> items)` validates/copies before
mutation. Cursor and selected keys are separate. Single selection commits on
Enter/click. Multiple selection toggles on Space; Ctrl+A selects enabled items;
Shift navigation extends a range from an explicit anchor. Disabled entries cannot
be selected; separators are menu-only inert entries. Keyboard search is ordinal
case-insensitive and wraps from the cursor.

Replacement/reordering preserves selected enabled keys; missing/disabled keys
are removed with one selection-change action. Cursor follows its stable key,
otherwise the nearest enabled entry. Returned selected keys follow current item
order. Large-list rendering/hit-testing touches visible rows, not the full list;
the list snapshot itself is deliberately bounded and retained.

`WidgetDropDown` saves committed selection/focus on opening. Navigation previews;
Enter commits once; Escape/outside dismissal restores the saved selection.
The popup chooses available space above/below and scrolls within screen bounds.

`WidgetMenuBar` and `WidgetPopupMenu` share `WidgetMenuItem` semantics:
command, checkable command, separator, or submenu. Shortcuts cannot bypass active
modal scopes. Disabled entries/separators produce no action. Right opens a
submenu; Left/Escape closes its top level; invocation closes the menu chain and
returns one command. A checkable command toggles its local state once and reports
the new state through `WidgetCheckedInvokedAction(CommandId, IsChecked)`.
Duplicate shortcuts in the same scope are
rejected; narrower active scopes take precedence over broader bindings.

## 7. Dialog and wizard outcomes

`WidgetDialogOutcome` is `Accepted`, `Declined`, `Previous`, `Next`, or
`Canceled`. `WidgetDialogResult<T>` contains the outcome and optional payload;
`WidgetDialogCompletedAction<T>` is emitted once, after successful validation.
Do not conflate a No answer with dismissal. Yes/No defaults to No; OK/Cancel
defaults to Cancel; a wizard defaults to Next when enabled. Callers may explicitly
configure the initial action. Escape is Canceled for each family.

`WidgetMessageDialog` supplies Yes/No and OK/Cancel factories;
`WidgetWizardDialog` supplies Previous/Next/Cancel and an application-provided
page container. Previous is disabled at the first page by default. A navigation
action requests a transition; the application validates/applies it and installs
the next page. Rejection retains the current page and focuses the invalid control.
Widgets does not invent wizard page order or execute application validation.

Modal leases nest in LIFO order. Closing/removing a top overlay restores its
previous eligible focus; if that control disappeared, choose the next eligible
control in the restored scope. Background focus and commands remain isolated.
Closing a parent unwinds its descendants and cancels them once. Modeless dialogs
use the same controls/results without modal isolation.

## 8. Shared filesystem browser and picker policy

`WidgetBrowser` supplies path display/input, root/drive choices, parent navigation,
directory-first listing, filtering, loading/error presentation and selection
display. `WidgetFolderPicker` permits only a current existing directory;
`WidgetFilePicker` permits existing files in single/multiple mode. Neither opens,
creates, deletes or writes files. Folder creation and save/overwrite workflows
are later candidates.

The replaceable provider owns path normalization, root/parent semantics, stable
entry keys, display names, link metadata and path comparison. The local provider
uses `System.IO` and preserves platform filename case semantics. Paths are
absolute; logical symlink paths remain logical, and navigation does not perform
recursive traversal. Provider-defined identity controls deduplication and cycle
tests; navigating back/forward across a link must not trigger recursive scans.

```csharp
interface IWidgetFileSystemProvider {
    StringComparer PathComparer { get; }
    ValueTask<IReadOnlyList<WidgetFileSystemEntry>> GetRootsAsync(CancellationToken token);
    ValueTask<WidgetDirectorySnapshot> ReadDirectoryAsync(string location, CancellationToken token);
    ValueTask<WidgetFileSelectionValidation> ValidateSelectionAsync(
        IReadOnlyList<string> paths, WidgetFileSelectionKind kind, CancellationToken token);
}
WidgetBrowserRequest WidgetBrowser.RequestDirectory(string location);
ValueTask<WidgetBrowserLoadResult> WidgetBrowserLoader.LoadAsync(
    IWidgetFileSystemProvider provider, WidgetBrowserRequest request, CancellationToken token);
bool WidgetBrowser.ApplyLoadResult(WidgetBrowserLoadResult result);
```

Requests/results include a generation, location and stable originating-browser
identity. Only the latest matching request of a live browser may apply.
Canceled/stale/disposed results cause no UI mutation; expected I/O failures become
typed errors (`NotFound`, `AccessDenied`, `InvalidPath`, `TooManyEntries`, `IoError`).
Unexpected provider exceptions propagate to the caller; the caller can display
its own failure. A failed navigation keeps the last successful listing and usable
navigation controls, with the attempted location/error visible. Roots and
directory snapshots exceeding 100,000 entries produce `TooManyEntries`.

Filters support named caller-supplied suffix sets and an all-files option, using
provider path comparison; they never hide navigable directories. Single mode
accepts one enabled existing file. Multiple mode maintains an ordered basket
across directory/filter changes, deduplicated with the provider comparer;
explicit removal/toggle changes it. Refresh or filtering never silently clears
the basket. Validate every selected path/type before Accepted; failure stays open
and identifies affected paths. The application drives validation asynchronously
and applies only the matching generation. Return a detached ordered path list;
successful validation is an observation, not a filesystem lock.

## 9. Roguelike conveniences and consumers

`WidgetInventoryList` and `WidgetActionList` compose lists with caller-supplied
items/commands; they know no actors, inventory rules or item types.
`WidgetMessageHistory` keeps a configurable bounded ring (default 1,000 messages),
wraps text and lets the caller append/clear. `WidgetStatusBar` arranges named
fields with explicit priorities; `WidgetHelpOverlay` displays caller labels and
effective host command bindings. `WidgetTextPrompt` and `WidgetNumberPrompt`
reuse input/dialog behaviors.

Three GPL samples exercise only public APIs: a complete control gallery, a
minimal editor opening existing files, and a small text roguelike demonstrating
inventory/actions/messages/status/help. The roguelike is an integration witness,
not a second game-engine project. A package-only consumer uses the packed NuGet
artifact with no source project reference.

DCurses immediate-raster refresh cannot compose overlapping panels. Samples
that use it must explicitly select a text view while an overlay is open, or use
a qualified retained presentation path; Widgets never switches it implicitly.

## 10. Verification and release

Use pure state/geometry tests, public normalized-input replay, retained-cell
inspection, fake provider tests and real temporary-directory tests. Cover stale
async completion, disposed focus, multi-selection under replacement, nested
modals, Unicode/control paste, compact/zero geometry, and resource rollback.
Measure 100,000-item scrolling and repeated overlay open/close; record actual
work/allocation rather than claiming universal terminal timing.

Freeze API fingerprint/XML docs, package payload/license/dependencies, README,
CHANGELOG and sample instructions at RC. Require fresh exact-head CI and live
Windows/macOS/Linux witnesses with recorded terminal/architecture/version.
Publication requires the separate authorized release step described in the
[development roadmap](../../../Icod.Widgets-1.0.0-Development-Roadmap.md).
