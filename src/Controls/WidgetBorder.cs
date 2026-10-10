/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using Icod.DCurses;

public sealed class WidgetBorder : WidgetContainer {
	public WidgetBorder( string id, Widget? child = null ) : base( id ) { if ( child is not null ) this.Add( child ); }
	protected override void OnArrange( CursesRectangle bounds ) {
		if ( 0 == this.Children.Count ) return;
		int rows = Math.Max( 0, bounds.Rows - 2 ); int columns = Math.Max( 0, bounds.Columns - 2 );
		this.Children[ 0 ].Arrange( new CursesRectangle( bounds.Row + Math.Min( 1, bounds.Rows ), bounds.Column + Math.Min( 1, bounds.Columns ), rows, columns ) );
	}
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		string line = new( '─', area.Columns ); screen.StandardWindow.Move( area.Row, area.Column ); screen.StandardWindow.Write( line, theme.Border );
		if ( 1 < area.Rows ) { screen.StandardWindow.Move( area.BottomExclusive - 1, area.Column ); screen.StandardWindow.Write( line, theme.Border ); }
	}
}

