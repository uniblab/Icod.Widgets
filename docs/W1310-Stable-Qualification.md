# W1310 Stable 1.3.0 Qualification and Owner Handoff

**Source:** PR #6, stable 1.3.0 branch source.<br>
**Package:** `Icod.Widgets 1.3.0` (local artifact; owner controls tag and publication).<br>
**Assembly:** `1.0.0.0`; **direct dependency:** `Icod.DCurses 2.5.0`.<br>
**Local environment:** Linux x64, .NET SDK 10.0.401/runtime 10.0.12.

## Local source and compatibility

Staging and Release solution builds finished with zero warnings/errors. The
297-test xUnit suite passed with zero failures and zero skips on each of
net8.0, net9.0 and net10.0 in both configurations. Gallery, Editor,
Roguelike and Workspace `--smoke` paths all passed in both configurations.
The gallery smoke renders seven pages under three themes and checks retained
focus. A final screen test verifies disabled selection precedence and
per-action dialog styles.

The [1.3 public API baseline](Public-API-Baseline-1.3.md) records 134 exported
types and zero removed types/members against 1.2. A binary consumer compiled
against each published 1.0, 1.1, and 1.2 package exited zero after substituting
the final 1.3 DLL. An exact package-only consumer restored from a **fresh
isolated NuGet cache**, built across all three target frameworks with no
warnings/errors, and ran on net10 with exit code zero.

## Exact local artifacts

The [PR description](https://github.com/uniblab/Icod.Widgets/pull/6)
records SHA-256 values for the exact artifacts rebuilt at its final source
commit. NuGet source metadata embeds the repository commit, so a build before
this qualification record is committed has a different hash.

The nupkg nuspec declares version 1.3.0 and only `Icod.DCurses 2.5.0` in
each net8/9/10 dependency group. It has a DLL/XML pair for each target,
README, LGPL license and icon, with no sample/test artifacts. The snupkg has
PDBs for all three targets. The packaged README gives an install command for
1.3.0, the runnable gallery, full GitHub URLs and no prerelease timing claims.
Release notes, changelog, root README, samples and package metadata agree.

The prior RC PR [workflow run](https://github.com/uniblab/Icod.Widgets/actions/runs/38320398369)
passed metadata, Windows, Linux and macOS jobs, including restore/build/test,
sample smokes, exact Staging package checks and the package-only consumer.
The final stable source commit's own PR matrix must be inspected separately
after push; the RC run does not qualify subsequent source changes. Automated
CI is not a live terminal color observation. Interactive Windows, macOS and
Linux terminal checks are **Not tested** in this environment.

The owner reviews final PR CI, merges, creates `v1.3.0`, and publishes through
the configured tag workflow. No merge, tag or publication is performed here.
