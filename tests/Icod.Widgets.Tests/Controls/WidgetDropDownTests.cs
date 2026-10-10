/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Tests.Controls;

using Icod.DCurses;
using Xunit;

public sealed class WidgetDropDownTests {
	[Fact]
	public void CancelRestoresSelectionAndFocus() {
		WidgetDropDown drop = Create(); using WidgetHost host = Host( drop ); host.Focus( drop );
		host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.FromMilliseconds( 1 ) );
		host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( "a", drop.SelectedKey ); Assert.Same( drop, host.FocusedWidget ); Assert.False( drop.IsOpen );
	}

	[Fact]
	public void EnterCommitsOnce() {
		WidgetDropDown drop = Create(); using WidgetHost host = Host( drop ); host.Focus( drop );
		host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.FromMilliseconds( 1 ) );
		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( "b", drop.SelectedKey ); Assert.Single( result.Actions ); Assert.Empty( host.DrainActions() );
	}

	[Fact]
	public void OutsideDismissRestoresSelection() {
		WidgetDropDown drop = Create(); using WidgetHost host = Host( drop ); drop.Open();
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.Zero ); drop.Dismiss();
		Assert.Equal( "a", drop.SelectedKey ); Assert.False( drop.IsOpen );
	}

	[Fact]
	public void PopupChoosesAvailableAboveOrBelowSpace() {
		WidgetDropDown top = Create(); using WidgetHost host = Host( top, row: 0, screenRows: 5 ); top.Open();
		Assert.True( top.PopupBounds.Row >= top.Bounds.BottomExclusive );
		WidgetDropDown bottom = Create(); using WidgetHost host2 = Host( bottom, row: 4, screenRows: 5 ); bottom.Open();
		Assert.True( bottom.PopupBounds.BottomExclusive <= bottom.Bounds.Row );
	}

	[Fact]
	public void CompactPopupScrollsPreviewIntoView() {
		WidgetDropDown drop = Create(); using WidgetHost host = Host( drop, row: 0, screenRows: 2 ); host.Focus( drop );
		host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.FromMilliseconds( 1 ) );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.FromMilliseconds( 2 ) );
		host.Render();
		Assert.StartsWith( "Gamma", ReadRow( host.Screen, drop.PopupBounds.Row, drop.PopupBounds.Columns ) );
	}

	private static WidgetDropDown Create() => new( "drop", [ new( "a", "Alpha" ), new( "b", "Beta" ), new( "c", "Gamma" ) ], "a" );
	private static WidgetHost Host( WidgetDropDown drop, int row = 1, int screenRows = 5 ) { WidgetHost host = new( new CursesScreen( 30, screenRows ), WidgetTheme.Default ); host.Attach( drop ); drop.Arrange( new CursesRectangle( row, 0, 1, 20 ) ); return host; }
	private static string ReadRow( CursesScreen screen, int row, int columns ) => string.Concat( Enumerable.Range( 0, columns ).Select( column => screen.StandardWindow.GetCell( row, column ).Content ) );
}
