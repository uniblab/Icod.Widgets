# W1009 — Folder Picker

**Status:** complete

The folder picker selects the browser's current directory, including a root,
only after a matching caller-applied provider validation. Navigation invalidates
pending validation. Cancel closes immediately and prevents a late successful
result from accepting. Deleted directories retain the dialog and expose a
recoverable validation error; failed start navigation remains recoverable through
the browser's typed error state.

Accepted payloads are detached one-path arrays and use the provider's path
comparison. Tests cover roots, invalid starts, deletion, cancel during
validation, and the rule that a displayed file entry cannot become the folder
result.
