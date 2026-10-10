# W1004 — List Selection

**Status:** complete

`WidgetList` now supports stable-key single and multiple selection, an
independent cursor, disabled entries, Space toggle, Ctrl+A, Shift ranges,
Home/End/Page movement, scrolling, and 750 ms ordinal case-insensitive search.
`SetItems` validates and copies the complete replacement before mutation,
preserves enabled selected keys and the cursor key, and rejects duplicate keys
or more than 100,000 entries.

Rendering visits only rows intersecting the viewport. Focused tests exercise
selection preservation, disabled entries, range anchors, replacement,
transactional validation, search expiry, and the 100,000/100,001 boundary.
