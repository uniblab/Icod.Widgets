# W1010 — File Picker

**Status:** complete

The file picker supports existing-file single and multiple modes. Named suffix
filters use provider path comparison, an all-files filter is available, and
directories remain visible under every filter. Multiple selection uses an
ordered basket that persists across navigation and filter changes and
deduplicates paths with the provider comparer.

Acceptance requires a nonempty basket and a matching caller-applied validation.
Deleted paths keep the dialog open with an error; navigation or cancellation
invalidates a pending result. Accepted paths are copied so later basket changes
cannot alter the result. Focused tests cover single mode, persistence,
deduplication, directory visibility, payload aliasing, empty acceptance, and
stale completion.
