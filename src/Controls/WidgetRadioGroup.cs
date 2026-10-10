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

public sealed class WidgetRadioGroup : Widget {
	private readonly string[] keys;
	private int selectedIndex;
	public WidgetRadioGroup( string id, IReadOnlyList<string> keys, string? selectedKey = null ) : base( id ) {
		ArgumentNullException.ThrowIfNull( keys );
		if ( 0 == keys.Count || keys.Count > WidgetLimits.MaximumItems ) throw new ArgumentOutOfRangeException( nameof( keys ) );
		this.keys = keys.ToArray();
		if ( this.keys.Any( string.IsNullOrWhiteSpace ) || this.keys.Distinct( StringComparer.Ordinal ).Count() != this.keys.Length ) throw new ArgumentException( "Radio keys must be nonempty and unique.", nameof( keys ) );
		this.selectedIndex = selectedKey is null ? 0 : Array.IndexOf( this.keys, selectedKey );
		if ( 0 > this.selectedIndex ) throw new ArgumentException( "The selected key is not present.", nameof( selectedKey ) );
	}
	public override bool IsFocusable => true;
	public IReadOnlyList<string> Keys => this.keys;
	public string SelectedKey => this.keys[ this.selectedIndex ];
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key != input.Kind ) return false;
		int next = input.KeyCode switch {
			WidgetKey.Up or WidgetKey.Left => Math.Max( 0, this.selectedIndex - 1 ),
			WidgetKey.Down or WidgetKey.Right => Math.Min( this.keys.Length - 1, this.selectedIndex + 1 ),
			WidgetKey.Home => 0,
			WidgetKey.End => this.keys.Length - 1,
			_ => this.selectedIndex
		};
		if ( next == this.selectedIndex ) return false;
		host.Enqueue( new WidgetValueChangedAction<string>( this.Id, this.keys[ next ] ) );
		this.selectedIndex = next;
		return true;
	}
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		string value = CursesText.TruncateToColumns( $"(*) {this.SelectedKey}", area.Columns, screen.TextWidthProvider );
		screen.StandardWindow.Move( area.Row, area.Column ); screen.StandardWindow.Write( value, focused ? theme.Focused : theme.Normal );
	}
}

