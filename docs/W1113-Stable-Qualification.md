# W1113 — Stable Qualification and Owner Handoff

**Version:** `1.1.0`
**Assembly version:** `1.0.0.0`
**Direct dependency:** `Icod.DCurses 2.4.0`
**Preparation environment:** Ubuntu 24.04, x64, .NET SDK 10.0.401/runtime 10.0.12

## Stable-source checks

- Version and PackageVersion are exactly 1.1.0; the package-only consumer pins
  exactly 1.1.0. No alpha/RC reference remains in current package metadata.
- The library, samples, tests, and measurement project build for net8.0/net9.0/
  net10.0 with zero warnings and errors in Staging and Release.
- The installed net10.0 runtime passes all 233 tests and all three deterministic
  sample smokes. The exact package-only consumer compiles for all three TFMs and
  runs successfully on net10.0.
- `PackageConsumerAllFrameworks=true` enables the explicit net8/net9/net10
  compile gate while the workflow-compatible default run target remains net10.0.
- The frozen reflection fingerprint retains every recorded 1.0 type/member and
  reports assembly identity 1.0.0.0 across the 1.1 surface.
- Source/license headers, project headers, README HTTP(S) links, package metadata,
  sole direct dependency, and archive contents were inspected.

## Exact stable artifacts

The local Release package contains README, LGPL license, icon, and DLL/XML assets
for all three TFMs, with no sample/test binaries. The exact local hashes are:

- nupkg SHA-256: `4cec0e818054fb6a32802576fad03a87916546a54650feed57d46ab6574ca439`
- snupkg SHA-256: `46b5972721fc1af294a7bcfbbda93cdf119667015fd5f174acfbde68c049ee58`

The tag workflow must rebuild from the exact merged commit. Its SourceLink/repository
commit and hashes may therefore differ from this pre-merge artifact; version,
dependency, framework assets, and contents must remain equivalent.

## Live-terminal qualification boundary

No interactive live terminal is available in this preparation environment.
Windows, macOS, and Linux 1.1 live scenarios are therefore **Not tested**, not
passed by inference from CI or smoke mode. The owner has already accepted proceeding
without macOS/Linux live checks. Before tagging, exercise Windows when available:
Unicode typing/undo, both scroll axes, selection drag/autoscroll, resize during
input, read-only mode, clipboard success/denial/timeout/limit, insert/overwrite
caret restoration, menus/dialogs, and clean shutdown. Record terminal/runtime
details with the release if performed.

## Owner-controlled completion

1. Review and merge PR #4. The agent does not merge.
2. Confirm main validation at the exact merged SHA.
3. Confirm project/package/consumer version 1.1.0, assembly 1.0.0.0, and DCurses 2.4.0.
4. Create tag `v1.1.0` at that verified commit.
5. Let the configured tag workflow publish; verify NuGet and GitHub artifacts.

No merge, tag, push to a registry, or publication was performed during preparation.
