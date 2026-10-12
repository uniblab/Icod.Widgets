# W1203 Active Content

`WidgetContentHost` displays one caller-owned widget tree and switches it through `SetContent`. Replacement preflights host capacity, ownership, and duplicate IDs while excluding the outgoing subtree, so a failed validation retains the old content and focus. Once the outgoing subtree is detached, host registration invalidates pointer capture and clipboard requests. Editor detachment cancels an unfinished paste and its pending operation epoch. Existing document/view/editor objects remain caller-owned and can be reattached later, including an editor with the same widget ID as its predecessor.

Focused editor content transfers focus to an eligible focusable descendant of the replacement. Null content leaves an empty host. Unexpected attachment failure attempts rollback to the old content and focus. No document or view is disposed by this host.

Verification: initial net10 compilation failed for missing `WidgetContentHost`; after implementation five focused tests passed, including 1,024-widget capacity rejection, late clipboard completion, pointer release, three view states, and same-ID replacement. The whole net10 suite passed: 245 tests, zero failures. Tests used local offline Roslyn compilation and VSTest against existing dependency DLLs; restored SDK builds and net8/net9 remain pending.
