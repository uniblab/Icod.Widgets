# Icod.Widgets Editor Open-File Sample

GPL-3.0-or-later. Displays document lines; it never writes files. The application
reads files returned by the picker (1 MiB sample limit).

```text
dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0
```

Ctrl+O or Enter on Open opens picker. Arrows navigate; Enter opens folder/chooses
and accepts a single file; Space selects without accepting; Ctrl+S (or Ctrl+Enter when reported) validates/accepts; Escape cancels. Ctrl+L types/pastes a path;
Backspace moves to parent; Ctrl+R roots; Ctrl+F filters. Cancel leaves document
unchanged. After opening, arrows/Home/End/Page keys browse lines. Tab/Shift+Tab
switch controls; Q/Escape exits outside picker; Ctrl+Q always exits. Resize and
retry inaccessible/deleted paths during acceptance.

```text
dotnet run --project samples/Icod.Widgets.Editor.Sample -c Release -f net10.0 -- --smoke
```

Expected: `PASS keyboard file selection, application-owned file reading, cancellation preserves document`.
