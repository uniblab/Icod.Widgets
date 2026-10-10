/* Copyright (C) 2026 Timothy J. Bruce. SPDX-License-Identifier: GPL-3.0-or-later */
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
}
