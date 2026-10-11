/*
	Icod.Widgets
	Reusable terminal UI controls for .NET.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.Widgets;

using Icod.DCurses;

/// <summary>Arranges children in a fixed-column, equally divided grid.</summary>
public sealed class WidgetGrid : WidgetContainer {
	public WidgetGrid( string id, int columns ) : base( id ) {
		if ( 0 >= columns ) throw new ArgumentOutOfRangeException( nameof( columns ) );
		this.Columns = columns;
	}
	public int Columns { get; }
	protected override void OnArrange( CursesRectangle bounds ) {
		if ( 0 == this.Children.Count ) return;
		int rows = ( this.Children.Count + this.Columns - 1 ) / this.Columns;
		for ( int index = 0; index < this.Children.Count; index++ ) {
			int rowIndex = index / this.Columns;
			int columnIndex = index % this.Columns;
			int top = bounds.Row + bounds.Rows * rowIndex / rows;
			int bottom = bounds.Row + bounds.Rows * ( rowIndex + 1 ) / rows;
			int left = bounds.Column + bounds.Columns * columnIndex / this.Columns;
			int right = bounds.Column + bounds.Columns * ( columnIndex + 1 ) / this.Columns;
			this.Children[ index ].Arrange( new CursesRectangle( top, left, bottom - top, right - left ) );
		}
	}
}

