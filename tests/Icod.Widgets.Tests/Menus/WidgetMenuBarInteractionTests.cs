/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/

namespace Icod.Widgets.Tests.Menus;

using Icod.DCurses;
using Xunit;

public sealed class WidgetMenuBarInteractionTests {
	[Fact]
	public void OneRowBarOpensContentSizedDropdownWithoutChangingEditorBounds() {
		using Fixture app = new(); CursesRectangle editorBounds = app.Editor.Bounds;
		app.Alt( 'f' ); app.Host.Render();
		Assert.Equal( "new", app.Menu.CurrentItem?.Id );
		Assert.Equal( 1, app.Menu.RenderBounds.Row );
		Assert.Equal( 0, app.Menu.RenderBounds.Column );
		Assert.InRange( app.Menu.RenderBounds.Columns, 18, 35 );
		Assert.Equal( 6, app.Menu.RenderBounds.Rows );
		Assert.Equal( editorBounds, app.Editor.Bounds );
		Assert.Equal( "F", app.Host.Screen.StandardWindow.GetCell( 0, 1 ).Content );
		Assert.True( app.Host.Screen.StandardWindow.GetCell( 0, 1 ).Style.Attributes.HasFlag( CursesTextAttributes.Underline ) );
	}

	[Fact]
	public void ArrowsWrapCommandsAndSwitchRootMenusInBothDirections() {
		using Fixture app = new(); app.Alt( 'f' );
		app.Key( WidgetKey.Up ); Assert.Equal( "exit", app.Menu.CurrentItem?.Id );
		app.Key( WidgetKey.Down ); Assert.Equal( "new", app.Menu.CurrentItem?.Id );
		app.Key( WidgetKey.Right ); Assert.Equal( "copy", app.Menu.CurrentItem?.Id );
		app.Key( WidgetKey.Right ); Assert.Equal( "about", app.Menu.CurrentItem?.Id );
		app.Key( WidgetKey.Right ); Assert.Equal( "new", app.Menu.CurrentItem?.Id );
		app.Key( WidgetKey.Left ); Assert.Equal( "about", app.Menu.CurrentItem?.Id );
		Assert.Equal( 2, app.Menu.Depth ); Assert.True( app.Menu.IsOpen );
	}

	[Fact]
	public void EscapeClosesRootDropdownAndRestoresEditorFocus() {
		using Fixture app = new(); app.Alt( 'f' ); app.Key( WidgetKey.Escape );
		Assert.False( app.Menu.IsOpen ); Assert.Same( app.Editor, app.Host.FocusedWidget );
	}

	[Fact]
	public void AltMnemonicSwitchesAnAlreadyOpenDropdown() {
		using Fixture app = new(); app.Alt( 'f' ); app.Alt( 'h' );
		Assert.Equal( "about", app.Menu.CurrentItem?.Id ); Assert.Equal( 2, app.Menu.Depth );
	}

	[Fact]
	public void ClickingHeadingThenCommandInvokesClickedItemAndRestoresEditor() {
		using Fixture app = new(); app.Pointer( CursesMouseAction.Press, 7, 0 ); app.Pointer( CursesMouseAction.Release, 7, 0 );
		Assert.Equal( "copy", app.Menu.CurrentItem?.Id ); app.Host.Render();
		int column = app.Menu.RenderBounds.Column + 2; int row = app.Menu.RenderBounds.Row + 1;
		app.Pointer( CursesMouseAction.Press, column, row );
		WidgetDispatchResult result = app.Pointer( CursesMouseAction.Release, column, row );
		Assert.Equal( "edit.copy", Assert.IsType<WidgetInvokedAction>( Assert.Single( result.Actions ) ).CommandId );
		Assert.False( app.Menu.IsOpen ); Assert.Same( app.Editor, app.Host.FocusedWidget );
	}

