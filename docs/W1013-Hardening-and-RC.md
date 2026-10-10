# W1013 — Hardening, API Freeze, and Release Candidate

**Status:** complete  
**Candidate:** `1.0.0-rc.1`

The complete solution built in Staging for net8/net9/net10 with zero warnings
and errors. All 69 deterministic tests passed independently on each framework.
Qualification includes the 1,024-widget, 4,096-action, 100,000-item, 16-modal,
and 16-submenu boundaries; repeated attach/detach; compact geometry; Unicode;
stale asynchronous completion; and host disposal.

Hardening added pointer-capture release on outside release/removal/disable and
invalidates pending file validation whenever the selection basket changes. The
new adversarial tests failed before those fixes and pass afterward.

All three net10 sample smoke paths passed. The package-only consumer restored,
built, and ran against the local `1.0.0-rc.1` package without a project
reference. The evidence package SHA-256 is
`f948fad94263e51154551541bb589ad9d8c5fb4c25a23ad9d44e7424b871ee21`.
Its nuspec declares LGPL-3.0-or-later and only `Icod.DCurses 2.3.1`; its payload
contains README, license, and DLL/XML assets for the three targets, with no GPL
sample/test content. Authored C# header/SPDX audit passed.

The reflection-derived public API fingerprint records the release-candidate
surface and assembly version `1.0.0.0`. W1014 remains open for exact-head live
Windows/macOS/Linux terminal and applicable architecture evidence before stable
metadata, tag, or publication.
