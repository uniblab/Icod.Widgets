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

public sealed class WidgetDynamicMenuTests {
	[Fact]
	public void UpdateOpenMenuKeepsStableSelectionAndChoosesNext() {
		using WidgetHost host = new( new CursesScreen( 30, 6 ), WidgetTheme.Default );
		WidgetPopupMenu menu = new( "menu", [ WidgetMenuItem.Command( "a", "A", "a" ), WidgetMenuItem.Command( "b", "B", "b" ), WidgetMenuItem.Command( "c", "C", "c" ) ] );
		host.Attach( menu ); host.Arrange( host.Screen.Bounds ); host.Focus( menu ); menu.Open();
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.Zero ); Assert.Equal( "b", menu.CurrentItem?.Id );
		menu.SetPresentation( [ new( "b", "New B", false ) ] ); Assert.Equal( "c", menu.CurrentItem?.Id );
		menu.SetPresentation( [ new( "b", "B", false ), new( "c", "New C", false ) ] ); Assert.Equal( "a", menu.CurrentItem?.Id );
		menu.SetPresentation( [ new( "a", "New A", false ), new( "b", "B", false ), new( "c", "C", false ) ] ); Assert.False( menu.IsOpen );
	}

	[Fact]
	public void ControlledCheckableEmitsNextValueWithoutMutatingDefinition() {
		using WidgetHost host = new( new CursesScreen( 30, 5 ), WidgetTheme.Default );
		WidgetMenuItem item = WidgetMenuItem.Checkable( "toggle", "Toggle", "toggle" ); WidgetPopupMenu menu = new( "menu", [ item ] );
		host.Attach( menu ); host.Arrange( host.Screen.Bounds ); host.Focus( menu );
		menu.SetPresentation( [ new( "toggle", "Controlled", true, true ) ] ); menu.Open();
		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero );
		Assert.False( Assert.IsType<WidgetCheckedInvokedAction>( Assert.Single( result.Actions ) ).IsChecked );
		Assert.False( item.IsChecked ); menu.Open(); Assert.True( menu.CurrentItem?.IsChecked );
	}

	[Fact]
	public void DuplicateUnknownAndOversizedStateRejectBeforeMutation() {
		WidgetPopupMenu menu = new( "menu", [ WidgetMenuItem.Command( "a", "A", "a" ) ] );
		menu.SetPresentation( [ new( "a", "Updated", true ) ] );
		Assert.Throws<ArgumentException>( () => menu.SetPresentation( [ new( "a", "X", true ), new( "a", "Y", true ) ] ) );
		Assert.Throws<ArgumentException>( () => menu.SetPresentation( [ new( "missing", "X", true ) ] ) );
		Assert.Throws<ArgumentOutOfRangeException>( () => menu.SetPresentation( Enumerable.Range( 0, WidgetLimits.MaximumItems + 1 ).Select( _ => new WidgetMenuItemPresentation( "a", "x", true ) ).ToArray() ) );
		menu.Open(); Assert.Equal( "Updated", menu.CurrentItem?.Text );
		Assert.Throws<ArgumentException>( () => new WidgetPopupMenu( "dup", [ WidgetMenuItem.Command( "a", "A", "a" ), WidgetMenuItem.Command( "a", "B", "b" ) ] ) );
	}
}
