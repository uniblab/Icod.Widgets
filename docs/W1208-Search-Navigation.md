# W1208 Search and Navigation Intent

`WidgetSearchForm` composes query and optional replacement fields, holds options and explicit document/view/revision context, and emits typed Find, Replace One, and Replace All requests. The application owns search execution, regex behavior, selection changes, edit transactions, and last-query state. Query and replacement submission limits are 4,096 and 65,536 UTF-16 units. Escape completes Cancel. A result can update the form's feedback only when the form is open and the request, context, options, and field revisions still match; a query changed and restored to the same text still invalidates its old result.

`WidgetGoToLineDialog` parses a positive one-based `long` with invariant culture and completes with a typed request in `WidgetDialogResult.Value`. The caller validates its current document revision and line bounds before navigating. Context replacement clears the old field and undo history. Neither dialog edits a document.

Verification: search and Go To Line tests initially failed for missing public types. A stale-result test then found a changed-and-restored query could accept an old result; field revision guarding fixed it. Eleven focused tests and the full net10 suite passed: 276 tests, zero failures. Verification used offline Roslyn/VSTest with existing dependency assemblies; restored SDK builds and net8/net9 remain pending.
