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
