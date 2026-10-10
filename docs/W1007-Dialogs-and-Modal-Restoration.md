# W1007 — Dialogs and Modal Restoration

**Status:** complete

OK, Yes/No and OK/Cancel dialogs use distinct typed outcomes, safe defaults, and
a single completion action. Escape always means Cancel, including for an OK-only
acknowledgement. Dialog titles render as captions in themed frames with inset
content and compact fallback. Modal leases isolate input
from background controls, nest to sixteen levels, and restore surviving prior
focus. Closing a parent cancels each descendant exactly once; host disposal is
silent cleanup.

The wizard shell exposes Previous/Next navigation requests without advancing
itself. Applications apply or reject transitions, so validation rejection keeps
the current page, focus, and error. Focused tests cover outcome distinction,
safe defaults, once-only cancellation, background isolation, parent unwind,
validation rejection, captioned framing, and the 16/17 modal boundary.
