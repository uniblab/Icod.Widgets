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

namespace Icod.Widgets.Tests.Qualification;
using Icod.DCurses;
using Xunit;

public sealed class WidgetLifecycleTests {
	[Fact]
	public void RepeatedAttachDetachReturnsHostToEmpty() {
		using WidgetHost host = new( new CursesScreen( 20, 2 ), WidgetTheme.Default );
		for ( int i = 0; i < 500; i++ ) { WidgetButton button = new( $"b{i}", "B", "b" ); host.Attach( button ); host.Arrange( host.Screen.Bounds ); host.Focus( button ); Assert.True( host.Detach( button ) ); }
		Assert.Equal( 0, host.AttachedCount ); Assert.Null( host.FocusedWidget );
	}

	[Fact]
	public void HostDisposalClosesModalsWithoutCompletionActions() {
		WidgetHost host = new( new CursesScreen( 20, 2 ), WidgetTheme.Default ); WidgetMessageDialog dialog = WidgetMessageDialog.OkCancel( "dialog", "D", "D" ); host.ShowModal( dialog );
		host.Dispose(); Assert.False( dialog.IsOpen );
	}

	[Fact]
	public void OpeningModalCancelsBackgroundPointerGesture() {
		using WidgetHost host = new( new CursesScreen( 20, 3 ), WidgetTheme.Default ); WidgetButton button = new( "button", "Run", "run" ); host.Attach( button ); host.Arrange( host.Screen.Bounds );
		host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press, CursesMouseButton.Primary, 0, 1 ) ), TimeSpan.Zero );
		WidgetModalLease modal = host.ShowModal( WidgetMessageDialog.OkCancel( "dialog", "D", "D" ) ); modal.Dispose(); _ = host.DrainActions();
		WidgetDispatchResult release = host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Release, CursesMouseButton.Primary, 0, 1 ) ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.DoesNotContain( release.Actions, action => action is WidgetInvokedAction );
	}

	[Fact]
	public void ContainerVisibilityAndEnablementApplyToDescendants() {
		using WidgetHost host = new( new CursesScreen( 20, 3 ), WidgetTheme.Default ); WidgetStack stack = new( "stack", WidgetOrientation.Vertical ); WidgetButton button = new( "button", "Run", "run" ); stack.Add( button ); host.Attach( stack ); host.Arrange( host.Screen.Bounds ); host.Focus( button );
		stack.IsVisible = false; Assert.Null( host.FocusedWidget ); Assert.False( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Handled );
		stack.IsVisible = true; stack.IsEnabled = false; Assert.False( host.Focus( button ) );
	}

	[Fact]
	public void ContainersRejectAncestorCyclesBeforeMutation() {
		WidgetStack parent = new( "parent", WidgetOrientation.Vertical ); WidgetStack child = new( "child", WidgetOrientation.Vertical ); parent.Add( child );
		Assert.Throws<InvalidOperationException>( () => child.Add( parent ) );
		Assert.Empty( child.Children ); Assert.Single( parent.Children );
	}
}
