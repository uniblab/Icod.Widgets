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
