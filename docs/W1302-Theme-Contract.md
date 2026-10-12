# W1302 — opt-in part styles

`WidgetStyleRole` is a finite set of widget parts and states.
`WidgetStyles` is immutable: `With` and `Without` return new values and
structural equality keeps `WidgetTheme` record equality meaningful.
`WidgetTheme.Parts` is an optional non-positional addition. The six existing
constructor/deconstruction members, `with`, and terminal-default theme remain.
Each absent part resolves through its legacy semantic role to a complete
`CursesStyle`, so neither color axis is overwritten independently by Widgets.

`Widget.Styles` applies an optional local style set to a control/subtree.
The nearest defined role wins; a missing role falls back through ancestors
and finally through the host theme. Clearing an override returns to inherited
behavior without modifying the shared theme. The application owns repaint.

The new three focused tests failed compilation against the prior public API,
then passed after implementation. Full Release tests on Linux x64 passed
283/283 on each of net8, net9, net10 using runtime 10.0.12 and roll-forward
for net8/net9. No live terminal observation is claimed. The sole direct
dependency remains DCurses 2.5.0; source identity is `1.3.0-alpha.1`.
