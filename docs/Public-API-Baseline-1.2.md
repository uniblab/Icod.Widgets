# Icod.Widgets 1.2 Public API Baseline

**Package:** `1.2.0`<br>
**Assembly version:** `1.0.0.0`  
**Targets:** `net8.0`, `net9.0`, `net10.0`  
**Sole direct dependency:** `Icod.DCurses 2.4.0`

The reflection snapshot exports 130 public types. An exact declared-member comparison against [`Public-API-Fingerprint-1.1.json`](Public-API-Fingerprint-1.1.json) found all 103 prior public types and every recorded declared member present. This includes the original binary signatures of `WidgetMenuItem.Command` and `Checkable`; new focused-edit overloads are additive. The previously compiled 1.0.0 and 1.1.0 consumer witnesses both ran with the 1.2 assembly substituted.

The 27 added types include tabs/content host, menu presentation, form validation, save-target inspection, search/replace requests, and Go To Line. The complete public declared-member surface is [`Public-API-Fingerprint-1.2.json`](Public-API-Fingerprint-1.2.json). Existing enum values remain in their original order; `Save` and `DontSave` are appended to `WidgetDialogOutcome`. Existing status-field positional construction and deconstruction remain three-argument.

`IWidgetFileSystemProvider` is unchanged. `IWidgetSaveTargetProvider` is a separate optional interface with an inspection method only. No application document lifetime, file write, search execution, or overwrite guarantee moves into the library.
