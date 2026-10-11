# W1002 — Host, Layout, and Lifecycle

**Status:** complete

The host now borrows a DCurses screen/session, owns only its interaction router,
and supports transactional tree attachment, focus, monotonic input dispatch,
single-drain immutable actions, rendering, and deterministic cleanup. Stack and
grid containers replace child geometry on every arrange. Empty geometry disables
interaction, and labels clip through DCurses' Unicode text-element APIs.

The host accepts exactly 1,024 widgets and rejects a 1,025-widget tree before
attachment. Duplicate identifiers also reject the full tree before any DCurses
region is registered. Focus repair is delegated to the public DCurses router and
selects the next eligible widget after removal.

Focused host and layout tests passed for .NET 8, 9, and 10. The local runner
requires serialized MSBuild (`-m:1`); this does not alter project behavior.
