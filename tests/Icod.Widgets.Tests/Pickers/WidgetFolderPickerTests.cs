/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Tests.Pickers;

using Icod.DCurses;
using Xunit;

public sealed class WidgetFolderPickerTests {
	[Fact]
	public void SelectCurrentFolderAtRoot() {
		FakeProvider provider = new(); WidgetFolderPicker picker = new( "folder", provider, "/" ); using WidgetHost host = ModalHost( picker );
		WidgetBrowserRequest load = picker.RequestDirectory( "/" ); picker.ApplyLoadResult( WidgetBrowserLoadResult.Success( load, new WidgetDirectorySnapshot( "/", null, [] ) ) );
		WidgetPickerValidationRequest request = picker.RequestAccept(); WidgetDispatchResult before = host.Process( WidgetInput.Key( WidgetKey.Space ), TimeSpan.Zero ); Assert.Empty( before.Actions );
		Assert.True( picker.ApplyValidationResult( WidgetPickerValidationResult.Success( request ) ) );
		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.DrainActions() ) );
		Assert.Equal( WidgetDialogOutcome.Accepted, completed.Result.Outcome ); Assert.Equal( "/", Assert.Single( Assert.IsAssignableFrom<IReadOnlyList<string>>( completed.Result.Value ) ) );
	}

	[Fact]
	public void InvalidStartShowsRecoverableError() {
		FakeProvider provider = new(); WidgetFolderPicker picker = new( "folder", provider, "/missing" ); WidgetBrowserRequest request = picker.RequestDirectory( "/missing" );
		Assert.True( picker.ApplyLoadResult( WidgetBrowserLoadResult.Failure( request, new WidgetBrowserError( WidgetBrowserErrorKind.NotFound, "Missing" ) ) ) );
		Assert.Equal( WidgetBrowserErrorKind.NotFound, picker.Browser.Error?.Kind );
	}

	[Fact]
	public void DeletedFolderCannotAccept() {
		FakeProvider provider = new(); WidgetFolderPicker picker = Loaded( provider ); using WidgetHost host = ModalHost( picker ); WidgetPickerValidationRequest request = picker.RequestAccept();
		picker.ApplyValidationResult( WidgetPickerValidationResult.Failure( request, [ "/folder" ] ) );
		Assert.True( picker.IsOpen ); Assert.NotNull( picker.ValidationError ); Assert.Empty( host.DrainActions() );
	}

	[Fact]
	public void CancelDuringValidationCannotAccept() {
		FakeProvider provider = new(); WidgetFolderPicker picker = Loaded( provider ); using WidgetHost host = ModalHost( picker ); WidgetPickerValidationRequest request = picker.RequestAccept();
		host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero );
		Assert.False( picker.ApplyValidationResult( WidgetPickerValidationResult.Success( request ) ) );
	}

	private static WidgetFolderPicker Loaded( FakeProvider provider ) { WidgetFolderPicker picker = new( "folder", provider, "/folder" ); WidgetBrowserRequest load = picker.RequestDirectory( "/folder" ); picker.ApplyLoadResult( WidgetBrowserLoadResult.Success( load, new WidgetDirectorySnapshot( "/folder", "/", [ new( "/folder/file", "file", false ) ] ) ) ); return picker; }
	private static WidgetHost ModalHost( WidgetFolderPicker picker ) { WidgetHost host = new( new CursesScreen( 40, 10 ), WidgetTheme.Default ); host.ShowModal( picker ); return host; }
	private sealed class FakeProvider : IWidgetFileSystemProvider {
		public StringComparer PathComparer => StringComparer.Ordinal;
		public ValueTask<IReadOnlyList<WidgetFileSystemEntry>> GetRootsAsync( CancellationToken token ) => ValueTask.FromResult<IReadOnlyList<WidgetFileSystemEntry>>( [] );
		public ValueTask<WidgetDirectorySnapshot> ReadDirectoryAsync( string location, CancellationToken token ) => throw new NotSupportedException();
		public ValueTask<WidgetFileSelectionValidation> ValidateSelectionAsync( IReadOnlyList<string> paths, WidgetFileSelectionKind kind, CancellationToken token ) => ValueTask.FromResult( WidgetFileSelectionValidation.Valid );
	}
}
