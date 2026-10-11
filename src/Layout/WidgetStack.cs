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

