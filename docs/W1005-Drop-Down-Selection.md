# W1005 — Drop-down Selection

**Status:** complete

Drop-downs keep committed and preview selections separate. Enter commits once;
Escape or explicit outside dismissal restores the committed key. Popups choose
the larger available area above or below the control and clip to the screen,
including an empty popup when no rows are available. Focus remains on the
drop-down throughout keyboard interaction.

Focused tests cover cancel/focus restoration, single action emission, outside
dismissal, and popup placement at both screen edges.
