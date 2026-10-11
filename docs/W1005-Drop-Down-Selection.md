# W1005 — Drop-down Selection

**Status:** complete

Drop-downs keep committed and preview selections separate. Enter commits once;
Escape or explicit outside dismissal restores the committed key. Popups choose
the larger available area above or below the control and clip to the screen,
including an empty popup when no rows are available. Normal-sized popups render
a complete theme border with inset items; viewports too small for a frame retain
the borderless compact list. Focus remains on the drop-down throughout keyboard
and pointer interaction.

Focused tests cover cancel/focus restoration, single action emission, outside
dismissal, framed and compact rendering, pointer mapping through the frame, and
popup placement at both screen edges.
