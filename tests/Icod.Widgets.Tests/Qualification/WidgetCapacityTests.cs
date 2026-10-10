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
}
