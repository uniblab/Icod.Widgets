# W1006 — Menus and Submenus

**Status:** complete

Menu bars and popup menus share command, checkable-command, separator, shortcut,
and submenu semantics. Disabled entries and separators are inert; checkable
commands report their new state once. Escape/Left unwind one submenu and restore
the parent's current item, while invocation closes the chain.

Shortcut duplicates are rejected before construction. Sixteen nested levels are
accepted and seventeen reject before interaction. Rendering clips the active
level to the arranged screen, including a one-cell viewport, while Escape
remains usable.
