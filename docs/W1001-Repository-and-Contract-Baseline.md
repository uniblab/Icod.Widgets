# W1001 — Repository and Contract Baseline

**Status:** complete  
**Package:** `Icod.Widgets 1.0.0-alpha.1`

## Dependency result

`Icod.DCurses 2.3.1` restores for `net8.0`, `net9.0`, and `net10.0` and remains
the sole direct production dependency. The restored package SHA-256 is
`f8c52b91cbd8ac23acce6413e4646ce9527e662333e9d9b61870d24ef710096c`.

The public mapping uses `CursesSession.Screen`, `CursesScreen`,
`CursesInteractionRouter`, `CursesInteractionRegion`, `CursesRectangle`,
`CursesText`, and the public members of `CursesInputEvent`. DCurses input-event
factory methods are internal, so Widgets exposes a small public `WidgetInput`
value abstraction for deterministic replay and converts real
`CursesInputEvent` instances at the host boundary.

## Verification

The repository now has one explicitly sourced DLL project, one GPL test
project, shared C# 13 settings, Debug/Staging/Release configurations, and the
current repository build/release scripts. `samples/` and `tests/` are excluded
from the library compile and package payload.

The Staging solution build passed for all three target frameworks with zero
warnings and errors. The dependency-boundary suite passed on all targets. Pack
produced a library containing only README, LGPL terms, and DLL/XML assets for
`net8.0`, `net9.0`, and `net10.0`. Its nuspec declares
`LGPL-3.0-or-later` and exactly one direct dependency per target:
`Icod.DCurses 2.3.1`.

The first W1002 tests were compiled before implementation and failed for the
expected missing `Widget` and `WidgetInput` contracts. This establishes the
next behavioral checkpoint without claiming host or layout completion.