	[Fact]
	public void ClickingAnotherHeadingSwitchesInsteadOfDismissingOrInvoking() {
		using Fixture app = new(); app.Alt( 'f' );
		app.Pointer( CursesMouseAction.Press, 13, 0 );
		Assert.Empty( app.Pointer( CursesMouseAction.Release, 13, 0 ).Actions );
		Assert.True( app.Menu.IsOpen ); Assert.Equal( "about", app.Menu.CurrentItem?.Id );
		app.Key( WidgetKey.Escape ); Assert.Same( app.Editor, app.Host.FocusedWidget );
	}

	[Fact]
	public void NestedSubmenuReturnsToParentBeforeRootMenuCloses() {
		using Fixture app = new(); app.Alt( 'e' ); app.Key( WidgetKey.Down ); app.Key( WidgetKey.Right );
		Assert.Equal( "nested", app.Menu.CurrentItem?.Id ); Assert.Equal( 3, app.Menu.Depth );
		app.Key( WidgetKey.Left ); Assert.Equal( "more", app.Menu.CurrentItem?.Id );
		app.Key( WidgetKey.Right ); app.Key( WidgetKey.Escape ); Assert.Equal( "more", app.Menu.CurrentItem?.Id );
		app.Key( WidgetKey.Escape ); Assert.False( app.Menu.IsOpen ); Assert.Same( app.Editor, app.Host.FocusedWidget );
	}

	[Fact]
	public void PointerHoverDoesNotMoveKeyboardSelectionOrFocus() {
		using Fixture app = new(); app.Pointer( CursesMouseAction.Move, 7, 0 ); Assert.Same( app.Editor, app.Host.FocusedWidget );
		app.Alt( 'f' ); app.Host.Render();
		app.Pointer( CursesMouseAction.Move, 3, 5 ); Assert.Equal( "new", app.Menu.CurrentItem?.Id );
		Assert.Equal( "file.new", Assert.IsType<WidgetInvokedAction>( Assert.Single( app.Key( WidgetKey.Enter ).Actions ) ).CommandId );
	}

	[Fact]
	public void ThemeControlsHoverAppearanceWithoutChangingEnterTarget() {
		using Fixture app = new();
		CursesStyle hover = new( CursesColor.Indexed( 0 ), CursesColor.Indexed( 3 ) );
		CursesStyle selected = new( CursesColor.Indexed( 7 ), CursesColor.Indexed( 4 ) );
		WidgetTheme theme = WidgetTheme.Default with { Parts = WidgetStyles.Empty.With( WidgetStyleRole.PopupHover, hover ).With( WidgetStyleRole.PopupFocused, selected ) };
		app.Host.SetTheme( theme ); app.Alt( 'f' ); app.Pointer( CursesMouseAction.Move, 3, 5 ); app.Host.Render();
		Assert.NotEqual( hover, app.Host.GetPresentationWindow( app.Menu ).GetCell( 4, 1 ).Style );
		app.Host.SetTheme( theme with { HighlightMenuHover = true } ); app.Host.Render();
		Assert.Equal( hover, app.Host.GetPresentationWindow( app.Menu ).GetCell( 4, 1 ).Style );
		Assert.Equal( selected, app.Host.GetPresentationWindow( app.Menu ).GetCell( 2, 1 ).Style );
		Assert.Equal( "new", app.Menu.CurrentItem?.Id );
		app.Pointer( CursesMouseAction.Move, 35, 9 ); app.Host.Render();
		Assert.NotEqual( hover, app.Host.GetPresentationWindow( app.Menu ).GetCell( 4, 1 ).Style );
		Assert.Equal( "file.new", Assert.IsType<WidgetInvokedAction>( Assert.Single( app.Key( WidgetKey.Enter ).Actions ) ).CommandId );
	}

