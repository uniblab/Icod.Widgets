# Controls and interaction

This guide covers control interaction, including document workspace controls.
The [main roadmap](../ROADMAP.md) tracks release status.
See the [root README](../README.md) for installation, host setup, and a runnable gallery.

## Menus

Mnemonics navigate menu structure; shortcuts invoke commands directly. Assign a
mnemonic separately from a shortcut so `Alt+V` can open View, plain `F` can invoke
Go to File within that open level, and `Ctrl+L` can invoke Go to Line without
opening the menu:

```csharp
WidgetMenuShortcut goToLine = new(
    WidgetKey.Character, WidgetInputModifiers.Control, 'l');

WidgetMenuBar menu = new("menu", [
    WidgetMenuItem.Submenu("view", "View", [
        WidgetMenuItem.Command(
            "file", "Go to File...", "view.file", 'f'),
        WidgetMenuItem.Command(
            "line", "Go to Line...", "view.line", 'l',
            shortcut: goToLine)
    ], 'v')
]);
```

Attach the bar to the host and arrange it in one row, for example
`menu.Arrange(new CursesRectangle(0, 0, 1, host.Screen.Columns))`. Arrange the
editor beneath it. Opening a menu does not rearrange either control: its
content-sized dropdown is a retained overlay anchored under the active heading,
clamped to the screen. Shortcuts have a separate right-aligned column. Long
menus scroll the selected item into view; very small screens omit the frame.
Standalone `WidgetPopupMenu` controls retain their caller-supplied width/bounds.

| Input | Behavior |
|---|---|
| Alt + heading mnemonic | Open that root menu and select its first enabled command, including while another root is open |
| Up / Down | Move through enabled commands, skipping separators and wrapping at either end |
| Right | Open the selected submenu; otherwise switch to the next root heading |
| Left | Return from a nested submenu; at the root dropdown, switch to the previous heading |
| Escape | Return from a nested submenu; close the root dropdown and restore previous focus |
| Enter / Space / plain mnemonic | Invoke the selected command or open its submenu |
| Click heading | Open or switch roots; click the active heading again to close |
| Press/release on the same enabled row | Invoke that row |
| Click outside | Dismiss without invoking an underlying control |
| Pointer movement | Optional theme highlight; never moves keyboard selection or invokes a command |

Mnemonics are case-insensitive and unique within each level. The mnemonic text
element is underlined by default. Root shortcuts invoke directly from a visible,
enabled menu bar and cannot bypass an active modal. A nested submenu keeps its parent visible and opens to the right when it fits.
Otherwise it overlays the parent. Left/Escape removes the child overlay and
restores the parent selection. Exposed parent rows remain clickable.
Direct commands on the bar also remain supported.

### Menu themes

Use `WidgetTheme.Parts` for a shared theme or inherited `Widget.Styles` for a
specific menu. Colors are application-defined; the widget contains no required
palette. `WidgetPalette.Create` supplies capability-aware dark/light examples.

```csharp
WidgetTheme theme = WidgetPalette.Create(session.PresentationCapabilities);
host.SetTheme(theme with {
    HighlightMenuHover = true, // false by default
    Parts = theme.Parts
        .With(WidgetStyleRole.MenuBarActive, theme.Focused)
        .With(WidgetStyleRole.PopupHover, theme.Selected)
});
host.Render();
```

| Role | Part |
|---|---|
| `MenuBarSurface`, `MenuBarItem`, `MenuBarActive`, `MenuBarDisabled` | Bar padding, normal heading, active heading, disabled heading |
| `MenuBarHover`, `PopupHover` | Hovered heading/command when `HighlightMenuHover` is enabled |
| `MenuBarMnemonic`, `MenuBarActiveMnemonic` | Normal/active heading mnemonic overrides |
| `PopupSurface`, `PopupBorder`, `PopupItem`, `PopupFocused`, `PopupDisabled` | Popup background, frame/separators and command states |
| `PopupShortcut`, `PopupCheck`, `PopupMnemonic`, `PopupFocusedMnemonic` | Unselected decorations and normal/selected mnemonic overrides |

Selected rows take precedence over hover and cover labels, check marks, padding
and shortcuts. Mnemonics inherit their row's colors and add underline unless an
explicit mnemonic part style is supplied. Defining such a style also lets you
change or remove the underline. Hovered rows use their hover style across all
parts, with the mnemonic underlined. A theme change repaints the current state;
it does not change what Enter will activate. Pointer presses and releases must
match; disabled rows and separators are inert.

The application must request DCurses `CursesMouseTrackingMode.AnyMotion` to
receive hover events when supported. Keyboard operation does not depend on
mouse support. The gallery's Menus page demonstrates theme switching and F8
hover toggling while a menu is open.

## Dialogs

Every dialog renders its `Title` as a caption in a `WidgetTheme.Border` frame
and insets its content within that frame. Viewports too small for a complete
frame retain a borderless title-and-content fallback. Message-dialog factories
cover acknowledgement and common choice flows. Use an action array when
captions and behavior are application-specific:

