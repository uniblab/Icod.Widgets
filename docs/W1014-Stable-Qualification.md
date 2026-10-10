# W1014 — Stable Qualification

**Status:** live qualification pending  
**Candidate:** `1.0.0-rc.1`  
**Exact commit:** record the tested PR head before starting

Automated pull-request jobs build and test net8/net9/net10 and run all three
net10 sample smoke workflows on Windows, macOS, and Linux. Those checks do not
replace a human-operated terminal pass.

## Live commands

```text
dotnet run --project samples/Icod.Widgets.Gallery.Sample -c Release -f net10.0
dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0
dotnet run --project samples/Icod.Widgets.Roguelike.Sample -c Release -f net10.0
```

For each platform, record the exact commit, OS/architecture, terminal and
version, .NET SDK version, and result. Keep failures and reruns in the notes.

| Platform | Architecture | Terminal/version | Commit | Result | Notes |
|---|---|---|---|---|---|
| Windows | x64 or ARM64 | pending | pending | pending | |
| macOS | x64 or ARM64 | pending | pending | pending | |
| Linux | x64 or ARM64 | pending | pending | pending | |

## Operator checklist

- Exercise every gallery control by keyboard, including Shift+Tab and Ctrl+A.
- Open/close nested modal paths and confirm background input is isolated and
  focus returns to the prior control.
- Resize repeatedly, including compact and zero-height/zero-width transitions.
- Enter and edit Unicode text, including a width-two character and a combining
  sequence; verify caret movement, insertion, Backspace, Delete, Home, and End.
- Use supported pointer input, including press/open-modal/release sequences.
- In the editor, navigate directories, trigger an inaccessible or disappearing
  location, change filters, select a file, validate it, and confirm the
  application reads only the returned path.
- In the roguelike sample, invoke inventory/actions, help, text prompt, and
  culture-aware number prompt.
- Confirm Escape cancels the active modal and Ctrl+Q/unhandled top-level Escape
  exits cleanly.

Stable `1.0.0` metadata, tag, release notes, and any publication remain blocked
until this table and checklist contain reviewed exact-head evidence.
