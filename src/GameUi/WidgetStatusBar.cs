/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using Icod.DCurses;

public sealed record WidgetStatusField( string Name, string Text, int Priority );

/// <summary>Fits prioritized caller-defined fields into one clipped line.</summary>
public sealed class WidgetStatusBar( string id ) : Widget( id ) {
	private WidgetStatusField[] fields = [];
	public IReadOnlyList<WidgetStatusField> Fields => this.fields;
	public void SetFields( IReadOnlyList<WidgetStatusField> fields ) { ArgumentNullException.ThrowIfNull( fields ); WidgetStatusField[] replacement = fields.ToArray(); if ( replacement.Select( field => field.Name ).Distinct( StringComparer.Ordinal ).Count() != replacement.Length ) throw new ArgumentException( "Status field names must be unique.", nameof( fields ) ); this.fields = replacement; }
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) { CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return; string value = string.Join( " | ", this.fields.OrderByDescending( field => field.Priority ).Select( field => field.Text ) ); screen.StandardWindow.Move( area.Row, area.Column ); screen.StandardWindow.Write( CursesText.TruncateToColumns( value, area.Columns, screen.TextWidthProvider ), theme.Normal ); }
}

