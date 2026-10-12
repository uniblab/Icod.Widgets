# W1206 Composable Dialogs and Decisions

`WidgetDialog` is now a `WidgetContainer`. Dialog children participate in host registration, modal scope, focus, input, and panel rendering. Eligible children can receive initial focus by stable widget ID. Escape remains a Cancel completion, including OK-only dialogs. Nested modal close restores a previously focused child. A duplicate child ID rejects modal attachment before changing focus.

`WidgetFormDialog` owns explicit actions with one default and at most one explicit cancel, plus optional child controls. `SaveDontSaveCancel` exposes three distinct outcomes and defaults to Cancel. Disabled actions do not complete. Validation-required actions emit `WidgetDialogValidationRequestedAction` with a dialog GUID and generation; only a current request can complete the still-open dialog. Rejected validation displays an error, and a later result after close or state change is ignored. No save or overwrite policy is embedded.

Verification: child composition and form tests initially failed to compile for missing public APIs, then seven focused tests passed. The full net10 suite passed: 262 tests, zero failures. This used offline Roslyn/VSTest with existing dependency DLLs; restored SDK builds and net8/net9 remain pending.
