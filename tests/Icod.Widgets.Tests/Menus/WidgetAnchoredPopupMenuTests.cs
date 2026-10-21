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

namespace Icod.Widgets.Tests.Menus;

using Icod.DCurses;
using Xunit;

public sealed class WidgetAnchoredPopupMenuTests {
	[Theory]
	[InlineData( 0, 0 )]
	[InlineData( 0, 19 )]
	[InlineData( 5, 0 )]
	[InlineData( 5, 19 )]
	public void OpenAtMeasuresAndClampsRootPopup( int row, int column ) {
		WidgetPopupMenu menu = Menu(); using WidgetHost host = Host( menu );

		menu.OpenAt( row, column ); host.Render();

		Assert.Equal( menu.RenderBounds, menu.RenderBounds.Intersect( host.Screen.Bounds ) );
		Assert.Equal( 4, menu.RenderBounds.Rows );
		Assert.Equal( 19, menu.RenderBounds.Columns );
		Assert.Equal( Math.Clamp( row, 0, 2 ), menu.RenderBounds.Row );
		Assert.Equal( Math.Clamp( column, 0, 1 ), menu.RenderBounds.Column );
	}

	[Fact]
	public void AnchoredPopupRetainsKeyboardPointerAndDismissalBehavior() {
		WidgetPopupMenu menu = Menu(); using WidgetHost host = Host( menu, out WidgetButton prior );

		menu.OpenAt( 1, 2 );
		Assert.Same( menu, host.FocusedWidget );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.Zero );
		WidgetInvokedAction keyboard = Assert.IsType<WidgetInvokedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) );
		Assert.Equal( "long", keyboard.CommandId ); Assert.Same( prior, host.FocusedWidget );

		menu.OpenAt( 1, 2 ); host.Render(); int itemRow = menu.RenderBounds.Row + 1; int itemColumn = menu.RenderBounds.Column + 1;
		host.Process( Pointer( CursesMouseAction.Press, itemColumn, itemRow ), TimeSpan.FromMilliseconds( 2 ) );
		WidgetInvokedAction pointer = Assert.IsType<WidgetInvokedAction>( Assert.Single(
			host.Process( Pointer( CursesMouseAction.Release, itemColumn, itemRow ), TimeSpan.FromMilliseconds( 3 ) ).Actions ) );
		Assert.Equal( "alpha", pointer.CommandId ); Assert.Same( prior, host.FocusedWidget );

		menu.OpenAt( 1, 2 );
		host.Process( Pointer( CursesMouseAction.Press, 19, 5 ), TimeSpan.FromMilliseconds( 4 ) );
		Assert.False( menu.IsOpen ); Assert.Same( prior, host.FocusedWidget );
	}

	[Fact]
	public void ExistingOpenUsesArrangedBoundsUnchanged() {
		WidgetPopupMenu menu = Menu(); using WidgetHost host = Host( menu );
		menu.Arrange( new CursesRectangle( 2, 3, 5, 12 ) );

		menu.Open(); host.Render();

		Assert.Equal( new CursesRectangle( 2, 3, 4, 12 ), menu.RenderBounds );
	}

	private static WidgetPopupMenu Menu() => new( "menu", [
		WidgetMenuItem.Command( "alpha", "Alpha", "alpha" ),
		WidgetMenuItem.Command( "long", "Longest command", "long" )
	] );

	private static WidgetHost Host( WidgetPopupMenu menu ) {
		WidgetHost host = new( new CursesScreen( 20, 6 ), WidgetTheme.Default ); host.Attach( menu ); return host;
	}

	private static WidgetHost Host( WidgetPopupMenu menu, out WidgetButton prior ) {
		WidgetHost host = Host( menu ); prior = new WidgetButton( "prior", "Prior", "prior" ); host.Attach( prior );
		prior.Arrange( new CursesRectangle( 0, 0, 1, 10 ) ); Assert.True( host.Focus( prior ) ); return host;
	}

	private static WidgetInput Pointer( CursesMouseAction action, int column, int row ) =>
		WidgetInput.PointerInput( new CursesMouseEvent( action, CursesMouseButton.Primary, column, row ) );
}
