# W1014 — Stable Qualification

**Candidate:** stable-source `1.0.0` (prepared from `1.0.0-rc.2`)
**Stable gate:** open; the Windows Terminal report has a source and environment record, with itemized live sample outcomes still pending. No tag or publication is authorized by this record.

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
| Windows x64 (AMD64) | Reported pass; live sample scope pending | Windows 10 build 10.0.26200.9457, Windows Terminal 1.24.12741.0, source `f1e0a9462a71f853acf5cf1797c7b89380675681`; itemized live steps pending |
| macOS ARM64/x64 as applicable | Pending | Terminal/version, architecture, exact SHA |
| Linux x64/ARM64 as applicable | Pending | Terminal/version, architecture, exact SHA; WSL identified separately |

## Live reports

- **2026-10-10 (operator report):** Everything was good on Windows 10 in Windows Terminal 1.24.12741.0. The Release solution build succeeded for all three target frameworks and all sample projects; `dotnet test tests\Icod.Widgets.Tests\Icod.Widgets.Tests.csproj -c Release --no-build` passed 387/387 tests (129 each on .NET 8.0.31, 9.0.20, and 10.0.12; none failed or skipped). The local source was `f1e0a9462a71f853acf5cf1797c7b89380675681` on AMD64, Windows build 10.0.26200.9457. Itemized live sample steps/outcomes remain to be confirmed. The documentation-only commit recording this result did not change the tested controls.

## Acceptance checklist

- Gallery: visit all six pages with Ctrl+N/P. Tab/Shift+Tab traverse eligible
  controls. Hidden/disabled items cannot activate. Read footer actions.
- Forms: type/paste combining and wide characters; edit in the middle with arrows,
  Home/End, Backspace/Delete; toggle checkbox and change radio choice.
- Lists: single commit, multi toggles/ranges/Ctrl+A/search; navigation preserves
  selection. Drop-down preview cancels on Escape/outside press and commits once.
- Menus: Alt+F/Alt+V opens the top-level menu, then an unmodified letter
  activates its matching item; Ctrl shortcuts invoke commands independently.
  Disabled/separator items remain inert; checkable command toggles once; enter
  a submenu, return with Left/Escape, invoke and close the chain. Inspect the
  popup border and caption on supported and compact terminal sizes.
- Dialogs: distinguish No from Cancel; safe defaults; Enter on an OK-only
  dialog reports Ok, and Escape on OK and Yes/No reports Cancel. Inspect borders
  and title captions. Tab stays in modal; Escape restores focus. Wizard first
  rejects Next, then advances; Previous returns.
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

The source and package versions are prepared as exact `1.0.0`, with assembly
`1.0.0.0` and curated release notes. After applicable live gates pass on the
final merged commit, verify main-branch CI, Release pack and package-only
consumption at that SHA. Then the owner can create `v1.0.0` on that commit.
The tag triggers the release workflow, including NuGet.org publication, GitHub
Packages publication and a GitHub Release. Confirm the Release environment and
NuGet OIDC user are configured before tagging; do not tag while this gate is open.
