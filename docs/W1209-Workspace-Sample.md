# W1209 Workspace Sample

The GPL workspace sample composes three caller-owned in-memory documents, independent views/editors, a stable-key tab strip, replaceable content host, context menus, actionable status, close-decision dialog, Find and Go To Line requests, and a save-target observation provider. Its application controller applies or denies semantic actions on the shared DCurses loop. Cancel retains a modified tab, Don't Save removes it, and Save remains open because the sample intentionally has no writer. Search results are applied only when document/view/revision still match. The sample provider has no write method.

Run interactively with `dotnet run --project samples/Icod.Widgets.Workspace.Sample -c Release -f net10.0`. The `-- --smoke` option exercises the controller headlessly and prints a PASS line.

Verification: before implementation the workspace sample command failed because its project did not exist. The Release solution build subsequently passed for net8.0, net9.0, and net10.0 with zero warnings/errors. Standard net10 xUnit passed 278 tests, zero failures. Gallery, Editor, Roguelike, and Workspace net10 smoke paths all printed PASS. This environment has the net10 runtime only, so net8/net9 binaries built but could not be executed on their exact runtimes here.
