# Document Workspace Sample

Run in an interactive terminal from the repository root:

```text
dotnet run --project samples/Icod.Widgets.Workspace.Sample -c Release -f net10.0
```

Run a headless integration check:

```text
dotnet run --project samples/Icod.Widgets.Workspace.Sample -c Release -f net10.0 -- --smoke
```

The sample owns three in-memory documents and their independent views/editors. Its tab strip emits activation and close requests; the application switches content, updates modified markers, and asks about closing a modified tab. Escape/Cancel retains the tab and its view, Don't Save removes it, and Save deliberately leaves it open because this sample has no file-writing path. The save-target provider only inspects an in-memory proposed path. It never writes a file.

Alt+V followed by plain F invokes Go to File; Ctrl+F opens Find directly. Ctrl+Tab cycles documents. The status fields open Find and Save As. The application checks document/view/revision before applying a search result or Go To Line request. Query execution and selection changes are sample policy, not library behavior. Ctrl+Q exits from any control.

The shared application loop in `samples/Common/WidgetSampleApplication.cs` owns the DCurses session, input, monotonic clock, queued provider work, render/refresh, and cleanup. Its provider results return to that loop before any widget state is updated.
