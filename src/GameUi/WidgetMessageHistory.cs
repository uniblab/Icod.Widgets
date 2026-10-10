/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using Icod.DCurses;

/// <summary>Keeps a bounded oldest-first terminal message history.</summary>
public sealed class WidgetMessageHistory : Widget {
	private readonly List<string> messages = [];
	public WidgetMessageHistory( string id, int capacity = 1000 ) : base( id ) { if ( capacity is < 1 or > WidgetLimits.MaximumItems ) throw new ArgumentOutOfRangeException( nameof( capacity ) ); this.Capacity = capacity; }
	public int Capacity { get; }
	public IReadOnlyList<string> Messages => this.messages.ToArray();
	public void Append( string message ) { ArgumentNullException.ThrowIfNull( message ); if ( this.messages.Count == this.Capacity ) this.messages.RemoveAt( 0 ); this.messages.Add( message ); }
	public void Clear() => this.messages.Clear();
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return; int start = Math.Max( 0, this.messages.Count - area.Rows );
		for ( int row = 0; row < area.Rows && start + row < this.messages.Count; row++ ) { screen.StandardWindow.Move( area.Row + row, area.Column ); screen.StandardWindow.Write( CursesText.TruncateToColumns( this.messages[ start + row ], area.Columns, screen.TextWidthProvider ), theme.Normal ); }
	}
}

