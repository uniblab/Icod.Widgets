# W1012 — Public and Package-only Consumer Qualification

**Status:** complete

Three GPL samples compile using public APIs for .NET 8, 9, and 10. Their .NET 10
smoke paths passed:

- gallery: forms, focus, semantic actions, and compact retained rendering;
- editor: picker returns a validated path and the application opens the file;
- roguelike: an inventory action reaches the caller once and modal help restores focus.

Release pack produced `Icod.Widgets.1.0.0-alpha.1.nupkg`. A separate console
consumer with only a `PackageReference` restored from `artifacts/packages`, ran,
and returned exit code 0. The package SHA-256 for this evidence run was
`357f1b4a21c3cdd4f6e1ed618bdf5f618df5d9397e49b8e5f0d059c2a44347ac`.
Its payload contains LGPL terms, README, and DLL/XML assets for net8/net9/net10;
it contains no sample or test assets.

The full net10 test suite passed 65 tests before packing. Cross-framework final
qualification is repeated at W1013 after the API and documentation freeze.
