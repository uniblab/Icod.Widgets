# W1304 core controls and text editing

Source: `Icod.Widgets 1.3.0-alpha.1` on PR #6; direct dependency `Icod.DCurses 2.5.0`.

Labels, buttons, checks, radios, borders, lists, indicators, text fields and the
editor now resolve scoped part styles. Owned blank bounds and empty list rows
receive explicit surface styles. Selected list rows style the entire row; the
editor and text field use separate surface, text and selection roles. An
unfocused selection has a separate editor role. Legacy focused text falls back
to the existing focused role when no part text style is present.

The new screen-only tests first failed, then passed after the renderer changes.
They inspect selected rows, trailing blanks, field text/selection and both
columns of a selected wide character. `dotnet build Icod.Widgets.sln -c Release
--no-restore`: zero warnings/errors. `dotnet test
tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release --no-restore`:
287 passed, zero failed on each of net8.0, net9.0 and net10.0. Gallery,
Editor, Roguelike and Workspace net10 `--smoke` all report PASS.

Remaining: tab/status/scroll/game parts, overlay/picker parts, capability
palette, public sample and package qualification. Terminal visual checks on
macOS and Linux are **Not tested**; CI will supply automated coverage.
