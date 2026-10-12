# W1309 Release Candidate Qualification

**Candidate:** `Icod.Widgets 1.3.0-rc.1`, built locally from PR #6 on Linux
x64 with .NET SDK 10.0.401/runtime 10.0.12. Assembly version `1.0.0.0`;
sole direct dependency `Icod.DCurses 2.5.0`. No prerelease publication.

The overlay staging screen is reused across renders at a fixed screen size,
avoiding one screen-sized staging allocation for every overlay repaint.
A 100-render 80×25 modal theme-switch measurement allocated 69,230,528 bytes
on this runtime (about 692 KiB/render), below the 100 MB regression budget;
DCurses cell presentation and dialog text layout still allocate. The test
checks focus retention. This is a local observation, not a terminal-wide
frame-rate guarantee.

The .NET 10 reflection comparison found 134 public types against 130 in the
1.2 baseline. All 130 prior types and all their declared public members remain;
the four added types are `WidgetPalette`, `WidgetPaletteVariant`,
`WidgetStyleRole` and `WidgetStyles`. The six-argument `WidgetTheme` record
constructor, deconstructor and positional properties remain. Consumer binaries
compiled against published 1.0.0, 1.1.0 and 1.2.0 ran with the RC DLL swapped
in, each exiting zero. A fresh package-only consumer used the new part-style,
palette and theme-switch APIs and built on net8/9/10, then ran on net10.

Staging and Release solution builds: zero warnings/errors. Full xUnit suite:
296 passed, zero failed on each net8.0, net9.0, net10.0 in both configurations.
Gallery, Editor, Roguelike and Workspace smokes: PASS in both configurations.
The local RC nupkg/snupkg contain all three framework assets; the nupkg has
README, LICENSE, icon, one `Icod.DCurses 2.5.0` direct dependency per target,
and no samples/tests.

| Artifact | SHA-256 |
| --- | --- |
| `Icod.Widgets.1.3.0-rc.1.nupkg` | `17995c951e7842079fd65cb8393d8b65a7b6da8e0a33325ea2e5eec1a79d80ed` |
| `Icod.Widgets.1.3.0-rc.1.snupkg` | `0b53debb7b7457b44d5321a234149d8c18e3db05cb759252676d2268153f895d` |

PR matrix for this candidate has not been observed. Windows/macOS/Linux live
terminal observations are **Not tested** here; automated CI is a separate
gate. W1310 will use exact stable-version artifacts and update the public
README, API fingerprint, changelog and release notes before maintainer review.
