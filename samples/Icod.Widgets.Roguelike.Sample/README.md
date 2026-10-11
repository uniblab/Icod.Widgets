# Icod.Widgets Roguelike UI Sample

GPL-3.0-or-later. Demonstrates caller inventory/actions, messages, status, and
modal help with retained text. It is a UI witness, not a complete game.

```text
dotnet run --project samples/Icod.Widgets.Roguelike.Sample -c Release -f net10.0
```

i inventory; a actions; h help. Tab/Shift+Tab traverse. Arrows navigate, Space
toggles inventory, Enter sends a command once and appends a message. Escape
closes help and restores focus. Q/Escape exits outside help; Ctrl+Q always
exits. Resize to exercise clipping.

```text
dotnet run --project samples/Icod.Widgets.Roguelike.Sample -c Release -f net10.0 -- --smoke
```

Expected: `PASS inventory/actions, messages/status, modal help restores focus, command delivered once`.
