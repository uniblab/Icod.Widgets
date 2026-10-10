# W1008 — Filesystem Provider and Browser

**Status:** complete

The filesystem boundary is replaceable and asynchronous. Browser requests carry
a stable controller identity and increasing generation; only the latest result
for a live browser can apply. Failed navigation retains the last successful
listing and exposes a typed error. Expected path, access, missing, and I/O
failures are converted by the loader while cancellation and unexpected provider
exceptions propagate.

The local provider uses `System.IO`, returns absolute logical paths, lists one
directory without recursion, sorts directories first, reports links, and uses
platform path comparison. Unix exposes `/` as its root; Windows exposes ready
drive roots. Tests cover out-of-order/foreign/disposed results, retained listings,
the 100,001-entry error, Unicode/space names, ordering, and root parent semantics.
