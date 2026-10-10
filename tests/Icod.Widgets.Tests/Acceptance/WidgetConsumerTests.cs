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

namespace Icod.Widgets.Tests.Acceptance;
using Icod.DCurses;
using Xunit;

public sealed class WidgetConsumerTests {
	[Fact]
	public void DialogCancellationLeavesCallerModelUnchanged() { int model = 7; using WidgetHost host = new( new CursesScreen( 20, 5 ), WidgetTheme.Default ); host.ShowModal( WidgetMessageDialog.OkCancel( "dialog", "Edit", "Change?" ) ); WidgetDialogCompletedAction action = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) ); if ( WidgetDialogOutcome.Ok == action.Result.Outcome ) model = 8; Assert.Equal( 7, model ); }
	[Fact]
	public void InventoryActionReachesCallerOnce() { WidgetActionList list = new( "actions" ); list.SetItems( [ new( "use", "Use" ) ] ); using WidgetHost host = new( new CursesScreen( 20, 3 ), WidgetTheme.Default ); host.Attach( list ); host.Arrange( host.Screen.Bounds ); host.Focus( list ); Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions.OfType<WidgetInvokedAction>() ); Assert.Empty( host.DrainActions() ); }
}
