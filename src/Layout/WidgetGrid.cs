/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
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

