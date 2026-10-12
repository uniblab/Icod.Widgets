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

namespace Icod.Widgets.Tests.GameUi;

using Icod.DCurses;
using Xunit;

public sealed class WidgetActionableStatusTests {
	[Fact]
	public void OldThreeArgumentFieldRemainsInertAndDeconstructs() {
		WidgetStatusField field = new( "name", "Text", 1 ); ( string name, string text, int priority ) = field;
		Assert.Equal( "name", name ); Assert.Equal( "Text", text ); Assert.Equal( 1, priority );
		WidgetStatusBar bar = new( "status" ); bar.SetFields( [ field ] ); Assert.False( bar.IsFocusable );
	}

	[Fact]
	public void LateActionableFieldReceivesFocusAndKeyboardPointerActions() {
		using WidgetHost host = new( new CursesScreen( 20, 1 ), WidgetTheme.Default ); WidgetStatusBar bar = new( "status" );
		host.Attach( bar ); bar.Arrange( host.Screen.Bounds ); Assert.False( host.Focus( bar ) );
		bar.SetFields( [ new WidgetStatusField( "encoding", "UTF-8", 10 ) { CommandId = "encoding.choose" } ] );
		Assert.Contains( "encoding", bar.VisibleFieldNames ); Assert.True( host.Focus( bar ) );
		Assert.Equal( "encoding.choose", Assert.IsType<WidgetInvokedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) ).CommandId );
		host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press, CursesMouseButton.Primary, 1, 0 ) ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Equal( "encoding.choose", Assert.IsType<WidgetInvokedAction>( Assert.Single( host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Release, CursesMouseButton.Primary, 1, 0 ) ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) ).CommandId );
	}

	[Fact]
	public void OverflowAndReplacementRepairFieldFocus() {
		using WidgetHost host = new( new CursesScreen( 8, 1 ), WidgetTheme.Default ); WidgetStatusBar bar = new( "status" ); host.Attach( bar ); bar.Arrange( host.Screen.Bounds );
		bar.SetFields( [ new( "high", "HIGH", 10 ) { CommandId = "high" }, new( "low", "LOW", 0 ) { CommandId = "low" } ] );
		Assert.Contains( "high", bar.VisibleFieldNames ); Assert.DoesNotContain( "low", bar.VisibleFieldNames );
		bar.SetFields( [ new( "other", "OTHER", 10 ) { CommandId = "other" } ] ); Assert.Equal( "other", bar.FocusedFieldName );
		Assert.Throws<ArgumentException>( () => bar.SetFields( [ new( "x", "X", 1 ), new( "x", "Y", 2 ) ] ) );
		Assert.Equal( "other", bar.FocusedFieldName );
	}

	[Fact]
	public void OneColumnRendersAndInvokesWithoutIndexOverflow() {
		using WidgetHost host = new( new CursesScreen( 1, 1 ), WidgetTheme.Default ); WidgetStatusBar bar = new( "status" ); host.Attach( bar ); bar.Arrange( host.Screen.Bounds );
		bar.SetFields( [ new( "one", "中", 2 ) { CommandId = "go" } ] ); host.Render(); Assert.True( host.Focus( bar ) );
		Assert.Equal( "go", Assert.IsType<WidgetInvokedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) ).CommandId );
	}
}
