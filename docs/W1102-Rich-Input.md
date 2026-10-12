# W1102 — Rich input preservation

Icod.Widgets 1.1 preserves the normalized metadata supplied by Icod.DCurses 2.4
instead of reducing every event to the original Widgets 1.0 key model.

## Additive contract

`WidgetInput` now carries the numbered function-key identity, key phase, focus
state, bracketed-paste phase, shifted and base-layout characters, associated
text, and the complete source modifier flags. The original `Modifiers` property
remains the compatible Shift/Control/Alt projection. Existing `Key` inputs mean
a press, and existing `Paste(string)` inputs mean one complete paste.

Public factories make numbered function keys, paste frames, and focus reports
replayable without exposing DCurses' internal event factories. Function-key
numbers use the existing DCurses range, 0 through 63.

`WidgetMenuShortcut.FunctionKeyNumber` is a non-positional init property. This
retains the original constructor and deconstruction while allowing F6 and F10 to
be distinct shortcuts. A legacy unnumbered `WidgetKey.Function` shortcut still
matches any numbered function-key input.

## Verification boundary

DCurses intentionally creates `CursesInputEvent` values only through a live
`CursesSession`; its event factories and terminal decoder are not public. The
focused unit tests therefore cover the public replay factories and shortcut
identity without reflection or friend access. The `WidgetInput.From` conversion
is exercised through the public session path in W1112 integration and sample
qualification, including repeat/release, associated text, extended modifiers,
paste framing, and focus state.
