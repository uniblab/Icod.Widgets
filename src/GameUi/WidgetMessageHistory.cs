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

