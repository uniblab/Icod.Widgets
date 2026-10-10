/* Copyright (C) 2026 Timothy J. Bruce. SPDX-License-Identifier: GPL-3.0-or-later */
namespace Icod.Widgets.Tests.Acceptance;
using Icod.DCurses;
using Xunit;

public sealed class WidgetConsumerTests {
	[Fact]
	public void DialogCancellationLeavesCallerModelUnchanged() { int model = 7; using WidgetHost host = new( new CursesScreen( 20, 5 ), WidgetTheme.Default ); host.ShowModal( WidgetMessageDialog.OkCancel( "dialog", "Edit", "Change?" ) ); WidgetDialogCompletedAction action = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) ); if ( WidgetDialogOutcome.Ok == action.Result.Outcome ) model = 8; Assert.Equal( 7, model ); }
	[Fact]
	public void InventoryActionReachesCallerOnce() { WidgetActionList list = new( "actions" ); list.SetItems( [ new( "use", "Use" ) ] ); using WidgetHost host = new( new CursesScreen( 20, 3 ), WidgetTheme.Default ); host.Attach( list ); host.Arrange( host.Screen.Bounds ); host.Focus( list ); Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions.OfType<WidgetInvokedAction>() ); Assert.Empty( host.DrainActions() ); }
}
