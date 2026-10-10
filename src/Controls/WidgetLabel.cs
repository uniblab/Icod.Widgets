/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
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
		screen.StandardWindow.Move( clipped.Row, clipped.Column );
		screen.StandardWindow.Write( value, this.IsEnabled ? theme.Normal : theme.Disabled );
	}
}
