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
	public void DuplicateShortcutRejects() {
		WidgetMenuShortcut shortcut = new( WidgetKey.Character, WidgetInputModifiers.Control, 'x' );
		Assert.Throws<ArgumentException>( () => new WidgetPopupMenu( "menu", [ WidgetMenuItem.Command( "a", "A", "a", shortcut: shortcut ), WidgetMenuItem.Command( "b", "B", "b", shortcut: shortcut ) ] ) );
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

	private static WidgetMenuItem Chain( int levels ) => 1 == levels ? WidgetMenuItem.Command( "leaf", "Leaf", "leaf" ) : WidgetMenuItem.Submenu( $"level-{levels}", "More", [ Chain( levels - 1 ) ] );
	private static WidgetHost Focus( WidgetPopupMenu menu, int columns = 30, int rows = 8 ) { WidgetHost host = new( new CursesScreen( columns, rows ), WidgetTheme.Default ); host.Attach( menu ); host.Arrange( host.Screen.Bounds ); Assert.True( host.Focus( menu ) ); return host; }
}
