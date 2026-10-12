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

/// <summary>Renders one clipped line of display text.</summary>
public sealed class WidgetLabel( string id, string text ) : Widget( id ) {
	public string Text { get; set; } = text ?? throw new ArgumentNullException( nameof( text ) );
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle clipped = this.Bounds.Intersect( screen.Bounds );
		if ( clipped.IsEmpty ) return;
		string value = CursesText.SliceByColumns( this.Text, clipped.Column - this.Bounds.Column, clipped.Columns, screen.TextWidthProvider );
		screen.StandardWindow.FillRectangle( clipped.Row, clipped.Column, clipped.Rows, clipped.Columns, CursesCell.Blank( this.Style( theme, WidgetStyleRole.Surface ) ) );
		screen.StandardWindow.Move( clipped.Row, clipped.Column );
		screen.StandardWindow.Write( value, this.Style( theme, this.IsEffectivelyEnabled ? WidgetStyleRole.Text : WidgetStyleRole.Disabled ) );
	}
}
