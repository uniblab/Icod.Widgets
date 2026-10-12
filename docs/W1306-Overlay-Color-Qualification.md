# W1306 menus, drop-downs, dialogs and pickers

Source: `Icod.Widgets 1.3.0-alpha.1` on PR #6; direct dependency
`Icod.DCurses 2.5.0`.

Popups fill their bounded surfaces and rows before writing content. Borders,
focused/disabled items, check indicators and shortcut legends use independent
roles. Closed drop-down and menu surfaces repaint on theme changes. Dialog
captions use `DialogTitle` separately from `DialogBorder`; dialog messages,
actions and error rows resolve scoped styles. Picker paths, listings, errors
and footer use the modal owner's style scope. Compact and framed rendering
keep the existing interaction and Escape behavior.

Screen tests for framed drop-down styling and dialog caption separation failed
before implementation and now pass. A menu test checks border, selected row,
checked marker and shortcut legend separately. Release solution build: zero
warnings/errors. Full tests: 293 passed, zero failed on net8.0, net9.0 and
net10.0. Gallery, Editor, Roguelike and Workspace net10 `--smoke`: all PASS.

Remaining W1307 capability fallback, W1308 sample/doc and W1309–W1310 package
qualification. Interactive macOS/Linux checks: **Not tested**.
