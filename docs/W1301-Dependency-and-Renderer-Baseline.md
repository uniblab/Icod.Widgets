# W1301 — DCurses 2.5 dependency and renderer baseline

**Source identity:** `1.3.0-alpha.1`; `AssemblyVersion` remains `1.0.0.0`.\
**Direct runtime dependency:** published `Icod.DCurses 2.5.0` only; `Icod.Terminal 1.29.0` is transitive.\
**Environment:** Linux x64, .NET SDK 10.0.401/runtime 10.0.12; net8/net9 test execution uses `DOTNET_ROLL_FORWARD=Major`.

The dependency assertion was changed first and failed with the csproj still
declaring 2.4.0. After the dependency and development source identity were
updated, restore of the whole solution succeeded against the 2.5.0 package.
The source edits in this tranche affect project metadata, the package-only
consumer's expected development version, a dependency assertion, and this
roadmap/evidence. Runtime code and the public API remain unchanged.

## Local checks

- `dotnet restore Icod.Widgets.sln -p:NuGetAudit=false`: all solution projects restored.
- `dotnet build Icod.Widgets.sln -c Release --no-restore -m:1 -nr:false`: zero warnings, zero errors.
- `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release --no-restore -m:1 -nr:false`: 280/280 on each of net8, net9 and net10; zero failures/skips.
- Gallery, Editor, Roguelike and Workspace `dotnet run --no-build --no-restore -- --smoke` on net10: all four reported PASS.

Local VSTest required a loopback socket outside the restricted network
namespace. Full cross-platform CI is a separate PR gate, not inferred from
these Linux results. No live terminal color observation has been made.

## Renderer ownership inventory

| Code area | Current rendering behavior | W13xx target |
| --- | --- | --- |
| `src/Core/WidgetTheme.cs` and `WidgetHost.cs` | Six complete shared styles; host theme fixed at construction | Add immutable part/state roles, scoped overrides and caller-driven replacement |
| `src/Core/WidgetHost.Presentation.cs` | Render overlay on a fresh screen and copy its bounds into a panel | Fill opaque owned cells with resolved surface style; preserve borrowed background |
| `src/Controls/` | Editor and text input fill bounds; most other controls write only their text; scrollbar shares one style | Distinct surface/selection/track/thumb/disabled/focus roles and complete owned cells |
| `src/Workspace/WidgetTabStrip.cs` | Fills strip, active tab uses `Focused` only while the strip has focus | Separate active state, focus, marker, close and overflow styles |
| `src/Menus/` and `WidgetDropDown.cs` | Popup border and item text have broad styles; padding can inherit a default blank | Styled popup surface, border, item parts and all padding at compact sizes |
| `src/Dialogs/`, `src/Pickers/`, `src/Search/` | Framed dialogs fill rows; nested controls and browser rows use shared styles | Caption/interior/action/error roles and consistent child/overlay ownership |
| `src/GameUi/` | Status bar fills a line; history/indicators may only write occupied text | Styled backgrounds, action fields, prompts, list regions and empty rows |

The existing 1.2 public API fingerprint is
[`Public-API-Fingerprint-1.2.json`](Public-API-Fingerprint-1.2.json). No
new public runtime signature is introduced by W1301. Later tranches must
compare against it, including record construction, deconstruction and `with`.
