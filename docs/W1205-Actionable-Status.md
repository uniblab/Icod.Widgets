# W1205 Actionable Status Fields

`WidgetStatusField` retains its three-position constructor/deconstruction and adds optional `CommandId` and `IsEnabled` init properties. Inert fields are not new Tab stops. `WidgetStatusBar` fits fields by descending priority using DCurses column widths, exposes visible stable field names and a focused field name, and emits a command on Enter/Space or a matched primary-pointer press and release. Field updates validate unique names before mutation and repair field focus when priority overflow changes visibility.

Host synchronization registers a region if an already attached widget gains focusability. A one-column viewport remains operable. Existing visual separators remain between fully fitted fields.

Verification: the four new tests initially failed compilation for absent members, then passed after implementation. The full net10 suite passed: 255 tests, zero failures. This is local offline Roslyn/VSTest verification; SDK restore/build and net8/net9 remain pending.
