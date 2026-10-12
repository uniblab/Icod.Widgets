# W1201 Workspace Contract

The 1.2 implementation source is versioned `1.2.0-alpha.1` while the assembly identity remains `1.0.0.0`. The sole direct package dependency remains `Icod.DCurses 2.4.0`. `WidgetLimits.MaximumTabs` caps a tab snapshot at 1,024. A public derived container can arrange its child and emit a host action without changes to the 1.1 extension API.

Verification on this Linux workspace used the installed .NET 10 Roslyn compiler, existing dependency assemblies, and VSTest directly because the NuGet package cache was unavailable for `dotnet build/test`. The first contract compile failed with CS0117 for `MaximumTabs`; after implementation the filtered two tests passed. The whole net10 suite passed: 235 tests, zero failures. Full SDK restore/build and net8/net9 runs remain release gates once a package cache is available.