	[Fact]
	public void BarStylesAndMnemonicOverridesAreIndependentFromPopupStyles() {
		using Fixture app = new();
		CursesStyle bar = new( CursesColor.Indexed( 0 ), CursesColor.Indexed( 7 ) );
		CursesStyle active = new( CursesColor.Indexed( 0 ), CursesColor.Indexed( 2 ) );
		CursesStyle mnemonic = new( CursesColor.Indexed( 1 ), CursesColor.Indexed( 7 ), CursesTextAttributes.Bold );
		app.Menu.Styles = WidgetStyles.Empty.With( WidgetStyleRole.MenuBarSurface, bar ).With( WidgetStyleRole.MenuBarItem, bar )
			.With( WidgetStyleRole.MenuBarActive, active ).With( WidgetStyleRole.MenuBarMnemonic, mnemonic );
		app.Host.Render();
		Assert.Equal( bar, app.Host.Screen.StandardWindow.GetCell( 0, 39 ).Style );
		Assert.Equal( mnemonic, app.Host.Screen.StandardWindow.GetCell( 0, 1 ).Style );
		app.Alt( 'f' ); app.Host.Render();
		Assert.Equal( active, app.Host.Screen.StandardWindow.GetCell( 0, 0 ).Style );
		Assert.Equal( active.Background, app.Host.Screen.StandardWindow.GetCell( 0, 1 ).Style.Background );
		Assert.NotEqual( active, app.Host.GetPresentationWindow( app.Menu ).GetCell( 0, 0 ).Style );
	}

	[Theory]
	[InlineData( 1, 1 )]
	[InlineData( 2, 2 )]
	[InlineData( 8, 3 )]
	[InlineData( 20, 4 )]
	public void SmallScreensClipDropdownsAndKeepEscapeUsable( int columns, int rows ) {
		using Fixture app = new(); app.Host.Screen.Resize( columns, rows );
		app.Menu.Arrange( new( 0, 0, 1, columns ) ); app.Alt( 'h' ); app.Host.Render();
		Assert.Equal( app.Menu.RenderBounds, app.Menu.RenderBounds.Intersect( app.Host.Screen.Bounds ) );
		app.Key( WidgetKey.Escape ); Assert.False( app.Menu.IsOpen );
	}

	[Fact]
	public void DynamicTextResizesPopupAndDisablingActiveHeadingSelectsAnother() {
		using Fixture app = new(); app.Alt( 'f' ); app.Host.Render(); int width = app.Menu.RenderBounds.Columns;
		app.Menu.SetPresentation( [ new( "new", "Create a longer document", true ) ] ); app.Host.Render();
		Assert.Equal( "new", app.Menu.CurrentItem?.Id ); Assert.True( app.Menu.RenderBounds.Columns > width );
		app.Menu.SetPresentation( [ new( "file", "File", false ) ] ); app.Host.Render();
		Assert.Equal( "copy", app.Menu.CurrentItem?.Id );
		app.Key( WidgetKey.Left ); Assert.Equal( "about", app.Menu.CurrentItem?.Id );
	}

	[Fact]
	public void DisabledAndSeparatorClicksCannotChangeSelectionOrInvoke() {
		using Fixture app = new(); app.Alt( 'f' );
		foreach ( int row in new[] { 2, 4 } ) {
			app.Pointer( CursesMouseAction.Press, 3, row );
			Assert.Empty( app.Pointer( CursesMouseAction.Release, 3, row ).Actions );
			Assert.Equal( "new", app.Menu.CurrentItem?.Id );
		}
		app.Pointer( CursesMouseAction.Press, 3, 5 );
		Assert.Empty( app.Pointer( CursesMouseAction.Release, 36, 9 ).Actions ); Assert.True( app.Menu.IsOpen );
		app.Pointer( CursesMouseAction.Press, 36, 9 ); Assert.False( app.Menu.IsOpen ); Assert.Same( app.Editor, app.Host.FocusedWidget );
	}

