/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Tests.Layout;

using Icod.DCurses;
using Xunit;

public sealed class WidgetLayoutTests {
	[Fact]
	public void VerticalStackSharesAvailableRows() {
		WidgetStack stack = new( "root", WidgetOrientation.Vertical );
		stack.Add( new WidgetLabel( "a", "Alpha" ) );
		stack.Add( new WidgetLabel( "b", "Beta" ) );

		stack.Arrange( new CursesRectangle( 1, 2, 5, 20 ) );

		Assert.Equal( new CursesRectangle( 1, 2, 3, 20 ), stack.Children[ 0 ].Bounds );
		Assert.Equal( new CursesRectangle( 4, 2, 2, 20 ), stack.Children[ 1 ].Bounds );
	}

	[Fact]
	public void ZeroSizeLayoutProducesEmptyChildBounds() {
		WidgetStack stack = new( "root", WidgetOrientation.Horizontal );
		stack.Add( new WidgetLabel( "a", "界" ) );

		stack.Arrange( new CursesRectangle( 0, 0, 0, 0 ) );

		Assert.True( stack.Children[ 0 ].Bounds.IsEmpty );
	}

	[Fact]
	public void ResizeReplacesChildGeometry() {
		WidgetStack stack = new( "root", WidgetOrientation.Horizontal );
		stack.Add( new WidgetLabel( "a", "A" ) );
		stack.Add( new WidgetLabel( "b", "B" ) );
		stack.Arrange( new CursesRectangle( 0, 0, 1, 10 ) );

		stack.Arrange( new CursesRectangle( 0, 0, 1, 3 ) );

		Assert.Equal( 2, stack.Children[ 0 ].Bounds.Columns );
		Assert.Equal( 1, stack.Children[ 1 ].Bounds.Columns );
	}
}
