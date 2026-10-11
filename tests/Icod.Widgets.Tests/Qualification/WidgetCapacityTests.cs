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

public sealed class WidgetCapacityTests {
	[Fact]
	public void ActionQueueRejects4097BeforeEnqueue() {
		using WidgetHost host = new( new CursesScreen( 2, 1 ), WidgetTheme.Default );
		for ( int i = 0; i < WidgetLimits.MaximumPendingActions; i++ ) host.Enqueue( new WidgetInvokedAction( "probe", i.ToString() ) );
		Assert.Throws<InvalidOperationException>( () => host.Enqueue( new WidgetInvokedAction( "probe", "overflow" ) ) );
		Assert.Equal( WidgetLimits.MaximumPendingActions, host.DrainActions().Count );
	}

	[Fact]
	public void FullQueueRejectsModalCloseBeforeStateMutationAndCanRetry() {
		using WidgetHost host = new( new CursesScreen( 20, 3 ), WidgetTheme.Default ); WidgetMessageDialog dialog = WidgetMessageDialog.OkCancel( "dialog", "D", "D" ); WidgetModalLease lease = host.ShowModal( dialog );
		for ( int i = 0; i < WidgetLimits.MaximumPendingActions; i++ ) host.Enqueue( new WidgetInvokedAction( "probe", i.ToString() ) );
		Assert.Throws<InvalidOperationException>( lease.Dispose );
		Assert.True( lease.IsActive ); Assert.True( dialog.IsOpen ); Assert.Equal( 1, host.ModalDepth );
		_ = host.DrainActions(); lease.Dispose(); Assert.False( lease.IsActive ); Assert.False( dialog.IsOpen ); Assert.Single( host.DrainActions() );
	}

	[Fact]
	public void ActionListPreflightsSelectionAndInvocationTogether() {
		WidgetActionList list = new( "actions" ); list.SetItems( [ new( "use", "Use" ) ] ); using WidgetHost host = new( new CursesScreen( 20, 3 ), WidgetTheme.Default ); host.Attach( list ); host.Arrange( host.Screen.Bounds ); host.Focus( list );
		for ( int i = 0; i < WidgetLimits.MaximumPendingActions - 1; i++ ) host.Enqueue( new WidgetInvokedAction( "probe", i.ToString() ) );
		Assert.Throws<InvalidOperationException>( () => host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ) );
		Assert.Empty( list.SelectedKeys ); Assert.Equal( WidgetLimits.MaximumPendingActions - 1, host.DrainActions().Count );
	}
}
