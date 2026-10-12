# W1202 Stable-Key Tab Strip

`WidgetTabStrip.SetTabs` validates and copies a complete snapshot before replacing the visible state. Tabs have unique stable keys, captions, modified/enabled/closable presentation flags, and a 1,024 item bound. A null active key preserves the old active key if enabled; removal selects an enabled survivor at the previous index or the nearest earlier one. The strip emits activation and close requests, leaving document ownership and close decisions with the application.

Keyboard navigation supports Left/Right, Home/End, Control+Tab, Control+Shift+Tab, Enter, and Delete. Primary pointer press activates a visible tab or its close affordance. A right overflow cell and keyboard cycling expose offscreen tabs. Width calculation uses DCurses text columns, and the active tab remains visible even in a one-column viewport.

Verification: initial net10 test compilation failed because the public types were absent. The filtered five tab tests passed after implementation, followed by the full net10 suite: 240 passed, zero failed. Verification used an offline Roslyn compile and VSTest with existing dependency assemblies. Standard restored SDK build and other target frameworks remain pending.
