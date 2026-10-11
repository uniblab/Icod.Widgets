# W1112 — Release-Candidate Qualification

**Candidate:** `1.1.0-rc.1`
**Implementation base:** `6c93af5542708f3dfa7ac2effcf806208b4d4b07` plus this W1112 commit
**Measured environment:** Ubuntu 24.04, x64, .NET SDK 10.0.401/runtime 10.0.12
**Assembly version:** `1.0.0.0`
**Direct dependency:** `Icod.DCurses 2.4.0`

## Public behavior witnesses

The package-facing qualification scenarios use only public APIs. They cover exact
mixed-EOL construction, selection replacement, undo to a checkpoint, denied Cut,
stale Paste, focused field/editor command scope, repeated attach/resize/modal/
shutdown, snapshots, a custom action-emitting widget, and screen-only hosting.

The editor sample now loads a bounded existing file without normalization, replaces
its in-memory document, edits through `WidgetTextEditor`, reports modified state,
supports contextual Ctrl+Z/Y/X/C/V/A and F6 focus escape, and applies clipboard
results on the application loop. It has no save command and never writes files.
Its deterministic smoke verifies an exact CRLF/LF document, edit, undo back to
clean state, and picker cancellation.

## Measurement audit

Release measurements used `tests/EditorMeasurements` on the environment above.
Times and allocations are diagnostic, not cross-machine promises.

| Production workload | Result |
| --- | --- |
| Load/index 1,300,000 units / 100,001 lines | 9.49 ms; 2,737,736 B allocated |
| Load/index 10,000,000-unit line | 28.91 ms; 20,210,920 B allocated |
| Sixteen immutable snapshots | 2,736 B additional allocation per fixture |
| 5,000 distant edits + 4,096 retained undos | 21.06 ms; 9,105,960 B allocated; retained snapshot exact |
| 1,300,000-unit editor cold first render | 52.35 ms; 3,649,568 B allocated |
| Same editor, 100 warm renders | 315.13 ms; 45,326,584 B allocated |
| 10,000,000-unit line, cold far seek | 45.17 ms first bounded pass; 160 progressive passes to requested region |
| Same long line, 100 warm renders | 32.56 ms; 6,367,480 B allocated |
| 100,002-unit pathological cluster, cold | 1.98 ms; 632,560 B allocated; source preserved |
| Same cluster, 100 warm renders | 29.46 ms; 1,019,240 B allocated |
| 256 paste groups with 32-group history cap | 0.45 ms; 374,488 B; exactly 32 undos; retained snapshot exact |

Far horizontal discovery advances by the frozen 65,536-unit projection budget;
it does not scan or allocate an unbounded display width in one call. Once the
requested region is indexed, the visible entry is cached and warm work is bounded.
The measured retained deltas were approximately 2.9 MiB for many lines, 20.3 MiB
for the 10-million-unit line, and 0.44 MiB for the pathological cluster. No frozen
W1101 ceiling was loosened.

## Compatibility and API freeze

The public reflection fingerprint contains 103 exported `Icod.Widgets` types.
An exact declared-member comparison against `Public-API-Fingerprint-1.0.json`
found no removed 1.0 type or member. Assembly identity remains 1.0.0.0. The W1101
binary witness compiled against released 1.0.0 ran with the candidate assembly;
the new package-only consumer compiles for net8.0/net9.0/net10.0 from the exact RC
artifact and runs successfully on the installed net10.0 runtime.
The consumer defaults to net10.0 so the shared workflow's unqualified `dotnet run`
remains valid; `-p:PackageConsumerAllFrameworks=true` enables the three-TFM compile gate.

The 1.1 names and signatures are frozen in `Public-API-Baseline-1.1.md` and
`Public-API-Fingerprint-1.1.json`. Further changes require compatibility review.

## Automated and artifact results

- Staging solution build: net8.0/net9.0/net10.0, 0 warnings, 0 errors.
- Staging net10.0 tests: 233 passed, 0 failed.
- Release solution build: net8.0/net9.0/net10.0, 0 warnings, 0 errors.
- Release net10.0 tests: 233 passed, 0 failed.
- Gallery, editor, and roguelike Release/net10.0 smoke modes: passed.
- Package-only consumer: all three TFMs compiled; net10.0 execution passed.
- `Icod.Widgets.1.1.0-rc.1.nupkg`: expected README/license/icon, DLL/XML for all
  three TFMs, only DCurses 2.4.0 direct dependency, no sample/test binaries.
- RC nupkg SHA-256: `d32baf5724464b49bcff0bef456973633e2b797d0ce99ba87dfce53114a06818`.
- RC snupkg SHA-256: `f6be419dc89a0da03d9490d6105b5705a39a4d68d177ada5bb51c3c2185e0612`.
- Packaged README link scan: all Markdown links use HTTP(S) URLs.

PowerShell is not installed in this environment, so the PowerShell artifact
wrapper could not run; the archive and nuspec checks above inspected the exact
package directly. .NET 8/9 target assemblies compile here, but those runtimes and
live terminals are not installed. Their execution and Windows/macOS/Linux live
behavior remain explicit CI/operator gates for W1113.
