# W1013 — Hardening, API Freeze, and Release Candidate

**Status:** complete  
**Candidate:** `1.0.0-rc.1`

The complete solution builds in Staging for net8/net9/net10 with zero warnings
and errors. All 86 deterministic tests pass independently on each framework.
Qualification includes the 1,024-widget, 4,096-action, 100,000-item, 16-modal,
and 16-submenu boundaries; repeated attach/detach; compact geometry; Unicode;
stale asynchronous completion; and host disposal.

Hardening added modal input isolation; ancestor effective state; pointer-capture
release on modal open, removal, and disable; action-queue preflight; container
cycle rejection; caret-based text editing; compact list/drop-down/menu scrolling;
complete wizard interaction; and keyboard-driven async pickers. Pending picker
validation is invalidated whenever selection or dialog lifecycle changes. The
new adversarial tests failed before those fixes and pass afterward.

All three net10 sample smoke paths run on every Windows, macOS, and Linux pull
request job. The package-only consumer restored, built, and ran against the
local `1.0.0-rc.1` package without a project reference. Its nuspec declares
LGPL-3.0-or-later and only `Icod.DCurses 2.3.1`; its payload contains README,
license, and DLL/XML assets for the three targets, with no GPL sample/test
content. Authored C# header/SPDX audit passed.

The reflection-derived public API fingerprint records the release-candidate
surface and assembly version `1.0.0.0`. W1014 remains open for exact-head live
Windows/macOS/Linux terminal and applicable architecture evidence before stable
metadata, tag, or publication.
