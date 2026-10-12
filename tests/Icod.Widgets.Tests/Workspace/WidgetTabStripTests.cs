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

namespace Icod.Widgets.Tests.Workspace;

using Icod.DCurses;
using Xunit;

public sealed class WidgetTabStripTests {
	[Fact]
	public void DuplicateKeysLeavePreviousSnapshotIntact() {
		WidgetTabStrip strip = new( "tabs" ); strip.SetTabs( [ new( "a", "A" ), new( "b", "B" ) ], "b" );
		Assert.Throws<ArgumentException>( () => strip.SetTabs( [ new( "a", "A" ), new( "a", "Again" ) ] ) );
		Assert.Equal( "b", strip.ActiveKey ); Assert.Equal( 2, strip.Tabs.Count );
		Assert.Throws<ArgumentOutOfRangeException>( () => strip.SetTabs( Enumerable.Range( 0, 1025 ).Select( i => new WidgetTab( $"{i}", "x" ) ).ToArray() ) );
	}

	[Fact]
	public void RemovedActiveSelectsNearestSurvivor() {
		WidgetTabStrip strip = new( "tabs" ); strip.SetTabs( [ new( "a", "A" ), new( "b", "B" ), new( "c", "C" ) ], "b" );
		strip.SetTabs( [ new( "a", "A" ), new( "c", "C" ) ] ); Assert.Equal( "c", strip.ActiveKey );
		strip.SetTabs( [ new( "a", "A" ) ] ); Assert.Equal( "a", strip.ActiveKey );
		strip.SetTabs( [] ); Assert.Null( strip.ActiveKey );
	}

	[Fact]
	public void OverflowKeepsActiveVisibleEvenWithWideGlyphAndOneColumn() {
		using WidgetHost host = new( new CursesScreen( 1, 1 ), WidgetTheme.Default );
		WidgetTabStrip strip = new( "tabs" ); host.Attach( strip ); strip.Arrange( host.Screen.Bounds );
		strip.SetTabs( Enumerable.Range( 0, 100 ).Select( i => new WidgetTab( $"k{i}", "中📄" ) ).ToArray(), "k99" );
		Assert.Contains( "k99", strip.VisibleKeys ); host.Render();
	}

	[Fact]
	public void PointerAndKeyboardEmitRequestsWithoutMutatingActiveKey() {
		using WidgetHost host = new( new CursesScreen( 40, 1 ), WidgetTheme.Default );
		WidgetTabStrip strip = new( "tabs" ); strip.SetTabs( [ new( "a", "A" ), new( "b", "B", true ) ], "a" );
		host.Attach( strip ); strip.Arrange( host.Screen.Bounds ); Assert.True( host.Focus( strip ) );
		WidgetDispatchResult cycle = host.Process( WidgetInput.Key( WidgetKey.Tab, WidgetInputModifiers.Control ), TimeSpan.Zero );
		Assert.Equal( "b", Assert.IsType<WidgetTabActivationRequestedAction>( Assert.Single( cycle.Actions ) ).TabKey );
		Assert.Equal( "a", strip.ActiveKey );
		WidgetDispatchResult close = host.Process( WidgetInput.Key( WidgetKey.Delete ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Equal( "a", Assert.IsType<WidgetTabCloseRequestedAction>( Assert.Single( close.Actions ) ).TabKey );
		WidgetDispatchResult pointer = host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press, CursesMouseButton.Primary, 8, 0 ) ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( "b", Assert.IsType<WidgetTabActivationRequestedAction>( Assert.Single( pointer.Actions ) ).TabKey );
	}

	[Fact]
	public void EmptyAndDisabledTabsRemainCancelable() {
		using WidgetHost host = new( new CursesScreen( 10, 1 ), WidgetTheme.Default );
		WidgetTabStrip strip = new( "tabs" ); host.Attach( strip ); strip.Arrange( host.Screen.Bounds ); host.Focus( strip );
		strip.SetTabs( [ new( "a", "A", IsEnabled: false ) ] ); Assert.Null( strip.ActiveKey );
		Assert.Empty( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions );
		Assert.Throws<ArgumentException>( () => strip.SetTabs( [ new( "a", "A", IsEnabled: false ) ], "a" ) );
		Assert.Empty( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 1 ) ).Actions );
	}
}
