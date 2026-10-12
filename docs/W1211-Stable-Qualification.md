# W1211 Stable Qualification and Owner Handoff

**Source:** PR #5, `docs/1.2.0-document-workspace-design`; qualified implementation tree `0d22184e570e38d55b6003fe3ea2c8590bf8013b` at remote commit `2264b35d1e37ed32a429b7973797b9fc14f0e721`<br>
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
| `dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Staging -f net10.0 --no-build --no-restore` | 280 passed, zero failed |
| Same test command with `-c Release` | 280 passed, zero failed |
| Gallery, Editor, Roguelike, Workspace `dotnet run --project samples/Icod.Widgets.NAME.Sample -c CONFIG -f net10.0 --no-build --no-restore -- --smoke` | All four PASS in both Staging and Release |
| `dotnet pack Icod.Widgets.csproj -c Release --no-build --no-restore -o artifacts/packages` | Exact stable nupkg and snupkg created |

The content-host invariant was tested red and then green through the base
`WidgetContainer.Add` reference; a caller cannot add a second child through that
public entry point. The original public `Add` signature remains unchanged.

The final workspace smoke also closes every document and exercises disabled
document commands in the resulting empty workspace. It verifies F6/Shift+F6
focus movement and F3 Find Next after the Find form closes. A focused rendering
test verifies that a Find-only form does not advertise Replace.

GitHub Actions [PR run 38285456464](https://github.com/uniblab/Icod.Widgets/actions/runs/38285456464)
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
| `Icod.Widgets.1.2.0.nupkg` | `912382b2aa6083460ee1bc0787a9ec72ccea2a12428ea61dc897963a867e3462` |
| `Icod.Widgets.1.2.0.snupkg` | `64b4318c05c5888642a05525ea9ab5586d5be5b389232da505f6a122459dcb36` |

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
