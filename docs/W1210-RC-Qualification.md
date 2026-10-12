# W1210 Release Candidate Qualification

**Candidate:** `1.2.0-rc.1`  
**Measured environment:** Linux x64, .NET SDK 10.0.401/runtime 10.0.12  
**Assembly version:** `1.0.0.0`  
**Direct dependency:** `Icod.DCurses 2.4.0`

## Public API and compatibility

The candidate exports 130 public types. The recorded 1.1 fingerprint has 103; exact comparison found zero removed public types or declared member signatures. A compatibility review restored the original menu factory overloads before freeze and added distinct focused-edit overloads. Consumers compiled against released 1.0.0 and 1.1.0 both executed successfully when the 1.2 candidate DLL replaced their resolved Widgets DLL. The package-only consumer restored from the exact local `1.2.0-rc.1` artifact, compiled for net8.0/net9.0/net10.0, and executed on net10.0.

## Measurement

`dotnet run --project tests/WorkspaceMeasurements/WorkspaceMeasurements.csproj -c Release` measured 10,000 content switches across three independent 16,777,216-unit documents: **102.6 ms total, 2,144 bytes allocated per switch**, below the 256 KiB ceiling. All document lengths and revisions remained unchanged. The 500-tab case kept tab `t499` visible at widths 1, 12, and 80 (visible tab counts 1, 1, and 4). One thousand menu presentation updates passed. These are observations for this runtime, not portable timing guarantees.

## Build, tests, samples, and package

- Staging and Release solution builds compiled all library, test, sample, and measurement projects for their target frameworks with zero warnings/errors.
- Standard net10 Release xUnit: 278 passed, zero failed. All four Gallery, Editor, Roguelike, Workspace headless smoke modes printed PASS.
- `dotnet pack Icod.Widgets.csproj -c Release -o artifacts/packages` produced matching `Icod.Widgets.1.2.0-rc.1.nupkg`/`.snupkg` files.
- Nuspec audit: version `1.2.0-rc.1`; one `Icod.DCurses 2.4.0` direct dependency per target group; DLL/XML for net8/net9/net10; README, LICENSE, and icon included; no sample/test paths.
- RC package SHA-256: `6173a3c1d52395f871e333d52472c563cb923a4014f8bc69e203372cb775968b`; symbol package SHA-256: `a819ce09f8c0a58f291b7ae1f7449ad3dcdb653a2975d47eaec5cfd26cc6e24e`.
- The repo's PR and release workflow smoke lists now include Workspace. The remote PR matrix at this candidate SHA remains to be observed.

Only the net10 runtime is installed here. net8/net9 source and external package consumers compiled, but their exact runtime execution is not claimed. PowerShell package-verifier and live Windows/macOS/Linux terminal sessions have not been run in this environment. The owner controls any prerelease publication; none was performed.
