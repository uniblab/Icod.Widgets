# Icod.Widgets 1.9 Public API Baseline

**Package:** `1.9.0`<br>
**Assembly version:** `1.0.0.0`<br>
**Targets:** net8.0, net9.0, net10.0<br>
**Sole direct dependency:** `Icod.DCurses 2.5.2`

The three target assemblies expose an identical public reflection surface.
Every type and member recorded in the
[1.8 fingerprint](Public-API-Fingerprint-1.8.json) remains present.

The sole semantic 1.9 addition is:

- `WidgetEditorOptions.ShowVerticalScrollBar`.

Reflection records its getter, init setter, and property signatures. Its default is
`false`; the wrapped-row viewport implementation remains internal.
`AssemblyVersion` remains `1.0.0.0` for binary compatibility. The exact current
surface is [this fingerprint](Public-API-Fingerprint-1.9.json).

After building Release, generate the target-neutral snapshot with:

```powershell
pwsh -NoProfile -File packaging/Get-PublicApiFingerprint.ps1 -Configuration Release
```

The script reflects net8.0, net9.0, and net10.0 independently, rejects any
target difference, and writes the versioned JSON. CI uses `-Verify` for a
non-mutating comparison.
