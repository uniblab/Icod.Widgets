# W1308 gallery and public documentation

Source: `Icod.Widgets 1.3.0-alpha.1` on PR #6; direct dependency
`Icod.DCurses 2.5.0`.

The gallery reads `CursesSession.PresentationCapabilities` and uses the public
`WidgetPalette` API. Ctrl+T cycles dark, light and legacy terminal-default
themes via `WidgetHost.SetTheme` without losing focus. Seven gallery pages now
include a multiline editor, active modified tab, scrollbar and actionable
status, alongside forms, lists, menus, overlays, pickers and game controls.
The deterministic gallery smoke renders every page under all three modes and
checks focus retention; it does not promise live terminal color behavior.
Root README and sample guides explain the style roles, both color axes,
capability limits, scoped overrides and runnable gallery command.

Release solution build: zero warnings/errors. Full tests: 295 passed, zero
failed on net8.0, net9.0 and net10.0. All four net10 sample smokes: PASS.
W1309–W1310 remain before stable release. Live macOS/Linux checks: **Not
tested**.
