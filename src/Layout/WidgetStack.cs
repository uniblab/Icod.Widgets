/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/

namespace Icod.Widgets;

using Icod.DCurses;

public enum WidgetOrientation { Horizontal, Vertical }

/// <summary>Shares one axis equally among ordered children.</summary>
public sealed class WidgetStack( string id, WidgetOrientation orientation ) : WidgetContainer( id ) {
	public WidgetOrientation Orientation { get; } = Enum.IsDefined( orientation ) ? orientation : throw new ArgumentOutOfRangeException( nameof( orientation ) );

	protected override void OnArrange( CursesRectangle bounds ) {
		if ( 0 == this.Children.Count ) return;
		int available = WidgetOrientation.Vertical == this.Orientation ? bounds.Rows : bounds.Columns;
		int baseSize = available / this.Children.Count;
		int remainder = available % this.Children.Count;
		int offset = 0;
		for ( int index = 0; index < this.Children.Count; index++ ) {
			int size = baseSize + ( index < remainder ? 1 : 0 );
			CursesRectangle childBounds = WidgetOrientation.Vertical == this.Orientation
				? new CursesRectangle( bounds.Row + offset, bounds.Column, size, bounds.Columns )
				: new CursesRectangle( bounds.Row, bounds.Column + offset, bounds.Rows, size );
			this.Children[ index ].Arrange( childBounds );
			offset += size;
		}
	}
}

