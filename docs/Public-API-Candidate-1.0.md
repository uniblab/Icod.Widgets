# Icod.Widgets 1.0 Public API Candidate

**Status:** completed design ledger, superseded for compatibility review by the
[`1.0 public API baseline`](Public-API-Baseline-1.0.md) and fingerprint.

## Dependency boundary

- Sole direct runtime dependency: `Icod.DCurses 2.3.1`.
- Applications may construct a `WidgetHost` from a live `CursesSession` or from
  an application-owned `CursesScreen` for deterministic composition/testing.
- A host borrows its session/screen and owns its `CursesInteractionRouter`.

## Core candidate

```csharp
WidgetHost(CursesSession session, WidgetTheme theme)
WidgetHost(CursesScreen screen, WidgetTheme theme)
CursesScreen WidgetHost.Screen { get; }
WidgetTheme WidgetHost.Theme { get; }
int WidgetHost.AttachedCount { get; }
Widget? WidgetHost.FocusedWidget { get; }
TimeSpan WidgetHost.CurrentTime { get; }
void WidgetHost.Attach(Widget widget)
bool WidgetHost.Detach(Widget widget)
void WidgetHost.Arrange(CursesRectangle bounds)
bool WidgetHost.Focus(Widget widget)
WidgetDispatchResult WidgetHost.Process(WidgetInput input, TimeSpan timestamp)
WidgetDispatchResult WidgetHost.Process(CursesInputEvent input, TimeSpan timestamp)
IReadOnlyList<WidgetAction> WidgetHost.DrainActions()
void WidgetHost.Render()
void WidgetHost.Dispose()

WidgetTheme(CursesStyle Normal, CursesStyle Focused,
    CursesStyle Disabled, CursesStyle Selected,
    CursesStyle Error, CursesStyle Border)
WidgetTheme WidgetTheme.Default { get; }

abstract Widget(string id)
string Widget.Id { get; }
CursesRectangle Widget.Bounds { get; }
bool Widget.IsEnabled { get; set; }
bool Widget.IsVisible { get; set; }
bool Widget.IsFocusable { get; }
void Widget.Arrange(CursesRectangle bounds)

WidgetInput WidgetInput.Key(WidgetKey key, WidgetInputModifiers modifiers = None,
    Rune? character = null)
WidgetInput WidgetInput.TextInput(Rune character)
WidgetInput WidgetInput.Paste(string text)
WidgetInput WidgetInput.PointerInput(CursesMouseEvent pointer)
WidgetInput WidgetInput.From(CursesInputEvent input)

WidgetStack(string id, WidgetOrientation orientation)
WidgetGrid(string id, int columns)
void WidgetContainer.Add(Widget child)
bool WidgetContainer.Remove(Widget child)

WidgetLabel(string id, string text)
WidgetButton(string id, string text, string commandId)
WidgetTextInput(string id, string value = "")
string WidgetTextInput.Value { get; }
int WidgetTextInput.MaxTextElements { get; set; } // default 4,096
WidgetCheckBox(string id, string text, bool isChecked = false)
WidgetRadioGroup(string id, IReadOnlyList<string> keys, string? selectedKey = null)
WidgetBorder(string id, Widget? child = null)
WidgetScrollIndicator(string id)

WidgetListItem(string Key, string Text, bool IsEnabled = true)
WidgetList(string id, WidgetSelectionMode selectionMode)
void WidgetList.SetItems(IReadOnlyList<WidgetListItem> items)
IReadOnlyList<WidgetListItem> WidgetList.Items { get; }
string? WidgetList.CursorKey { get; }
IReadOnlyList<string> WidgetList.SelectedKeys { get; }

WidgetDropDown(string id, IReadOnlyList<WidgetListItem> items, string selectedKey)
string WidgetDropDown.SelectedKey { get; }
string WidgetDropDown.PreviewKey { get; }
bool WidgetDropDown.IsOpen { get; }
void WidgetDropDown.Open()
void WidgetDropDown.Dismiss()

WidgetMenuShortcut(WidgetKey Key,
    WidgetInputModifiers Modifiers = None, char? Character = null)
char? WidgetMenuItem.Mnemonic { get; }
WidgetMenuItem WidgetMenuItem.Command(string id, string text,
    string commandId, bool isEnabled = true,
    WidgetMenuShortcut? shortcut = null)
WidgetMenuItem WidgetMenuItem.Command(string id, string text,
    string commandId, char mnemonic, bool isEnabled = true,
    WidgetMenuShortcut? shortcut = null)
WidgetMenuItem WidgetMenuItem.Checkable(string id, string text,
    string commandId, bool isChecked = false, bool isEnabled = true,
    WidgetMenuShortcut? shortcut = null)
WidgetMenuItem WidgetMenuItem.Checkable(string id, string text,
    string commandId, char mnemonic, bool isChecked = false,
    bool isEnabled = true, WidgetMenuShortcut? shortcut = null)
WidgetMenuItem WidgetMenuItem.Separator(string id)
WidgetMenuItem WidgetMenuItem.Submenu(string id, string text,
    IReadOnlyList<WidgetMenuItem> children, bool isEnabled = true)
WidgetMenuItem WidgetMenuItem.Submenu(string id, string text,
    IReadOnlyList<WidgetMenuItem> children, char mnemonic,
    bool isEnabled = true)
WidgetPopupMenu(string id, IReadOnlyList<WidgetMenuItem> items)
WidgetMenuBar(string id, IReadOnlyList<WidgetMenuItem> items)
void WidgetPopupMenu.Open()
void WidgetPopupMenu.Close()

WidgetModalLease WidgetHost.ShowModal(WidgetDialog dialog)
int WidgetHost.ModalDepth { get; }
WidgetMessageDialog WidgetMessageDialog.YesNo(string id, string title, string message)
WidgetMessageDialog WidgetMessageDialog.OkCancel(string id, string title, string message)
WidgetWizardDialog(string id, string title, IReadOnlyList<string> pages)
void WidgetWizardDialog.ApplyNavigation(int pageIndex)
void WidgetWizardDialog.RejectNavigation(string error)
WidgetDialogCompletedAction(string WidgetId, WidgetDialogResult Result)
WidgetWizardNavigationRequestedAction(string WidgetId, int FromPage, int ToPage)
WidgetBrowserLoadRequestedAction(string WidgetId, WidgetBrowserRequest Request)
WidgetPickerValidationRequestedAction(string WidgetId,
    WidgetPickerValidationRequest Request)

interface IWidgetFileSystemProvider
WidgetBrowserRequest WidgetBrowser.RequestDirectory(string location)
bool WidgetBrowser.ApplyLoadResult(WidgetBrowserLoadResult result)
ValueTask<WidgetBrowserLoadResult> WidgetBrowserLoader.LoadAsync(
    IWidgetFileSystemProvider provider, WidgetBrowserRequest request,
    CancellationToken token)
LocalWidgetFileSystemProvider()

WidgetFolderPicker(string id, IWidgetFileSystemProvider provider,
    string startLocation)
WidgetBrowserRequest WidgetFolderPicker.RequestDirectory(string location)
WidgetPickerValidationRequest WidgetFolderPicker.RequestAccept()
bool WidgetFolderPicker.ApplyValidationResult(WidgetPickerValidationResult result)

WidgetFileFilter(string name, IReadOnlyList<string> suffixes)
WidgetFileFilter WidgetFileFilter.AllFiles { get; }
WidgetFileSelectionBasket(StringComparer comparer)
WidgetFilePicker(string id, IWidgetFileSystemProvider provider,
    string startLocation, bool allowMultiple,
    IReadOnlyList<WidgetFileFilter> filters)
void WidgetFilePicker.ToggleFile(string path)
WidgetPickerValidationRequest WidgetFilePicker.RequestAccept()
bool WidgetFilePicker.ApplyValidationResult(WidgetPickerValidationResult result)

WidgetInventoryList(string id)
WidgetActionList(string id)
WidgetMessageHistory(string id, int capacity = 1000)
WidgetStatusBar(string id)
WidgetHelpOverlay(string id, string title,
    IReadOnlyList<WidgetHelpBinding> bindings)
WidgetTextPrompt(string id, string title, string value = "")
WidgetNumberPrompt(string id, string title, CultureInfo culture,
    double minimum, double maximum, string value = "")
```

The rc.2 correction extends picker loading with roots:

```csharp
WidgetBrowserRequest WidgetBrowser.RequestRoots()
bool WidgetBrowserRequest.IsRoots { get; init; }
void WidgetFolderPicker.SetRoots(IReadOnlyList<WidgetFileSystemEntry> roots)
void WidgetFilePicker.SetRoots(IReadOnlyList<WidgetFileSystemEntry> roots)
```

Normal asynchronous roots loading uses RequestRoots/WidgetBrowserLoader and
ApplyLoadResult, preserving the identity/generation guard. SetRoots is explicit
application-controlled presentation replacement. Picker input emits load and
validation requests rather than performing I/O. Applications drain completion
results after applying validation on their loop.
