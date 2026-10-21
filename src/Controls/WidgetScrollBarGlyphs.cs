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

internal readonly record struct WidgetScrollBarGlyphs( string Decrement, string Track, string Thumb, string Increment ) {
	internal static WidgetScrollBarGlyphs Create( WidgetScrollOrientation orientation, ICursesTextWidthProvider widthProvider ) {
		ArgumentNullException.ThrowIfNull( widthProvider );
		WidgetScrollBarGlyphs preferred = WidgetScrollOrientation.Vertical == orientation
			? new WidgetScrollBarGlyphs( "▲", "│", "┃", "▼" ) : new WidgetScrollBarGlyphs( "◄", "─", "━", "►" );
		if ( 1 == widthProvider.GetWidth( preferred.Decrement ) && 1 == widthProvider.GetWidth( preferred.Track ) &&
			1 == widthProvider.GetWidth( preferred.Thumb ) && 1 == widthProvider.GetWidth( preferred.Increment ) ) return preferred;
		return WidgetScrollOrientation.Vertical == orientation
			? new WidgetScrollBarGlyphs( "^", "|", "#", "v" ) : new WidgetScrollBarGlyphs( "<", "-", "=", ">" );
	}
}
