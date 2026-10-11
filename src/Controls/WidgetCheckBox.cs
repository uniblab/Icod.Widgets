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

public sealed class WidgetCheckBox( string id, string text, bool isChecked = false ) : Widget( id ) {
	public override bool IsFocusable => true;
	public string Text { get; set; } = text ?? throw new ArgumentNullException( nameof( text ) );
	public bool IsChecked { get; private set; } = isChecked;
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key != input.Kind || input.KeyCode is not ( WidgetKey.Space or WidgetKey.Enter ) ) return false;
		host.Enqueue( new WidgetValueChangedAction<bool>( this.Id, !this.IsChecked ) );
		this.IsChecked = !this.IsChecked;
		return true;
	}
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		string value = CursesText.TruncateToColumns( $"[{( this.IsChecked ? 'x' : ' ' )}] {this.Text}", area.Columns, screen.TextWidthProvider );
		screen.StandardWindow.Move( area.Row, area.Column );
		screen.StandardWindow.Write( value, !this.IsEffectivelyEnabled ? theme.Disabled : focused ? theme.Focused : theme.Normal );
	}
}

