# W1101 — Contract and Storage Decision

**Candidate:** `1.1.0-alpha.1`  
**Baseline:** `471179e0afd0047fc69460dd50e9272905560dc6`  
**Measured environment:** Ubuntu 24.04, x64, .NET SDK 10.0.401/runtime 10.0.12  
**Dependency decision:** sole direct runtime dependency `Icod.DCurses 2.4.0`; `Icod.Terminal 1.29.0` remains transitive

## Compatibility baseline

The package version advances independently from assembly identity. Keep
`AssemblyVersion` at `1.0.0.0` for the 1.x line. Existing public types, enum
numeric values, constructors/factories, positional records, and default behavior
remain the compatibility floor. New APIs are additive.

The Staging library builds without warnings for net8.0, net9.0, and net10.0.
The net10.0 baseline passed 129/129 tests. The sandbox does not contain the
.NET 8 or 9 runtime, so execution of those target test hosts remains a PR-CI
gate. A consumer compiled from the released Icod.Widgets 1.0.0 package ran
successfully after replacing its runtime Icod.Widgets/DCurses assemblies with
the 1.1.0-alpha.1/2.4.0 candidate assemblies.

## Storage experiment

`tests/EditorMeasurements` is a GPL, non-packable executable. It applies the
same deterministic replacement trace to a gap buffer and a piece-table reference
candidate, compares their final UTF-16 content to a string oracle, retains
snapshots, and reads a bounded 65,536-unit slice. The list-backed piece table is
an experiment, not the selected production index.

Representative Release results:

| Candidate | Initial units | Operations | Elapsed | Allocated | Retained snapshots | Final fragments |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Gap buffer | 4,096 | 2,400 | 1.00 ms | 242,616 B | 9 | 1 |
| Piece table | 4,096 | 2,400 | 177.42 ms | 576,728 B | 9 | 2,974 |
| Gap buffer | 1,300,000 (100,000 lines) | 77 | 6.68 ms | 52,265,232 B | 10 | 1 |
| Piece table | 1,300,000 (100,000 lines) | 77 | 0.14 ms | 277,360 B | 10 | 110 |
| Gap buffer | 10,000,000 (one line) | 77 | 128.17 ms | 400,265,232 B | 10 | 1 |
| Piece table | 10,000,000 (one line) | 77 | 0.50 ms | 277,360 B | 10 | 110 |
| Gap buffer | 32 mixed Unicode/EOL units | 2,400 | 0.16 ms | 77,032 B | 9 | 1 |
| Piece table | 32 mixed Unicode/EOL units | 2,400 | 27.37 ms | 346,144 B | 9 | 2,479 |

The gap buffer is simpler and faster for small local text but retaining snapshots
copies the whole large buffer. The piece table retains large-document snapshots
with bounded additional allocation and handles distant edits without moving the
whole document; its list implementation degrades with fragmentation.

**Decision:** use a private persistent balanced piece tree with subtree UTF-16
length and newline aggregates. Coalesce adjacent compatible pieces and rebuild
when the frozen fragmentation budget is reached. Snapshots retain an immutable
root. Public APIs expose document/snapshot behavior, never pieces or tree nodes.
Line lookup descends newline aggregates. This combines the measured large-source
and snapshot advantage with logarithmic lookup/edit behavior missing from the
reference list.

History is deliberately a layer above storage. W1105 measures actual inverse
edits, grouped history and eviction against the frozen retained-unit budget;
the storage experiment must not hide history ownership inside a candidate.

## Frozen resource contracts

All text counts are UTF-16 code units unless stated otherwise. Defaults are
constructor options; hard maxima reject configuration. Arithmetic is checked.
Expected limit failures are typed and leave document, view, history, actions,
and pending services unchanged. Nothing truncates silently.

| Resource | Default | Hard maximum | Limit outcome |
| --- | ---: | ---: | --- |
| Document length | 268,435,456 units | 1,073,741,823 units | Construction/edit rejects before mutation |
| Single edit / complete paste | 16,777,216 units | 268,435,456 units | Transaction rejects atomically |
| History groups | 4,096 | 65,536 | Evict oldest complete reachable groups |
| History retained deleted+inserted text | 67,108,864 units | 536,870,912 units | Evict whole groups; reject one group larger than budget |
| Live snapshots | 16 | 128 | New snapshot rejected; existing snapshots retained |
| Projection source slice | 65,536 units | 1,048,576 units | Continue on a later update/row; never split a cluster |
| Pathological cluster scan | 4,096 units | 65,536 units | Render one safe placeholder and expose continuation |
| Projection cache | 512 rows / 1,048,576 units | 4,096 rows / 16,777,216 units | Evict least-recent unaffected entries |
| Work per caller update | 64 edit/layout/autoscroll steps | 1,024 steps | Return with pending work; caller supplies the next tick |
| Clipboard transport | DCurses 65,536 bytes read and write | DCurses 16 MiB each | Typed TooLarge; application may configure session before open |

Piece fragmentation is limited to 1,048,576 pieces or one piece per eight live
text units, whichever is lower but never below 4,096 pieces. Crossing the soft
threshold schedules bounded coalescing/rebuild work; an edit that would exceed
the hard limit is rejected before mutation.

## Editing policy defaults

- Inserted newline: `\n`; an application such as dedit supplies the active
  document policy when another sequence is required. Existing endings are never normalized.
- Tab width: 4 cells. A Tab command inserts `\t`; indentation uses `\t` by
  default and may be configured to spaces. Source tabs remain tabs.
- Typing coalescence: 750 ms of monotonic caller time, same view, adjacent insert
  or delete, no selection/navigation/focus/checkpoint/paste boundary.
- Word movement: Unicode letters, decimal digits, connector punctuation and
  combining marks form words; whitespace forms runs; all other symbols and
  punctuation form separate runs. Movement always lands on a text-element boundary.
- No-wrap is the default. Wrapped extents may be explicitly estimated until
  indexed; an estimate is never labeled exact.

## Proposed public seams

The exact names in the approved 1.1 plan are accepted as the implementation
starting point: additive rich-input metadata/factories; `WidgetHost.Update`,
`MoveFocus`, and `ExecuteEditCommand`; protected bounded action/update/capture
hooks; `WidgetTextDocument`/snapshot/edit/history contracts; independent
`WidgetTextViewState`; `WidgetTextEditor`; `WidgetScrollBar`; typed clipboard
request/results; and focused `WidgetCaretIntent`. Public API freezes only at W1112.

## Lower-layer gap decision

No DCurses gap is demonstrated. Version 2.4.0 supplies public Unicode layout,
bounded visible projection mechanisms, interaction routing/capture, explicit
clipboard operations, and reversible cursor-style leases. Widgets implements
document state and policy above those APIs; no new lower-layer release is assumed.
