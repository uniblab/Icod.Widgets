# W1006 — Menus and Submenus

**Status:** complete

Menu bars and popup menus share command, checkable-command, separator, mnemonic,
shortcut, and submenu semantics. Alt plus a top-level mnemonic opens that menu
from another focused control. Once open, an unmodified mnemonic acts only within
the current level; commands invoke, checkable commands toggle once, and submenus
open. Command shortcuts remain separate, invoke directly from the visible menu
bar's active host scope, and cannot bypass an active modal scope.

Shortcut duplicates and case-insensitive mnemonic duplicates within one level
are rejected before construction. The active popup level uses a themed frame and
right-aligned shortcut column when space permits; one- and two-row viewports keep
borderless clipping and usable Escape behavior. Sixteen nested levels are
accepted and seventeen reject before interaction. Escape/Left unwind one submenu
and restore its parent item, while invocation closes the chain.
