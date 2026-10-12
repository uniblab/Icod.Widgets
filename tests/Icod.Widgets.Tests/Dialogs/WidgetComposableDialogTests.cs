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

namespace Icod.Widgets.Tests.Dialogs;

using Icod.DCurses;
using Xunit;

public sealed class WidgetComposableDialogTests {
	private sealed class CustomDialog( string id ) : WidgetDialog( id, "Custom" ) {
		protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
			if ( input.Kind == WidgetInputKind.Key && input.KeyCode == WidgetKey.Escape ) { this.Finish( host, WidgetDialogOutcome.Cancel ); return true; }
			return false;
		}
	}

	[Fact]
	public void ModalChildReceivesTextAndTabStaysInScope() {
		using WidgetHost host = new( new CursesScreen( 30, 6 ), WidgetTheme.Default ); WidgetButton background = new( "background", "B", "b" ); host.Attach( background ); background.Arrange( host.Screen.Bounds ); host.Focus( background );
		CustomDialog dialog = new( "dialog" ); WidgetTextInput input = new( "name" ); dialog.Add( input );
		using WidgetModalLease lease = host.ShowModal( dialog ); Assert.Same( input, host.FocusedWidget );
		host.Process( WidgetInput.TextInput( new System.Text.Rune( 'x' ) ), TimeSpan.Zero ); Assert.Equal( "x", input.Value );
		host.Process( WidgetInput.Key( WidgetKey.Tab ), TimeSpan.FromMilliseconds( 1 ) ); Assert.NotSame( background, host.FocusedWidget );
		Assert.Equal( WidgetDialogOutcome.Cancel, Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) ).Result.Outcome );
	}

	[Fact]
	public void DuplicateChildIdRejectsBeforeFocusChange() {
		using WidgetHost host = new( new CursesScreen( 20, 5 ), WidgetTheme.Default ); WidgetButton background = new( "same", "B", "b" ); host.Attach( background ); background.Arrange( host.Screen.Bounds ); host.Focus( background );
		CustomDialog dialog = new( "dialog" ); dialog.Add( new WidgetTextInput( "same" ) );
		Assert.Throws<InvalidOperationException>( () => host.ShowModal( dialog ) ); Assert.Same( background, host.FocusedWidget );
	}

	[Fact]
	public void NestedCloseRestoresChildFocusAndCanceledCapture() {
		using WidgetHost host = new( new CursesScreen( 30, 6 ), WidgetTheme.Default ); CustomDialog parent = new( "parent" ); WidgetButton child = new( "child", "Run", "run" ); parent.Add( child );
		using WidgetModalLease outer = host.ShowModal( parent ); host.Focus( child );
		using WidgetModalLease inner = host.ShowModal( new CustomDialog( "inner" ) ); inner.Dispose(); _ = host.DrainActions(); Assert.Same( child, host.FocusedWidget );
	}
}
