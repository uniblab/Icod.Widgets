# W1013 — Resumed Interactive Acceptance Corrections

**Recovered checkpoint:** `5cb98d5296f1c074cf6aebfeb2ff1f810d63e686`
**Reconciled remote checkpoint:** `21aa2f6134d06afa144478a922d3305690d9083a`
**Candidate:** `1.0.0-rc.2`

Inspection of the recovered checkpoint found that its samples exited with informational text unless
`--smoke` was passed. Pickers provided state/validation APIs but no live listing,
navigation or selection UI; the wizard did not render its page/error. Consequently,
the rc.1 evidence did not establish complete interactive consumer acceptance.
The branch advanced during recovery with additional hardening and live sample
commits. Those fixes and tests were reconciled with this continuation; the
rc.2 result preserves them and completes roots/path/basket and retained overlays
before requesting live qualification.

## Changes

- Shared provider-backed picker view: listing/cursor/scrolling, current/path display,
  roots/drives, selected basket/removal, filters, pending/errors, keyboard/pointer input.
- Semantic load/validation actions; roots share directory identity/generation/loader
  guards. Applications retain all I/O and UI-loop result application.
- Real gallery/editor/roguelike loops, with raw input, optional input protocols,
  resize/rearrange, explicit refresh and cleanup. Drawing occurs on changes, not
  every timeout. Smoke paths reuse those controllers.
- DCurses retained panels preserve borrowed background cells; modal and popup scopes isolate focus/input, unwind on removal, and restore
  eligible focus. Modeless state and hidden-container eligibility are corrected.
- Unicode caret editing, Ctrl+A, pointer list selection, long popup scrolling,
  message wrapping, visible wizard/number errors, nonfinite-number rejection,
  stale direct-basket validation and loads after picker cancellation/reopening, and action-capacity mutation ordering corrected.
- Ctrl+S is a legacy-terminal-safe picker acceptance gesture; Ctrl+Enter remains
  available when distinguishable. No terminal protocol encoders were introduced.
- Sample CI workflows, API fingerprint, consumer version, README and sample
  acceptance instructions updated for rc.2. Library LGPL and GPL sample/test
  separation and sole direct DCurses dependency remain intact.

## Verification

New behavioral assertions were run RED before each corrective implementation.
The standard xUnit project remains the CI authority. This executor denies the
socket creation required by the test platform; a scratch in-process runner invokes
all Fact methods and awaits async methods for local verification. It does not
replace standard CI. Build/runtime/package results are recorded in the PR at the
pushed head; no operator terminal result is inferred from synthetic checks.

Local verification for rc.2: Staging solution build with zero warnings/errors;
107 behavioral tests passed on each of net8.0, net9.0 and net10.0; all three
sample smoke workflows passed on each framework (nine runs); Release package
build and package-only consumer succeeded. Package inspection confirmed the
three framework DLL/XML pairs, LGPL metadata and sole direct DCurses dependency,
with no sample/test payload. CI additionally runs the standard xUnit suite and
package consumer on all three operating systems.

A Linux test PTY with TERM=xterm-256color exercised gallery page navigation and
Ctrl+Q cleanup. This is a limited input/cleanup witness, not W1014 qualification.
`docs/W1014-Stable-Qualification.md` records all live OS/terminal gates as pending.

The continuation used the approved sequential primary-agent workflow. Final
review is an author review; no independent review is claimed.
