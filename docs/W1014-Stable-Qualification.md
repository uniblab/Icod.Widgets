# W1014 — Stable Qualification

**Candidate:** `1.0.0-rc.2`
**Stable gate:** open; no stable version/tag/publication authorized by this record.

## Record the source before testing

From the repository root:

```text
git checkout docs/1.0.0-roadmap
git pull --ff-only
git rev-parse HEAD
dotnet --info
dotnet build Icod.Widgets.sln -c Release
dotnet test tests/Icod.Widgets.Tests/Icod.Widgets.Tests.csproj -c Release --no-build
```

Record the SHA, operating system/build, architecture, terminal/version, runtime,
steps and outcomes. Run the live commands in `samples/README.md` without `--smoke`.
A deterministic smoke or test PTY does not qualify a real terminal implementation.

| Environment | Live result | Required record |
|---|---|---|
| Windows x64 | Pending | Windows Terminal plus any additional terminals actually used |
| macOS ARM64/x64 as applicable | Pending | Terminal/version, architecture, exact SHA |
| Linux x64/ARM64 as applicable | Pending | Terminal/version, architecture, exact SHA; WSL identified separately |

## Acceptance checklist

- Gallery: visit all six pages with Ctrl+N/P. Tab/Shift+Tab traverse eligible
  controls. Hidden/disabled items cannot activate. Read footer actions.
- Forms: type/paste combining and wide characters; edit in the middle with arrows,
  Home/End, Backspace/Delete; toggle checkbox and change radio choice.
- Lists: single commit, multi toggles/ranges/Ctrl+A/search; navigation preserves
  selection. Drop-down preview cancels on Escape/outside press and commits once.
- Menus: disabled/separator inert; checkable command toggles once; enter a submenu,
  return with Left/Escape, invoke and close the chain.
- Dialogs: distinguish No from Cancel; safe defaults; Tab stays in modal; Escape
  restores focus. Wizard first rejects Next, then advances; Previous returns.
  Text/number prompts render and validate; invalid number stays open with an error.
- Pickers: start/current folder, parent, roots/drives, path input, single/multiple
  files, filters, basket across directories and explicit removal. **Ctrl+S accepts**;
  Ctrl+Enter is also available when the terminal reports the modifier. Failed loads
  keep the previous listing usable; deleted selection fails validation.
- While I/O is pending, cancel; later completion cannot reopen/accept a closed picker.
- Editor: Ctrl+O opens, selected file content appears, cancellation preserves content.
  The application performs reading and never writes files.
- Roguelike UI: inventory/actions/messages/status/help; commands arrive once;
  help closes and restores focus.
- Repeated shrink/grow, compact/zero geometry, Unicode, keyboard-only input,
  supported mouse press/release, clean Q/Escape exits, Ctrl+Q from a text field.

Report each result as Pass / Fail / Unsupported / Not tested. Identify exact steps
for failures and which terminal was used. Do not substitute a green CI run for live
results. Requalify affected controls at the corrected SHA after a reproducible fix.

## Stable-source completion

Only after applicable live gates pass: set exact `1.0.0`, retain assembly
`1.0.0.0`, rerun CI/pack/package-only consumption, refresh curated release notes,
README and CHANGELOG, then report readiness. Tagging/NuGet/GitHub Release require
a separate release instruction.
