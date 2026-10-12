# W1303 — host theme switch and opaque surfaces

`WidgetHost.SetTheme` replaces the immutable theme on the application loop;
the application calls `Render` and DCurses refresh afterward. The replacement
does not route input, move focus, drain actions, close a modal, or mutate a
document. The old constructor and read-only public `Theme` getter remain.

The overlay staging window fills only the overlay's bounds with its resolved
dialog or popup surface before widget/child rendering. Dialog frames also
paint the full interior with the resolved surface role. An opaque styled blank
belongs to the overlay; closing it still exposes the unchanged lower screen.
The host does not fill transparent containers or other borrowed regions.

Two focused tests failed first for the absent `SetTheme` API, then passed for
theme replacement/focus and styled dialog padding/background restoration.
Full Release suite: 285/285 per target on net8, net9 and net10 on Linux x64,
with .NET 10 roll-forward for net8/net9. No interactive terminal check is
claimed. Source remains `1.3.0-alpha.1`, DCurses 2.5.0 direct.
