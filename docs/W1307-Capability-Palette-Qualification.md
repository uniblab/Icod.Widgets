# W1307 capability palette

Source: `Icod.Widgets 1.3.0-alpha.1` on PR #6; direct dependency
`Icod.DCurses 2.5.0`.

`WidgetPalette.Create(CursesPresentationCapabilities, WidgetPaletteVariant)`
selects dark/light themes using public DCurses observations. RGB requests
require direct RGB advertisement; otherwise indexed requests remain strictly
within the advertised count. Foreground and background use independent
selectors. Unadvertised color axes retain terminal-default requests. Only
advertised text attributes are requested, and attributes restricted during
color use are omitted. No terminal palette mutation or color quantization is
performed. If a terminal advertises neither color nor attributes, the palette
uses default styles and controls retain their text markers, borders and
structural cues; it cannot promise distinguishable cell rendition.

The palette API test initially failed to compile. Focused tests now exercise
zero color, attribute-only, indexed foreground with unavailable background,
and direct RGB background with unavailable foreground. Release solution build:
zero warnings/errors. Full tests: 295 passed, zero failed on net8.0, net9.0
and net10.0. Gallery, Editor, Roguelike and Workspace net10 `--smoke`: PASS.

Remaining W1308 public gallery/doc; W1309–W1310 package qualification.
Interactive macOS/Linux terminal checks: **Not tested**.
