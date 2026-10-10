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

public sealed class WidgetScrollIndicator( string id ) : Widget( id ) {
	public int Total { get; set; }
	public int Offset { get; set; }
	public int Visible { get; set; }
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		int percent = 0 >= this.Total ? 100 : Math.Clamp( ( this.Offset + this.Visible ) * 100 / this.Total, 0, 100 );
		string value = CursesText.TruncateToColumns( $"{percent}%", area.Columns, screen.TextWidthProvider );
		screen.StandardWindow.Move( area.Row, area.Column ); screen.StandardWindow.Write( value, theme.Normal );
	}
}
