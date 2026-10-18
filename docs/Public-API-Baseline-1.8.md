# Icod.Widgets 1.8 Public API Baseline

**Package:** `1.8.0`<br>
**Assembly version:** `1.0.0.0`<br>
**Targets:** net8.0, net9.0, net10.0<br>
**Sole direct dependency:** `Icod.DCurses 2.5.2`

The three target assemblies expose an identical reflection surface of 134
public types. Exact type/member comparison with the
[1.3 baseline](Public-API-Fingerprint-1.3.json) found all 134 types and every
recorded member present. Releases 1.4–1.8 add members to existing types without
removing a recorded signature; the exact current surface is
[this fingerprint](Public-API-Fingerprint-1.8.json).

The 1.8 dialog additions are:

- `WidgetDialogAction.Mnemonic`;
- the four-argument `WidgetMessageDialog` action-array constructor;
- read-only `WidgetMessageDialog.Actions`; and
- `WidgetMessageDialog.SetActionEnabled(string, bool)`.

`WidgetMessageDialog.YesNo(string, string, string)` remains in the fingerprint
with its existing signature and behavior. Its `ObsoleteAttribute` is metadata
rather than a reflection member signature: the attribute is warning-level
(`IsError == false`) and directs source consumers to the action-array
constructor. `AssemblyVersion` remains `1.0.0.0` for binary compatibility.

After building Release, generate the target-neutral snapshot with:

```powershell
pwsh -NoProfile -File packaging/Get-PublicApiFingerprint.ps1 -Configuration Release
```

The script reflects net8.0, net9.0, and net10.0 independently, rejects any
target difference, and writes the versioned JSON. CI and release validation use
the non-mutating check:

```powershell
pwsh -NoProfile -File packaging/Get-PublicApiFingerprint.ps1 -Configuration Release -Verify
```
