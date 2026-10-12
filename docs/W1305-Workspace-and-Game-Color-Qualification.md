# W1305 workspace and game controls

Source: `Icod.Widgets 1.3.0-alpha.1` on PR #6; direct dependency
`Icod.DCurses 2.5.0`.

Tabs render an active tab even while focus is elsewhere. Modified and close
markers, inactive tabs, strip background and overflow have separate roles.
The status bar resolves surface, ordinary field, actionable field, focused
action and disabled styles; priority and input routing stay unchanged.
Scrollbars resolve track, thumb and focused thumb independently, and message
history repaints both messages and unused rows on a theme switch.

The new focused tests first failed against the previous renderers. Release
solution build: zero warnings/errors. Full tests: 290 passed, zero failed on
each of net8.0, net9.0 and net10.0. Gallery, Editor, Roguelike and Workspace
net10 `--smoke` paths all report PASS.

Remaining: W1306 overlays/pickers; W1307 capability palette; W1308 sample and
documentation; W1309–W1310 package gates. Live terminal color checks on macOS
and Linux are **Not tested**.
