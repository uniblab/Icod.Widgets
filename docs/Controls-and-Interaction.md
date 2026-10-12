# Controls and interaction

This guide describes the shipped Icod.Widgets 1.0 controls. Multiline editing
and the document workspace are planned separately in the [main roadmap](../ROADMAP.md).
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

Mnemonics are case-insensitive and unique within each menu level. While a menu
is open, an unmodified mnemonic invokes a command, toggles a checkable command,
or opens a submenu. Command shortcuts remain distinct, render right-aligned, and
work from the visible menu bar's active host scope without changing menu
navigation. Expanded menus use `WidgetTheme.Border`; viewports too small for a
complete frame retain borderless keyboard operation.

## Dialogs

Every dialog renders its `Title` as a caption in a `WidgetTheme.Border` frame
and insets its content within that frame. Viewports too small for a complete
frame retain a borderless title-and-content fallback. Message-dialog factories
cover acknowledgement and choice flows:

```csharp
WidgetMessageDialog notice = WidgetMessageDialog.Ok(
    "saved", "Saved", "The document was saved.");
WidgetMessageDialog question = WidgetMessageDialog.YesNo(
    "overwrite", "Overwrite?", "Replace the existing file?");
```

Enter completes the selected action. Escape always reports
`WidgetDialogOutcome.Cancel`, including from an OK-only dialog; cancellation is
therefore distinct from acknowledging the message.

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
