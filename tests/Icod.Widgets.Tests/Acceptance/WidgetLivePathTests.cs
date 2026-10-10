/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Tests.Acceptance;
using Icod.DCurses;
using Xunit;

public sealed class WidgetLivePathTests {
	[Fact]
	public void ModalTabKeepsFocusAndBlocksBackground() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetButton button = new( "run", "Run", "run" ); host.Attach( button ); host.Arrange( host.Screen.Bounds ); host.Focus( button );
		WidgetMessageDialog dialog = WidgetMessageDialog.YesNo( "dialog", "Question", "Continue?" ); host.ShowModal( dialog );
		host.Process( WidgetInput.Key( WidgetKey.Tab ), TimeSpan.Zero );
		Assert.Same( dialog, host.FocusedWidget );
		Assert.True( host.Process( WidgetInput.TextInput( new( 'q' ) ), TimeSpan.Zero ).Handled );
	}
	[Fact]
	public void DetachingModalClosesDescendantsAndRestoresFocus() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		WidgetButton button = new( "run", "Run", "run" ); host.Attach( button ); host.Arrange( host.Screen.Bounds ); host.Focus( button );
		WidgetMessageDialog parent = WidgetMessageDialog.YesNo( "parent", "Question", "Continue?" ); host.ShowModal( parent ); host.ShowModal( WidgetMessageDialog.OkCancel( "child", "Child", "Child" ) );
		host.Detach( parent ); Assert.Equal( 0, host.ModalDepth ); Assert.Same( button, host.FocusedWidget ); Assert.Equal( 2, host.DrainActions().Count );
	}
	[Fact]
	public void FilePickerKeyboardChangesSelectionAndRendersListing() {
		WidgetFilePicker picker = new( "files", new Provider(), "/dir", true, [ WidgetFileFilter.AllFiles ] );
		WidgetBrowserRequest request = picker.RequestDirectory( "/dir" ); picker.ApplyLoadResult( WidgetBrowserLoadResult.Success( request, Snapshot() ) );
		using WidgetHost host = new( new CursesScreen( 60, 12 ), WidgetTheme.Default ); host.ShowModal( picker );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.Zero ); host.Process( WidgetInput.Key( WidgetKey.Space ), TimeSpan.Zero );
		Assert.Equal( [ "/dir/a.txt" ], picker.Basket.Paths ); host.Render(); Assert.Contains( "a.txt", Cells( host.GetPresentationWindow( picker ) ) );
	}
	[Fact]
	public void FolderPickerRendersCurrentDirectoryAndEntries() {
		WidgetFolderPicker picker = new( "folder", new Provider(), "/dir" );
		WidgetBrowserRequest request = picker.RequestDirectory( "/dir" ); picker.ApplyLoadResult( WidgetBrowserLoadResult.Success( request, Snapshot() ) );
		using WidgetHost host = new( new CursesScreen( 60, 12 ), WidgetTheme.Default ); host.ShowModal( picker ); host.Render();
		Assert.Contains( "/dir", Cells( host.GetPresentationWindow( picker ) ) ); Assert.Contains( "sub", Cells( host.GetPresentationWindow( picker ) ) );
	}
	[Fact]
	public void WizardRendersPageAndRejectedNavigationError() {
		using WidgetHost host = new( new CursesScreen( 60, 12 ), WidgetTheme.Default ); WidgetWizardDialog wizard = new( "wizard", "Setup", [ "First page", "Second page" ] ); host.ShowModal( wizard ); wizard.RejectNavigation( "Fix the value" ); host.Render();
		Assert.Contains( "First page", Cells( host.GetPresentationWindow( wizard ) ) ); Assert.Contains( "Fix the value", Cells( host.GetPresentationWindow( wizard ) ) );
	}
	[Fact]
	public void ControlASelectsEnabledListItems() {
		using WidgetHost host = new( new CursesScreen( 40, 12 ), WidgetTheme.Default ); WidgetList list = new( "list", WidgetSelectionMode.Multiple ); list.SetItems( [ new( "a", "Alpha" ), new( "b", "Beta" ), new( "c", "Disabled", false ) ] ); host.Attach( list ); host.Arrange( host.Screen.Bounds ); host.Focus( list );
		host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 'a' ) ), TimeSpan.Zero ); Assert.Equal( [ "a", "b" ], list.SelectedKeys );
	}
	[Fact]
	public void TextCaretEditsWholeElementsInMiddle() {
		using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default ); WidgetTextInput text = new( "text", "á界z" ); host.Attach( text ); host.Arrange( host.Screen.Bounds ); host.Focus( text );
		host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.Zero ); host.Process( WidgetInput.Key( WidgetKey.Backspace ), TimeSpan.Zero ); Assert.Equal( "áz", text.Value );
		host.Process( WidgetInput.TextInput( new( 'b' ) ), TimeSpan.Zero ); Assert.Equal( "ábz", text.Value );
		host.Process( WidgetInput.Key( WidgetKey.Home ), TimeSpan.Zero ); host.Process( WidgetInput.Key( WidgetKey.Delete ), TimeSpan.Zero ); Assert.Equal( "bz", text.Value );
	}
	[Fact]
	public void LongDropDownKeepsPreviewVisible() {
		using WidgetHost host = new( new CursesScreen( 30, 5 ), WidgetTheme.Default ); WidgetDropDown drop = new( "drop", Enumerable.Range( 0, 20 ).Select( i => new WidgetListItem( i.ToString(), "Item " + i ) ).ToArray(), "0" ); host.Attach( drop ); drop.Arrange( new( 0, 0, 1, 30 ) ); host.Focus( drop ); drop.Open();
		for ( int i = 0; i < 10; i++ ) host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.Zero ); host.Render(); Assert.Contains( "Item 10", Cells( host.GetPresentationWindow( drop ) ) );
	}
	[Fact]
	public void DirectBasketMutationCannotAcceptStaleValidation() {
		WidgetFilePicker picker = new( "files", new Provider(), "/dir", true, [ WidgetFileFilter.AllFiles ] ); WidgetBrowserRequest browse = picker.RequestDirectory( "/dir" ); picker.ApplyLoadResult( WidgetBrowserLoadResult.Success( browse, Snapshot() ) );
		using WidgetHost host = new( new CursesScreen( 40, 10 ), WidgetTheme.Default ); host.ShowModal( picker ); picker.ToggleFile( "/dir/a.txt" ); WidgetPickerValidationRequest request = picker.RequestAccept(); picker.Basket.Clear();
		Assert.False( picker.ApplyValidationResult( WidgetPickerValidationResult.Success( request ) ) ); Assert.True( picker.IsOpen );
	}
	[Fact]
	public void MessageWrapShowsTextAfterFirstLine() {
		using WidgetHost host = new( new CursesScreen( 20, 8 ), WidgetTheme.Default ); WidgetMessageDialog dialog = WidgetMessageDialog.YesNo( "dialog", "Question", "First words then more words with a final marker" ); host.ShowModal( dialog ); host.Render(); Assert.Contains( "marker", Cells( host.GetPresentationWindow( dialog ) ) );
	}
	[Fact]
	public void ListPointerReleaseCommitsClickedRowOnce() {
		using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default ); WidgetList list = new( "list", WidgetSelectionMode.Single ); list.SetItems( [ new( "a", "Alpha" ), new( "b", "Beta" ) ] ); host.Attach( list ); host.Arrange( host.Screen.Bounds );
		host.Process( WidgetInput.PointerInput( new( CursesMouseAction.Press, CursesMouseButton.Primary, 2, 1 ) ), TimeSpan.Zero );
		WidgetDispatchResult result = host.Process( WidgetInput.PointerInput( new( CursesMouseAction.Release, CursesMouseButton.Primary, 2, 1 ) ), TimeSpan.Zero ); Assert.Equal( [ "b" ], list.SelectedKeys ); Assert.Single( result.Actions );
	}
	[Fact]
	public void DropDownOutsidePressCancelsPreview() {
		using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default ); WidgetDropDown drop = new( "drop", [ new( "a", "Alpha" ), new( "b", "Beta" ) ], "a" ); host.Attach( drop ); drop.Arrange( new( 1, 1, 1, 10 ) ); host.Focus( drop ); drop.Open();
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.Zero ); host.Process( WidgetInput.PointerInput( new( CursesMouseAction.Press, CursesMouseButton.Primary, 30, 6 ) ), TimeSpan.Zero ); Assert.False( drop.IsOpen ); Assert.Equal( "a", drop.SelectedKey );
	}
	[Fact]
	public void ClosedMenuShowsItsLabels() {
		using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default ); WidgetMenuBar menu = new( "menu", [ WidgetMenuItem.Command( "run", "Run", "run" ) ] ); host.Attach( menu ); host.Arrange( host.Screen.Bounds ); host.Render(); Assert.Contains( "Run", Cells( host.Screen ) );
	}
	[Fact]
	public void QueueOverflowLeavesCheckboxAndModalUnchanged() {
		using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default ); WidgetCheckBox check = new( "check", "Check" ); host.Attach( check ); host.Arrange( host.Screen.Bounds ); host.Focus( check );
		for ( int i = 0; i < WidgetLimits.MaximumPendingActions; i++ ) host.Enqueue( new WidgetInvokedAction( "probe", "probe" ) );
		Assert.Throws<InvalidOperationException>( () => host.Process( WidgetInput.Key( WidgetKey.Space ), TimeSpan.Zero ) ); Assert.False( check.IsChecked ); host.DrainActions();
		WidgetMessageDialog dialog = WidgetMessageDialog.YesNo( "question", "Question", "Continue?" ); host.ShowModal( dialog );
		for ( int i = 0; i < WidgetLimits.MaximumPendingActions; i++ ) host.Enqueue( new WidgetInvokedAction( "probe", "probe" ) );
		Assert.Throws<InvalidOperationException>( () => host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ) ); Assert.True( dialog.IsOpen ); Assert.Equal( 1, host.ModalDepth );
	}
	[Fact]
	public void HiddenContainerExcludesChildrenFromFocusAndRendering() {
		using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default ); WidgetStack stack = new( "stack", WidgetOrientation.Vertical ); WidgetButton child = new( "child", "Hidden child", "run" ); stack.Add( child ); host.Attach( stack ); host.Arrange( host.Screen.Bounds ); stack.IsVisible = false; Assert.False( host.Focus( child ) ); host.Render(); Assert.DoesNotContain( "Hidden child", Cells( host.Screen ) );
	}
	[Fact]
	public void ModelessDialogReportsOpenAndClosesOnce() {
		using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default ); WidgetMessageDialog dialog = WidgetMessageDialog.YesNo( "question", "Question", "Continue?" ); host.Attach( dialog ); host.Arrange( host.Screen.Bounds ); host.Focus( dialog ); Assert.True( dialog.IsOpen );
		Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ); Assert.False( dialog.IsOpen ); Assert.Equal( 0, host.AttachedCount );
	}
	[Fact]
	public void FilePickerHasLegacyKeyboardAcceptanceGesture() {
		WidgetFilePicker picker = new( "files", new Provider(), "/dir", false, [ WidgetFileFilter.AllFiles ] ); WidgetBrowserRequest browse = picker.RequestDirectory( "/dir" ); picker.ApplyLoadResult( WidgetBrowserLoadResult.Success( browse, Snapshot() ) );
		using WidgetHost host = new( new CursesScreen( 40, 10 ), WidgetTheme.Default ); host.ShowModal( picker ); picker.ToggleFile( "/dir/a.txt" );
		Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 's' ) ), TimeSpan.Zero ).Actions.OfType<WidgetPickerValidationRequestedAction>() );
	}
	[Fact]
	public void NumberPromptRendersErrorAndRejectsNonfiniteValues() {
		using WidgetHost host = new( new CursesScreen( 60, 8 ), WidgetTheme.Default ); WidgetNumberPrompt prompt = new( "number", "Number", System.Globalization.CultureInfo.InvariantCulture, 0, 100, "NaN" ); host.ShowModal( prompt ); host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ); Assert.True( prompt.IsOpen ); host.Render(); Assert.Contains( "Enter a number", Cells( host.GetPresentationWindow( prompt ) ) );
	}
	[Fact]
	public void DialogDoesNotOverwriteBorrowedBackground() {
		using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default ); host.Screen.StandardWindow.Write( "Application background" ); WidgetMessageDialog dialog = WidgetMessageDialog.YesNo( "dialog", "Question", "Overlay" ); host.ShowModal( dialog ); host.Render(); Assert.StartsWith( "Application background", Cells( host.Screen ) );
		host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ); host.Render(); Assert.StartsWith( "Application background", Cells( host.Screen ) );
	}
	private static WidgetDirectorySnapshot Snapshot() => new( "/dir", "/", [ new( "/dir/sub", "sub", true ), new( "/dir/a.txt", "a.txt", false ) ] );
	[Fact]
	public void ReopenedPickerRejectsLoadStartedBeforeCancellation() {
		foreach ( bool files in new[] { false, true } ) {
			using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default );
			WidgetFolderPicker folder = new( "folder", new Provider(), "/dir" );
			WidgetFilePicker file = new( "files", new Provider(), "/dir", true, [ WidgetFileFilter.AllFiles ] );
			WidgetDialog picker = files ? file : folder;
			WidgetBrowserRequest request = files ? file.RequestDirectory( "/dir" ) : folder.RequestDirectory( "/dir" );
			host.ShowModal( picker ).Dispose(); host.DrainActions(); host.ShowModal( picker );
			WidgetBrowserLoadResult result = WidgetBrowserLoadResult.Success( request, Snapshot() );
			Assert.False( files ? file.ApplyLoadResult( result ) : folder.ApplyLoadResult( result ) );
		}
	}
	private static string Cells( CursesScreen screen ) => Cells( screen.StandardWindow );
	private static string Cells( CursesWindow screen ) => string.Join( "\n", Enumerable.Range( 0, screen.Rows ).Select( row => string.Concat( Enumerable.Range( 0, screen.Columns ).Select( col => screen.GetCell( row, col ).Content ) ) ) );
	private sealed class Provider : IWidgetFileSystemProvider {
		public StringComparer PathComparer => StringComparer.Ordinal;
		public ValueTask<IReadOnlyList<WidgetFileSystemEntry>> GetRootsAsync( CancellationToken token ) => ValueTask.FromResult<IReadOnlyList<WidgetFileSystemEntry>>( [ new( "/", "/", true ) ] );
		public ValueTask<WidgetDirectorySnapshot> ReadDirectoryAsync( string location, CancellationToken token ) => ValueTask.FromResult( Snapshot() );
		public ValueTask<WidgetFileSelectionValidation> ValidateSelectionAsync( IReadOnlyList<string> paths, WidgetFileSelectionKind kind, CancellationToken token ) => ValueTask.FromResult( WidgetFileSelectionValidation.Valid );
	}
}
