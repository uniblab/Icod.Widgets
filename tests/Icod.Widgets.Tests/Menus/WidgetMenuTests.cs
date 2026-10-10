/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Tests.Menus;

using Icod.DCurses;
using Xunit;

public sealed class WidgetMenuTests {
	[Fact]
	public void AltMnemonicOpensTopLevelMenuFromAnotherFocusedWidget() {
		WidgetMenuBar menu = new( "menu", [
			WidgetMenuItem.Submenu( "file", "File", [ WidgetMenuItem.Command( "new", "New", "file.new" ) ], 'f' ),
			WidgetMenuItem.Submenu( "view", "View", [ WidgetMenuItem.Command( "go-file", "Go to File...", "view.file" ) ], 'v' )
		] );
		using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default );
		WidgetButton editor = new( "editor", "Editor", "editor.focus" ); host.Attach( editor ); host.Attach( menu ); host.Arrange( host.Screen.Bounds ); Assert.True( host.Focus( editor ) );

		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new( 'V' ) ), TimeSpan.Zero );

		Assert.True( result.Handled ); Assert.True( menu.IsOpen ); Assert.Equal( 2, menu.Depth ); Assert.Equal( "go-file", menu.CurrentItem?.Id ); Assert.Same( menu, host.FocusedWidget );
	}

	[Fact]
	public void DisabledTopLevelMnemonicIsInert() {
		WidgetMenuBar menu = new( "menu", [ WidgetMenuItem.Submenu( "edit", "Edit", [ WidgetMenuItem.Command( "copy", "Copy", "edit.copy" ) ], 'e', isEnabled: false ) ] );
		using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default );
		WidgetButton editor = new( "editor", "Editor", "editor.focus" ); host.Attach( editor ); host.Attach( menu ); host.Arrange( host.Screen.Bounds ); Assert.True( host.Focus( editor ) );

		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new( 'e' ) ), TimeSpan.Zero );

		Assert.False( result.Handled ); Assert.Empty( result.Actions ); Assert.False( menu.IsOpen ); Assert.Same( editor, host.FocusedWidget );
	}

	[Fact]
	public void PlainMnemonicInvokesCommandInOpenMenuLevel() {
		WidgetMenuBar menu = new( "menu", [
			WidgetMenuItem.Submenu( "file", "File", [ WidgetMenuItem.Command( "find", "Find", "file.find", 'f' ) ], 'f' ),
			WidgetMenuItem.Submenu( "view", "View", [ WidgetMenuItem.Command( "go-file", "Go to File...", "view.file", 'f' ) ], 'v' )
		] );
		using WidgetHost host = Focus( menu );
		host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new( 'v' ) ), TimeSpan.Zero );

		WidgetInvokedAction action = Assert.IsType<WidgetInvokedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Character, character: new( 'F' ) ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) );

		Assert.Equal( "view.file", action.CommandId ); Assert.False( menu.IsOpen );
	}

	[Fact]
	public void ControlShortcutInvokesDirectlyWithoutOpeningMenu() {
		WidgetMenuShortcut shortcut = new( WidgetKey.Character, WidgetInputModifiers.Control, 'l' );
		WidgetMenuBar menu = new( "menu", [ WidgetMenuItem.Submenu( "view", "View", [ WidgetMenuItem.Command( "line", "Go to Line...", "view.line", shortcut: shortcut ) ], 'v' ) ] );
		using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default );
		WidgetButton editor = new( "editor", "Editor", "editor.focus" ); host.Attach( editor ); host.Attach( menu ); host.Arrange( host.Screen.Bounds ); Assert.True( host.Focus( editor ) );

		WidgetInvokedAction action = Assert.IsType<WidgetInvokedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 'L' ) ), TimeSpan.Zero ).Actions ) );

		Assert.Equal( "view.line", action.CommandId ); Assert.False( menu.IsOpen ); Assert.Same( editor, host.FocusedWidget );
	}

	[Fact]
	public void DisabledAndSeparatorsAreInert() {
		WidgetPopupMenu menu = new( "menu", [ WidgetMenuItem.Command( "off", "Disabled", "off", isEnabled: false ), WidgetMenuItem.Separator( "sep" ), WidgetMenuItem.Command( "on", "Enabled", "on" ) ] );
		using WidgetHost host = Focus( menu ); menu.Open();
		Assert.Empty( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Equal( "on", menu.CurrentItem?.Id );
	}

	[Fact]
	public void CheckableCommandReportsNewStateOnce() {
		WidgetPopupMenu menu = new( "menu", [ WidgetMenuItem.Checkable( "grid", "Grid", "view.grid" ) ] ); using WidgetHost host = Focus( menu ); menu.Open();
		WidgetCheckedInvokedAction action = Assert.IsType<WidgetCheckedInvokedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) );
		Assert.True( action.IsChecked ); Assert.Empty( host.DrainActions() );
	}

	[Fact]
	public void CheckableMnemonicReportsNewStateOnce() {
		WidgetPopupMenu menu = new( "menu", [ WidgetMenuItem.Checkable( "grid", "Grid", "view.grid", 'g' ) ] ); using WidgetHost host = Focus( menu ); menu.Open();

		WidgetCheckedInvokedAction action = Assert.IsType<WidgetCheckedInvokedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Character, character: new( 'G' ) ), TimeSpan.Zero ).Actions ) );

		Assert.True( action.IsChecked ); Assert.Empty( host.DrainActions() );
	}

	[Fact]
	public void DuplicateShortcutRejects() {
		WidgetMenuShortcut shortcut = new( WidgetKey.Character, WidgetInputModifiers.Control, 'x' );
		Assert.Throws<ArgumentException>( () => new WidgetPopupMenu( "menu", [ WidgetMenuItem.Command( "a", "A", "a", shortcut: shortcut ), WidgetMenuItem.Command( "b", "B", "b", shortcut: shortcut ) ] ) );
	}

	[Fact]
	public void DuplicateMnemonicRejectsWithinOneLevelCaseInsensitively() {
		Assert.Throws<ArgumentException>( () => new WidgetPopupMenu( "menu", [
			WidgetMenuItem.Command( "find", "Find", "find", 'f' ),
			WidgetMenuItem.Command( "file", "Go to File...", "file", 'F' )
		] ) );
	}

	[Fact]
	public void SubmenuCloseRestoresParentItem() {
		WidgetPopupMenu menu = new( "menu", [ WidgetMenuItem.Submenu( "tools", "Tools", [ WidgetMenuItem.Command( "run", "Run", "run" ) ] ) ] ); using WidgetHost host = Focus( menu ); menu.Open();
		host.Process( WidgetInput.Key( WidgetKey.Right ), TimeSpan.Zero );
		host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Equal( 1, menu.Depth ); Assert.Equal( "tools", menu.CurrentItem?.Id ); Assert.Same( menu, host.FocusedWidget );
	}

	[Fact]
	public void DeepMenuClipsAndRejectsSeventeenLevels() {
		WidgetMenuItem sixteen = Chain( 16 ); WidgetPopupMenu menu = new( "ok", [ sixteen ] ); using WidgetHost host = Focus( menu, 1, 1 ); menu.Open(); host.Render();
		Assert.True( menu.RenderBounds.Rows <= 1 );
		Assert.Throws<ArgumentException>( () => new WidgetPopupMenu( "deep", [ Chain( 17 ) ] ) );
	}

	[Fact]
	public void PopupRendersBorderAroundInsetItems() {
		WidgetPopupMenu menu = new( "menu", [ WidgetMenuItem.Command( "alpha", "Alpha", "alpha" ) ] );
		using WidgetHost host = Focus( menu, columns: 20, rows: 5 ); menu.Open(); host.Render();
		CursesWindow window = host.GetPresentationWindow( menu );

		Assert.Equal( 3, menu.RenderBounds.Rows );
		Assert.Equal( "┌", window.GetCell( 0, 0 ).Content ); Assert.Equal( "┐", window.GetCell( 0, 19 ).Content );
		Assert.Equal( "└", window.GetCell( 2, 0 ).Content ); Assert.Equal( "┘", window.GetCell( 2, 19 ).Content );
		Assert.Equal( "A", window.GetCell( 1, 1 ).Content );
	}

	[Fact]
	public void PopupRendersShortcutInRightAlignedColumn() {
		WidgetMenuShortcut shortcut = new( WidgetKey.Character, WidgetInputModifiers.Control, 'l' );
		WidgetPopupMenu menu = new( "menu", [ WidgetMenuItem.Command( "line", "Go to Line...", "view.line", shortcut: shortcut ) ] );
		using WidgetHost host = Focus( menu, columns: 24, rows: 5 ); menu.Open(); host.Render();

		string row = string.Concat( Enumerable.Range( 0, 24 ).Select( column => host.GetPresentationWindow( menu ).GetCell( 1, column ).Content ) );

		Assert.Equal( "│Go to Line...   Ctrl+L│", row );
	}

	[Fact]
	public void CompactMenuScrollsCurrentItemIntoView() {
		WidgetPopupMenu menu = new( "menu", [ WidgetMenuItem.Command( "a", "Alpha", "a" ), WidgetMenuItem.Command( "b", "Beta", "b" ), WidgetMenuItem.Command( "c", "Gamma", "c" ) ] );
		using WidgetHost host = Focus( menu, columns: 20, rows: 1 ); menu.Open();
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.Zero );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.FromMilliseconds( 1 ) );
		host.Render();
		Assert.StartsWith( "Gamma", string.Concat( Enumerable.Range( 0, menu.RenderBounds.Columns ).Select( column => host.GetPresentationWindow( menu ).GetCell( 0, column ).Content ) ) );
	}

	private static WidgetMenuItem Chain( int levels ) => 1 == levels ? WidgetMenuItem.Command( "leaf", "Leaf", "leaf" ) : WidgetMenuItem.Submenu( $"level-{levels}", "More", [ Chain( levels - 1 ) ] );
	private static WidgetHost Focus( WidgetPopupMenu menu, int columns = 30, int rows = 8 ) { WidgetHost host = new( new CursesScreen( columns, rows ), WidgetTheme.Default ); host.Attach( menu ); host.Arrange( host.Screen.Bounds ); Assert.True( host.Focus( menu ) ); return host; }
}
