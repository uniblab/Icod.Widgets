# Licensing assignments

Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>.

The approved licensing split is LGPL version 3 for the reusable DLL and GPL
version 3 for samples and tests. This project uses the existing Icod
"version 3 or, at your option, any later version" convention.

| Material | SPDX identifier | Terms |
|---|---|---|
| `src/`, root library project, library-specific build metadata, project documentation | `LGPL-3.0-or-later` | [LICENSE](LICENSE) |
| `samples/`, including sample code, assets authored here, and sample projects | `GPL-3.0-or-later` | [LICENSE.GPL](LICENSE.GPL) |
| `tests/`, including test code, fixtures authored here, and test projects | `GPL-3.0-or-later` | [LICENSE.GPL](LICENSE.GPL) |

The root LGPL text includes the incorporated GPL version 3 terms. The separate
GPL text supports explicit samples/tests licensing. Both texts are copied from
the established `uniblab/.github` license files, without rewriting their terms.
Upstream template/build-tool notices must be retained when those files are
imported. Third-party assets keep their own documented licenses and attribution.

## Implementation requirements

- Every authored C# source file carries the applicable license notice, copyright,
  and `SPDX-License-Identifier`. Project/props files carry an XML comment notice.
- `Icod.Widgets.csproj` sets `PackageLicenseExpression` to `LGPL-3.0-or-later`.
- Samples and tests declare `GPL-3.0-or-later` in their project metadata and are
  `IsPackable=false`.
- The library explicitly includes only `src/**/*.cs`; SDK default globbing must
  not compile GPL samples/tests into the DLL.
- NuGet carries the library, XML documentation, package README, and applicable
  notices. Samples/tests and their assets must not enter the library payload.
- Package-content inspection and source-header checks are release gates.

These are planned requirements. This PR contains no project files or assemblies.
