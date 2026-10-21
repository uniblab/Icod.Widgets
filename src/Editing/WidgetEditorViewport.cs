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

internal readonly record struct WidgetEditorViewport(
	CursesRectangle Gutter,
	CursesRectangle Text,
	CursesRectangle VerticalScrollBar,
	CursesRectangle HorizontalScrollBar,
	CursesRectangle ScrollCorner ) {
	internal int GutterColumns => this.Gutter.Columns;

	internal static WidgetEditorViewport Create(
		CursesRectangle area,
		long lineCount,
		bool showLineNumbers,
		bool showVerticalScrollBar,
		bool showHorizontalScrollBar = false ) {
		if ( area.IsEmpty ) return new WidgetEditorViewport( default, area, default, default, default );
		CursesRectangle gutter = default; CursesRectangle text = area;
		if ( showLineNumbers ) {
			int digits = 1;
			for ( long value = Math.Max( 1, lineCount ); value >= 10; value /= 10 ) digits++;
			int gutterColumns = checked( digits + 1 );
			if ( area.Columns > gutterColumns ) {
				gutter = new CursesRectangle( area.Row, area.Column, area.Rows, gutterColumns );
				text = new CursesRectangle( area.Row, area.Column + gutterColumns, area.Rows, area.Columns - gutterColumns );
			}
		}
		bool horizontal = showHorizontalScrollBar && 2 <= text.Rows && 2 <= text.Columns;
		int contentRows = text.Rows - ( horizontal ? 1 : 0 );
		bool vertical = showVerticalScrollBar && 2 <= contentRows && 2 <= text.Columns;
		int contentColumns = text.Columns - ( vertical ? 1 : 0 );
		if ( !gutter.IsEmpty ) gutter = new CursesRectangle( gutter.Row, gutter.Column, contentRows, gutter.Columns );
		CursesRectangle verticalScrollBar = vertical
			? new CursesRectangle( text.Row, text.Column + contentColumns, contentRows, 1 ) : default;
		CursesRectangle horizontalScrollBar = horizontal
			? new CursesRectangle( text.Row + contentRows, text.Column, 1, contentColumns ) : default;
		CursesRectangle corner = vertical && horizontal
			? new CursesRectangle( text.Row + contentRows, text.Column + contentColumns, 1, 1 ) : default;
		text = new CursesRectangle( text.Row, text.Column, contentRows, contentColumns );
		return new WidgetEditorViewport( gutter, text, verticalScrollBar, horizontalScrollBar, corner );
	}
}