	[Fact]
	public void InvokingAboutClosesMenuBeforeModalAndRestoresEditorOnDismissal() {
		using Fixture app = new(); app.Alt( 'h' );
		Assert.Equal( "help.about", Assert.IsType<WidgetInvokedAction>( Assert.Single( app.Key( WidgetKey.Enter ).Actions ) ).CommandId );
		WidgetMessageDialog about = WidgetMessageDialog.Ok( "about-dialog", "About", "Sample application" ); app.Host.ShowModal( about );
		about.Arrange( new( 3, 6, 5, 28 ) ); app.Host.Render(); Assert.False( app.Menu.IsOpen ); Assert.Same( about, app.Host.FocusedWidget );
		app.Alt( 'f' ); Assert.False( app.Menu.IsOpen );
		app.Key( WidgetKey.Escape ); Assert.Same( app.Editor, app.Host.FocusedWidget );
	}

	[Fact]
	public void ClickingActiveHeadingClosesMenuAndRestoresFocus() {
		using Fixture app = new(); app.Alt( 'f' );
		app.Pointer( CursesMouseAction.Press, 2, 0 );
		Assert.False( app.Menu.IsOpen ); Assert.Same( app.Editor, app.Host.FocusedWidget );
		Assert.Empty( app.Pointer( CursesMouseAction.Release, 2, 0 ).Actions );
	}

