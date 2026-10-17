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

internal readonly record struct WidgetEditorViewport( CursesRectangle Gutter, CursesRectangle Text ) {
	internal int GutterColumns => this.Gutter.Columns;

	internal static WidgetEditorViewport Create( CursesRectangle area, long lineCount, bool showLineNumbers ) {
		if ( !showLineNumbers || area.IsEmpty ) return new WidgetEditorViewport( default, area );
		int digits = 1;
		for ( long value = Math.Max( 1, lineCount ); value >= 10; value /= 10 ) digits++;
		int gutterColumns = checked( digits + 1 );
		if ( area.Columns <= gutterColumns ) return new WidgetEditorViewport( default, area );
		return new WidgetEditorViewport(
			new CursesRectangle( area.Row, area.Column, area.Rows, gutterColumns ),
			new CursesRectangle( area.Row, area.Column + gutterColumns, area.Rows, area.Columns - gutterColumns ) );
	}
}
