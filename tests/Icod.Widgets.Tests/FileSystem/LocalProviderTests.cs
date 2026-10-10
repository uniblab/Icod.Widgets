/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
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
