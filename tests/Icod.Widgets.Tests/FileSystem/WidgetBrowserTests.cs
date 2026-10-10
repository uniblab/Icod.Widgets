/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Tests.FileSystem;

using Xunit;

public sealed class WidgetBrowserTests {
	[Fact]
	public void OutOfOrderResultsCannotReplaceLatest() {
		WidgetBrowser browser = new(); WidgetBrowserRequest first = browser.RequestDirectory( "/first" ); WidgetBrowserRequest second = browser.RequestDirectory( "/second" );
		Assert.True( browser.ApplyLoadResult( WidgetBrowserLoadResult.Success( second, new WidgetDirectorySnapshot( "/second", "/", [] ) ) ) );
		Assert.False( browser.ApplyLoadResult( WidgetBrowserLoadResult.Success( first, new WidgetDirectorySnapshot( "/first", "/", [] ) ) ) );
		Assert.Equal( "/second", browser.Snapshot?.Location );
	}

	[Fact]
	public void ForeignAndDisposedBrowserResultsAreRejected() {
		WidgetBrowser browser = new(); WidgetBrowser other = new(); WidgetBrowserRequest foreign = other.RequestDirectory( "/foreign" );
		Assert.False( browser.ApplyLoadResult( WidgetBrowserLoadResult.Success( foreign, new WidgetDirectorySnapshot( "/foreign", "/", [] ) ) ) );
		WidgetBrowserRequest own = browser.RequestDirectory( "/own" ); browser.Dispose();
		Assert.False( browser.ApplyLoadResult( WidgetBrowserLoadResult.Success( own, new WidgetDirectorySnapshot( "/own", "/", [] ) ) ) );
	}

	[Fact]
	public void PermissionFailureRetainsListing() {
		WidgetBrowser browser = new(); WidgetBrowserRequest good = browser.RequestDirectory( "/good" ); browser.ApplyLoadResult( WidgetBrowserLoadResult.Success( good, new WidgetDirectorySnapshot( "/good", "/", [ new( "/good/a", "a", false ) ] ) ) );
		WidgetBrowserRequest denied = browser.RequestDirectory( "/denied" ); browser.ApplyLoadResult( WidgetBrowserLoadResult.Failure( denied, new WidgetBrowserError( WidgetBrowserErrorKind.AccessDenied, "Denied" ) ) );
		Assert.Equal( "/good", browser.Snapshot?.Location ); Assert.Equal( WidgetBrowserErrorKind.AccessDenied, browser.Error?.Kind );
	}

	[Fact]
	public async Task TooManyEntriesHasExplicitError() {
		WidgetBrowser browser = new(); WidgetBrowserRequest request = browser.RequestDirectory( "/huge" ); FakeProvider provider = new( WidgetLimits.MaximumItems + 1 );
		WidgetBrowserLoadResult result = await WidgetBrowserLoader.LoadAsync( provider, request, CancellationToken.None );
		Assert.Equal( WidgetBrowserErrorKind.TooManyEntries, result.Error?.Kind );
	}

	private sealed class FakeProvider( int count ) : IWidgetFileSystemProvider {
		public StringComparer PathComparer => StringComparer.Ordinal;
		public ValueTask<IReadOnlyList<WidgetFileSystemEntry>> GetRootsAsync( CancellationToken token ) => ValueTask.FromResult<IReadOnlyList<WidgetFileSystemEntry>>( [] );
		public ValueTask<WidgetDirectorySnapshot> ReadDirectoryAsync( string location, CancellationToken token ) => ValueTask.FromResult( new WidgetDirectorySnapshot( location, "/", Enumerable.Range( 0, count ).Select( i => new WidgetFileSystemEntry( $"/{i}", i.ToString(), false ) ).ToArray() ) );
		public ValueTask<WidgetFileSelectionValidation> ValidateSelectionAsync( IReadOnlyList<string> paths, WidgetFileSelectionKind kind, CancellationToken token ) => ValueTask.FromResult( WidgetFileSelectionValidation.Valid );
	}
}
