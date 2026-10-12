# W1211 Stable Qualification and Owner Handoff

**Source:** PR #5, `docs/1.2.0-document-workspace-design`; qualified implementation tree `6d5c5e39e1baab682cbff2b6b194f8273b3a3425` at remote commit `8c61d0a467e582d2a8733dcc8b985f9abd5c29d2`<br>
**Package:** `Icod.Widgets 1.2.0`<br>
**Assembly:** `1.0.0.0`<br>
**Direct dependency:** `Icod.DCurses 2.4.0`<br>
**Local environment:** Linux x64, .NET SDK 10.0.401/runtime 10.0.12

## Exact source checks

From the repository root, using the locally installed SDK and restored packages:

| Check | Outcome |
| --- | --- |
| `dotnet build Icod.Widgets.sln -c Staging --no-restore` | All net8/net9/net10 targets and sample/test projects compiled; zero warnings/errors |
| `dotnet build Icod.Widgets.sln -c Release --no-restore` | All targets compiled; zero warnings/errors |
| `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Staging -f net10.0 --no-build --no-restore` | 279 passed, zero failed |
| Same test command with `-c Release` | 279 passed, zero failed |
| Gallery, Editor, Roguelike, Workspace `dotnet run --project samples/Icod.Widgets.NAME.Sample -c CONFIG -f net10.0 --no-build --no-restore -- --smoke` | All four PASS in both Staging and Release |
| `dotnet pack Icod.Widgets.csproj -c Release --no-build --no-restore -o artifacts/packages` | Exact stable nupkg and snupkg created |

The content-host invariant was tested red and then green through the base
`WidgetContainer.Add` reference; a caller cannot add a second child through that
public entry point. The original public `Add` signature remains unchanged.

GitHub Actions [PR run 38282514083](https://github.com/uniblab/Icod.Widgets/actions/runs/38282514083)
at the qualified implementation tree completed successfully: metadata, Windows,
Linux, and macOS jobs all concluded **success**. Its matrix ran Staging restore,
build, tests, sample smokes, package checks, and package-only consumer. These
are automated CI checks, not live interactive-terminal checks.

## Artifact and compatibility

The stable `.nupkg` nuspec declares `1.2.0` and one direct `Icod.DCurses 2.4.0`
dependency in each net8.0/net9.0/net10.0 group. The archive contains DLL/XML
assets for all three targets, README, LICENSE, and icon, with no samples or
tests. The packaged README has a 1.2.0 installation command and runnable
workspace command, full external GitHub links, and no prerelease or temporary
publication language. The `.snupkg` has PDBs for all three targets.

| Local artifact | SHA-256 |
| --- | --- |
| `Icod.Widgets.1.2.0.nupkg` | `bfa9418841dd0eca04741c96a31a1d84898e7753f8d703db11dcd04e518737f6` |
| `Icod.Widgets.1.2.0.snupkg` | `e12ce7a1a7f36d24837f3e43c2cc62e526f2665f2e5cbc0340e9c1a2dae0ab06` |

A fresh external `PackageConsumer` restored from the local stable artifact,
compiled for net8/net9/net10 in Staging with zero warnings/errors, and ran on
net10 with exit code zero. The binary compatibility consumer was compiled
against each published 1.0.0 and 1.1.0 baseline, then executed with the final
stable 1.2.0 DLL substituted; both exited zero. Fresh reflection found 130
exported types versus 103 at 1.1, no removed public types or declared members,
and assembly version `1.0.0.0`. See the
[API baseline](Public-API-Baseline-1.2.md) and
[RC measurement record](W1210-RC-Qualification.md).

## Scope and owner gate

Only .NET 10 is installed in this local environment. net8/net9 builds and the
package-only consumer compiled, but their runtime tests were **Not tested** here.
Windows, macOS, and Linux live terminal interactions were **Not tested** here;
headless sample smokes do not qualify interactive rendering/input. The remote
PR platform matrix is recorded above. PowerShell
package-verifier execution was **Not tested**; direct archive/nuspec inspection
is recorded above.

The owner reviews PR checks, merges, creates `v1.2.0`, and publishes through the
configured tag workflow. Merge, tag creation, and publication remain **pending**.
