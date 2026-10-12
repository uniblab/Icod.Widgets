# Icod.Widgets 1.3 Public API Baseline

**Package:** `1.3.0`<br>
**Assembly version:** `1.0.0.0`<br>
**Targets:** net8.0, net9.0, net10.0<br>
**Sole direct dependency:** `Icod.DCurses 2.5.0`

The .NET 10 reflection snapshot exports 134 public types. Exact declared
member comparison with [1.2](Public-API-Fingerprint-1.2.json) found every one
of its 130 types and every recorded member present. The four added types are
`WidgetPalette`, `WidgetPaletteVariant`, `WidgetStyleRole`, and `WidgetStyles`.
The full new surface is [this fingerprint](Public-API-Fingerprint-1.3.json).
The positional six-role `WidgetTheme` constructor/deconstructor, equality and
`with` remain. Scope and live theme replacement add members to `Widget` and
`WidgetHost` without changing their old signatures.

Consumers compiled against the 1.0.0, 1.1.0, and 1.2.0 packages ran with the
1.3 DLL substituted. A package-only consumer compiled the new palette/part
APIs from the exact 1.3 package. Style roles carry complete DCurses styles;
color capability selection makes requests, not physical color guarantees.
