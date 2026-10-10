/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
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
