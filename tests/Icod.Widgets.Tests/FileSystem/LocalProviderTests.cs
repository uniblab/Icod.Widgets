/*
	Icod.Widgets.Tests
	Automated tests for the Icod.Widgets library.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.Widgets.Tests.FileSystem;

using Xunit;

public sealed class LocalProviderTests {
	[Fact]
	public async Task ReadsUnicodeAndSpaceNamesWithoutRecursion() {
		string root = Path.Combine( Path.GetTempPath(), $"icod widgets {Guid.NewGuid():N}" ); Directory.CreateDirectory( root );
		try {
			Directory.CreateDirectory( Path.Combine( root, "資料 folder" ) ); await File.WriteAllTextAsync( Path.Combine( root, "résumé.txt" ), "x" );
			LocalWidgetFileSystemProvider provider = new(); WidgetDirectorySnapshot snapshot = await provider.ReadDirectoryAsync( root, CancellationToken.None );
			Assert.Equal( 2, snapshot.Entries.Count ); Assert.True( snapshot.Entries[ 0 ].IsDirectory ); Assert.All( snapshot.Entries, entry => Assert.True( Path.IsPathFullyQualified( entry.Path ) ) );
		} finally { Directory.Delete( root, recursive: true ); }
	}

	[Fact]
	public async Task RootsHaveNoParent() {
		LocalWidgetFileSystemProvider provider = new(); IReadOnlyList<WidgetFileSystemEntry> roots = await provider.GetRootsAsync( CancellationToken.None );
		Assert.NotEmpty( roots ); foreach ( WidgetFileSystemEntry root in roots ) { WidgetDirectorySnapshot snapshot = await provider.ReadDirectoryAsync( root.Path, CancellationToken.None ); Assert.Null( snapshot.ParentLocation ); }
	}
}
