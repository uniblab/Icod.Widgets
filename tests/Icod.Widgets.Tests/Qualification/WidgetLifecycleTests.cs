/* Copyright (C) 2026 Timothy J. Bruce. SPDX-License-Identifier: GPL-3.0-or-later */
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
}