	[Fact]
	public void WideAmbiguousSeparatorDoesNotOverwritePopupFrame() {
		CursesScreen screen = new( 30, 8, UnicodeCursesTextWidthProvider.WideAmbiguousInstance );
		using WidgetHost host = new( screen, WidgetTheme.Default );
		WidgetMenuBar menu = new( "bar", [ WidgetMenuItem.Submenu( "file", "File", [
			WidgetMenuItem.Command( "open", "Open 界", "open", 'o' ), WidgetMenuItem.Separator( "sep" ), WidgetMenuItem.Command( "quit", "Quit", "quit" ) ], 'f' ) ] );
		host.Attach( menu ); menu.Arrange( new( 0, 0, 1, 30 ) ); host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new( 'f' ) ), TimeSpan.Zero ); host.Render();
		CursesWindow window = host.GetPresentationWindow( menu );
		Assert.Equal( "+", window.GetCell( 0, menu.RenderBounds.Columns - 1 ).Content );
		Assert.Equal( "|", window.GetCell( 2, menu.RenderBounds.Columns - 1 ).Content );
	}

	[Fact]
	public void TerminalFocusLossCancelsPendingMenuClick() {
		using Fixture app = new(); app.Alt( 'f' ); app.Pointer( CursesMouseAction.Press, 3, 5 );
		app.Host.Process( WidgetInput.FocusChanged( CursesFocusState.Unfocused ), TimeSpan.Zero );
		app.Host.Process( WidgetInput.FocusChanged( CursesFocusState.Focused ), TimeSpan.Zero );
		Assert.Empty( app.Pointer( CursesMouseAction.Release, 3, 5 ).Actions );
		Assert.True( app.Menu.IsOpen ); Assert.Equal( "new", app.Menu.CurrentItem?.Id );
	}

	[Theory]
	[InlineData( 40, true )]
	[InlineData( 20, false )]
	public void NestedPanelsPreserveParentAndOverlayRightOrOnTop( int columns, bool fitsRight ) {
		using Fixture app = new(); app.Host.Screen.Resize( columns, 12 ); app.Menu.Arrange( new( 0, 0, 1, columns ) );
		app.Alt( 'e' ); app.Key( WidgetKey.Down ); app.Host.Render();
		CursesRectangle parent = app.Menu.RenderBounds;
		app.Key( WidgetKey.Right ); app.Host.Render();
		var panels = Panels( app.Host.Screen ).Where( panel => panel.IsVisible ).ToArray();
		Assert.Equal( 2, panels.Length );
		Assert.Equal( parent, panels[ 0 ].Bounds );
		Assert.Equal( fitsRight ? parent.Column + parent.Columns : parent.Column, panels[ 1 ].Bounds.Column );
		Assert.Contains( "More", Row( panels[ 0 ].ContentWindow, 2 ) );
		Assert.Contains( "Nested", Row( panels[ 1 ].ContentWindow, 1 ) );
		app.Key( WidgetKey.Left ); app.Host.Render();
		Assert.Single( Panels( app.Host.Screen ), panel => panel.IsVisible );
		Assert.Equal( "more", app.Menu.CurrentItem?.Id );
	}

	[Fact]
	public void ClickOnExposedParentInvokesItAndClosesAllPanels() {
		using Fixture app = new(); app.Alt( 'e' ); app.Key( WidgetKey.Down ); app.Host.Render();
		CursesRectangle parent = app.Menu.RenderBounds;
		app.Key( WidgetKey.Right ); app.Host.Render();
		app.Pointer( CursesMouseAction.Press, parent.Column + 2, parent.Row + 1 );
		WidgetDispatchResult result = app.Pointer( CursesMouseAction.Release, parent.Column + 2, parent.Row + 1 );
		Assert.Equal( "edit.copy", Assert.IsType<WidgetInvokedAction>( Assert.Single( result.Actions ) ).CommandId );
		Assert.False( app.Menu.IsOpen ); Assert.Empty( Panels( app.Host.Screen ) );
	}

	private static CursesPanel[] Panels( CursesScreen screen ) => (CursesPanel[])typeof( CursesScreen ).GetMethod( "SnapshotPanelsBottomToTop", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic )!.Invoke( screen, null )!;

	private static string Row( CursesWindow window, int row ) => string.Concat( Enumerable.Range( 0, window.Columns ).Select( column => window.GetCell( row, column ).Content ) );

	private sealed class Fixture : IDisposable {
		internal WidgetHost Host { get; } = new( new CursesScreen( 40, 12 ), WidgetTheme.Default );
		internal WidgetButton Editor { get; } = new( "editor", "Document stays here", "editor" );
		internal WidgetMenuBar Menu { get; } = new( "bar", [
			WidgetMenuItem.Submenu( "file", "File", [
				WidgetMenuItem.Command( "disabled", "Unavailable", "disabled", isEnabled: false ),
				WidgetMenuItem.Command( "new", "New file", "file.new", 'n', shortcut: new( WidgetKey.Character, WidgetInputModifiers.Control, 'n' ) ),
				WidgetMenuItem.Separator( "sep" ), WidgetMenuItem.Command( "exit", "Exit", "file.exit", 'x' ) ], 'f' ),
			WidgetMenuItem.Submenu( "edit", "Edit", [ WidgetMenuItem.Command( "copy", "Copy", "edit.copy", 'c' ),
				WidgetMenuItem.Submenu( "more", "More", [ WidgetMenuItem.Command( "nested", "Nested", "edit.nested" ) ] ) ], 'e' ),
			WidgetMenuItem.Submenu( "help", "Help", [ WidgetMenuItem.Command( "about", "About", "help.about", 'a' ) ], 'h' )
		] );
		internal Fixture() { this.Host.Attach( this.Editor ); this.Host.Attach( this.Menu ); this.Editor.Arrange( new( 1, 0, 10, 40 ) ); this.Menu.Arrange( new( 0, 0, 1, 40 ) ); this.Host.Focus( this.Editor ); }
		internal WidgetDispatchResult Key( WidgetKey key ) => this.Host.Process( WidgetInput.Key( key ), TimeSpan.Zero );
		internal void Alt( char value ) => this.Host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new( value ) ), TimeSpan.Zero );
		internal WidgetDispatchResult Pointer( CursesMouseAction action, int column, int row ) => this.Host.Process( WidgetInput.PointerInput( new( action, action == CursesMouseAction.Move ? CursesMouseButton.None : CursesMouseButton.Primary, column, row ) ), TimeSpan.Zero );
		public void Dispose() => this.Host.Dispose();
	}
}
