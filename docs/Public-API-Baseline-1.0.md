# Icod.Widgets 1.0 Public API Baseline

**Candidate:** `1.0.0-rc.1`  
**Assembly version:** `1.0.0.0`  
**Targets:** `net8.0`, `net9.0`, `net10.0`

The 1.0 candidate exposes a retained `Widget` tree hosted by `WidgetHost`,
replayable `WidgetInput`, immutable semantic actions, stack/grid layout, themes,
forms, stable-key lists, drop-downs, menus, typed dialogs, provider-backed folder
and file pickers, and domain-neutral game UI conveniences.

Applications own terminal sessions and loops, command execution, validation,
filesystem operations, and game state. The library's sole direct production
dependency is `Icod.DCurses 2.3.1`. Samples and tests are separate GPL projects
and are absent from the LGPL package.

The machine-readable declared-type/member snapshot is
[`Public-API-Fingerprint-1.0.json`](Public-API-Fingerprint-1.0.json). Stable 1.0
must preserve source and binary compatibility with this baseline unless a
reviewed release-blocking correction updates the baseline before W1014.
