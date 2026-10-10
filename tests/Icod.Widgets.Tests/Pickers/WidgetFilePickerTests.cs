/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Tests.Pickers;

using Icod.DCurses;
using Xunit;

public sealed class WidgetFilePickerTests {
	[Fact]
	public void SingleReturnsOneExistingFile() {
		FakeProvider provider = new( StringComparer.Ordinal ); WidgetFilePicker picker = Loaded( provider, multiple: false ); using WidgetHost host = ModalHost( picker ); picker.ToggleFile( "/dir/a.txt" );
		WidgetPickerValidationRequest request = picker.RequestAccept(); picker.ApplyValidationResult( WidgetPickerValidationResult.Success( request ) );
		WidgetDialogCompletedAction action = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.DrainActions() ) ); Assert.Equal( "/dir/a.txt", Assert.Single( Assert.IsAssignableFrom<IReadOnlyList<string>>( action.Result.Value ) ) );
	}

	[Fact]
	public void BasketSurvivesDirectoryAndFilterChanges() {
		FakeProvider provider = new( StringComparer.Ordinal ); WidgetFilePicker picker = Loaded( provider, multiple: true ); picker.ToggleFile( "/dir/a.txt" ); picker.SelectFilter( "logs" );
		WidgetBrowserRequest request = picker.RequestDirectory( "/other" ); picker.ApplyLoadResult( WidgetBrowserLoadResult.Success( request, new WidgetDirectorySnapshot( "/other", "/", [] ) ) );
		Assert.Equal( [ "/dir/a.txt" ], picker.Basket.Paths );
	}

	[Fact]
	public void ComparerDeduplicatesPaths() {
		WidgetFileSelectionBasket basket = new( StringComparer.OrdinalIgnoreCase ); basket.Toggle( "/A.txt" ); basket.Toggle( "/a.txt" );
		Assert.Empty( basket.Paths );
	}

	[Fact]
	public void FilterNeverHidesDirectories() {
		FakeProvider provider = new( StringComparer.Ordinal ); WidgetFilePicker picker = Loaded( provider, multiple: true ); picker.SelectFilter( "logs" );
		Assert.Contains( picker.VisibleEntries, entry => entry.IsDirectory ); Assert.DoesNotContain( picker.VisibleEntries, entry => entry.Path.EndsWith( ".txt", StringComparison.Ordinal ) );
	}

	[Fact]
	public void ReturnedPathsDoNotAliasBasket() {
		FakeProvider provider = new( StringComparer.Ordinal ); WidgetFilePicker picker = Loaded( provider, multiple: true ); using WidgetHost host = ModalHost( picker ); picker.ToggleFile( "/dir/a.txt" );
		WidgetPickerValidationRequest request = picker.RequestAccept(); picker.ApplyValidationResult( WidgetPickerValidationResult.Success( request ) ); IReadOnlyList<string> returned = Assert.IsAssignableFrom<IReadOnlyList<string>>( Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.DrainActions() ) ).Result.Value );
		picker.Basket.Clear(); Assert.Equal( [ "/dir/a.txt" ], returned );
	}

	[Fact]
	public void EmptyBasketCannotAccept() { FakeProvider provider = new( StringComparer.Ordinal ); WidgetFilePicker picker = Loaded( provider, true ); Assert.Throws<InvalidOperationException>( picker.RequestAccept ); }

	[Fact]
	public void StaleValidationCannotAcceptAfterNavigationOrCancel() {
		FakeProvider provider = new( StringComparer.Ordinal ); WidgetFilePicker picker = Loaded( provider, true ); using WidgetHost host = ModalHost( picker ); picker.ToggleFile( "/dir/a.txt" ); WidgetPickerValidationRequest request = picker.RequestAccept(); picker.RequestDirectory( "/other" );
		Assert.False( picker.ApplyValidationResult( WidgetPickerValidationResult.Success( request ) ) );
	}

	[Fact]
	public void BasketChangeInvalidatesPendingValidation() {
		FakeProvider provider = new( StringComparer.Ordinal ); WidgetFilePicker picker = Loaded( provider, true ); using WidgetHost host = ModalHost( picker ); picker.ToggleFile( "/dir/a.txt" ); WidgetPickerValidationRequest request = picker.RequestAccept(); picker.ToggleFile( "/dir/a.log" );
		Assert.False( picker.ApplyValidationResult( WidgetPickerValidationResult.Success( request ) ) );
	}

	[Fact]
	public void DirectBasketRemovalInvalidatesPendingValidation() {
		FakeProvider provider = new( StringComparer.Ordinal ); WidgetFilePicker picker = Loaded( provider, true ); using WidgetHost host = ModalHost( picker ); picker.ToggleFile( "/dir/a.txt" ); WidgetPickerValidationRequest request = picker.RequestAccept(); picker.Basket.Clear();
		Assert.False( picker.ApplyValidationResult( WidgetPickerValidationResult.Success( request ) ) );
	}

	[Fact]
	public void ClosedAndReopenedPickerRejectsOldValidation() {
		FakeProvider provider = new( StringComparer.Ordinal ); WidgetFilePicker picker = Loaded( provider, true ); using WidgetHost host = new( new CursesScreen( 40, 10 ), WidgetTheme.Default ); WidgetModalLease lease = host.ShowModal( picker ); picker.ToggleFile( "/dir/a.txt" ); WidgetPickerValidationRequest request = picker.RequestAccept(); lease.Dispose(); _ = host.DrainActions(); host.ShowModal( picker );
		Assert.False( picker.ApplyValidationResult( WidgetPickerValidationResult.Success( request ) ) ); Assert.True( picker.IsOpen );
	}

	[Fact]
	public void KeyboardSelectionRequestsValidation() {
		FakeProvider provider = new( StringComparer.Ordinal ); WidgetFilePicker picker = Loaded( provider, false ); using WidgetHost host = ModalHost( picker );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.Zero );
		WidgetPickerValidationRequestedAction action = Assert.IsType<WidgetPickerValidationRequestedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) );
		Assert.Equal( [ "/dir/a.txt" ], action.Request.Paths );
	}

	private static WidgetFilePicker Loaded( FakeProvider provider, bool multiple ) { WidgetFilePicker picker = new( "files", provider, "/dir", multiple, [ new WidgetFileFilter( "text", [ ".txt" ] ), new WidgetFileFilter( "logs", [ ".log" ] ), WidgetFileFilter.AllFiles ] ); WidgetBrowserRequest request = picker.RequestDirectory( "/dir" ); picker.ApplyLoadResult( WidgetBrowserLoadResult.Success( request, new WidgetDirectorySnapshot( "/dir", "/", [ new( "/dir/sub", "sub", true ), new( "/dir/a.txt", "a.txt", false ), new( "/dir/a.log", "a.log", false ) ] ) ) ); return picker; }
	private static WidgetHost ModalHost( WidgetFilePicker picker ) { WidgetHost host = new( new CursesScreen( 40, 10 ), WidgetTheme.Default ); host.ShowModal( picker ); return host; }
	private sealed class FakeProvider( StringComparer comparer ) : IWidgetFileSystemProvider {
		public StringComparer PathComparer => comparer;
		public ValueTask<IReadOnlyList<WidgetFileSystemEntry>> GetRootsAsync( CancellationToken token ) => ValueTask.FromResult<IReadOnlyList<WidgetFileSystemEntry>>( [] );
		public ValueTask<WidgetDirectorySnapshot> ReadDirectoryAsync( string location, CancellationToken token ) => throw new NotSupportedException();
		public ValueTask<WidgetFileSelectionValidation> ValidateSelectionAsync( IReadOnlyList<string> paths, WidgetFileSelectionKind kind, CancellationToken token ) => ValueTask.FromResult( WidgetFileSelectionValidation.Valid );
	}
}