```csharp
WidgetMessageDialog notice = WidgetMessageDialog.Ok(
    "saved", "Saved", "The document was saved.");
IReadOnlyList<WidgetDialogAction> actions = [
    new("yes", "Yes", WidgetDialogOutcome.Yes) { Mnemonic = 'Y' },
    new("no", "Don't Save", WidgetDialogOutcome.No) { Mnemonic = 'N' },
    new("cancel", "Cancel", WidgetDialogOutcome.Cancel) {
        Mnemonic = 'C', IsDefault = true, IsCancel = true
    }
];
WidgetMessageDialog question = new(
    "close", "Unsaved changes", "Do you want to save these changes?", actions
);
```

`Label` controls presentation while `Value` and `Outcome` define the result.
`Mnemonic`, `IsDefault`, and `IsCancel` independently define plain-letter,
Enter, and Escape behavior. `Actions` is read-only; `SetActionEnabled` changes
availability while retaining at least one enabled choice. The legacy `YesNo`
factory remains binary compatible but is warning-level obsolete; migrate to the
constructor when the decision is not literally Yes/No.

Every action renders as `[Label]`. Enter completes the selected/default action;
plain mnemonics, keyboard selection, and a primary-button press/release on the
same visible choice all use the same action procedure. Gaps are not buttons;
capture loss or a release outside the pressed choice cancels the click. Escape
activates the declared cancel action, or reports synthetic
`WidgetDialogOutcome.Cancel` when none exists, including an OK-only dialog.
Scoped `Styles` can change all dialog presentation roles without changing
mnemonics, hit testing, defaults, cancellation, or result policy.

For a centered dialog, call `host.ShowModal(dialog)` and then arrange the dialog
inside the screen; repeat that layout after resizing. The gallery's About
command is a complete example. A menu command closes its popup before dispatch,
so the host can open a modal and later restore the original editor focus.

Define `WidgetStyleRole.ModalBackdrop` in the theme (or dialog's styles) to
repaint the underlying host content in a dim style while a modal is open.
The built-in dark/light palettes provide it. Omitting this role, as the
terminal-default theme does, preserves the normal background appearance.
The backdrop is a separate retained panel: it does not alter borrowed base
cells, and nested modals dim the parent dialog too. Its style can change while
the modal is open.

## File pickers

Picker input emits `WidgetBrowserLoadRequestedAction` and
`WidgetPickerValidationRequestedAction`. The application performs provider I/O
and applies its result on the loop, then drains completion actions. Root requests
use `WidgetBrowserRequest.IsRoots` and the same loader/generation guard as directory
requests. Initial loading is explicit: call `RequestDirectory(StartLocation)`.

Arrows/Page keys move the cursor; Enter opens directories, selects/accepts a single file, or toggles files in multiple mode;
Space toggles files; Ctrl+S (or Ctrl+Enter when reported) validates/accepts; Escape cancels. Backspace/Alt+Up
moves to parent; Ctrl+L edits a path; Ctrl+R lists roots; Ctrl+F cycles file filters;
Ctrl+B displays the basket (Enter/Space/Delete removes a selected file).
Errors leave the last listing usable. Loads after picker closure and stale/foreign
results are ignored. Applications may cancel outstanding provider work themselves.

## Document workspace

`WidgetTabStrip` accepts a validated ordered tab snapshot with stable keys.
Activation and close actions are requests; the application decides whether to
switch or remove a document. `WidgetContentHost.SetContent` detaches the old
caller-owned widget tree without disposing its document or view. Use separate
`WidgetTextViewState` instances per document and retain them in the application.
The multiline editor consumes Tab/Shift+Tab for indentation; applications that
place it beside workspace controls should provide an explicit focus key such as
F6. The workspace sample uses F6/Shift+F6 for the editor, tabs, and status fields.
Delete emits a close request when the tab strip has focus.

Menu presentation snapshots can update captions, checked/enabled state, and
selection while a menu is open. `FocusedEditCommand` first routes to the focused
editor; an inapplicable edit falls back to the application command. Optional
status field commands create visible, keyboard/pointer-operable fields.

`WidgetFormDialog` supports composed child controls and typed validation. The
Save/Don't Save/Cancel factory defaults to its Cancel action, which Escape also
activates.
`WidgetSaveTargetPicker` asks a separate optional provider to inspect a proposed
target and never writes files. Search and Go To Line forms emit typed requests;
the application validates the document/view/revision before applying results.

The [workspace sample](../samples/Icod.Widgets.Workspace.Sample/README.md)
exercises these controls in an in-memory application loop.

## Errors and configuration

Configure styles through `WidgetTheme`, choose a filesystem provider, file filters,
text limits and number culture/range at construction. Duplicate IDs/keys, invalid
ownership and capacity violations throw before attachment. Provider loaders map
expected I/O/path failures to typed browser errors; unexpected provider exceptions
remain application errors. A DCurses session requires interactive input/output
and a terminal profile supporting cursor motion.

## Limits

One host accepts 1,024 widgets, 4,096 pending actions, 16 modal levels, and 16
submenu levels. Lists and directory snapshots accept 100,000 items. Extremely
small viewports retain keyboard cancellation and clip rendering. DCurses
immediate-raster presentation cannot compose overlapping retained panels, so an
application using raster content must select its text presentation while an
overlay is visible.
