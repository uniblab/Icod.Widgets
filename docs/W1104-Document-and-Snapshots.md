# W1104 — Exact documents and immutable snapshots

`WidgetTextDocument` owns an exact copy of caller-supplied UTF-16 chunks. Chunk
boundaries have no semantic meaning: they may occur between CR and LF, between
surrogate code units, or inside a combining sequence without changing reads or
logical-line results. Empty text has one logical line, and a final newline is
followed by an empty logical line.

## Storage and coordinates

The private production store is the persistent balanced piece tree selected in
W1101. Leaves retain immutable strings; branches aggregate checked 64-bit UTF-16
length and logical newline counts. The newline summary treats CRLF as one ending
even when its two units occupy different leaves. Reads and line lookup descend
the tree without exposing pieces or returning the whole document.

Public offsets, lengths, line indexes, revisions, and state IDs use `long`.
`Read` is deliberately bounded by an `int` result length. `WidgetTextLine`
reports content and ending separately, preserving CRLF, CR, and LF exactly.

The document starts at revision/state zero and has an opaque `Guid` identity.
W1105 advances those stamps through transactions and history.

## Ownership and limits

Source memory is copied while the enumerable is consumed, so later mutation of
an input array cannot change the document. Default construction accepts at most
268,435,456 UTF-16 units; configuration cannot exceed 1,073,741,823.

A snapshot retains an immutable tree root plus document identity, revision, and
state. It is safe to read independently of later root replacement. Disposal is
idempotent and releases the document's live-snapshot slot; all access after
disposal throws `ObjectDisposedException`. The default live limit is 16 and the
hard configurable maximum is 128. A full snapshot set rejects a new snapshot
without evicting or invalidating an existing one.

## Measured evidence

Release measurements on the W1101 Ubuntu x64/.NET 10 environment used 4,093-unit
source chunks and retained all 16 default snapshots:

| Fixture | Logical lines | Load time | Load allocation | 16 snapshot allocation |
| --- | ---: | ---: | ---: | ---: |
| 1,300,000 units / 100,000 CRLF lines | 100,001 | 4.68 ms | 2,737,576 B | 2,736 B |
| 10,000,000-unit single line | 1 | 17.54 ms | 20,210,784 B | 2,736 B |
| 32 mixed Unicode/EOL units | 5 | 0.01 ms | 131,488 B | 2,736 B |

The snapshot allocation is independent of document size because snapshots share
the immutable root. Tests cover every split offset in a mixed fixture, a forced
cross-leaf CRLF, multi-leaf indexing, mutable-source isolation, randomized reads
and line oracles, disposal, and capacity rejection.
