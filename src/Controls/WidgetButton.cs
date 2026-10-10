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

/// <summary>Emits a caller command after keyboard activation or a matching pointer release.</summary>
public sealed class WidgetButton( string id, string text, string commandId ) : Widget( id ) {
	private bool pointerPressed;
	public override bool IsFocusable => true;
	public string Text { get; set; } = text ?? throw new ArgumentNullException( nameof( text ) );
	public string CommandId { get; } = string.IsNullOrWhiteSpace( commandId ) ? throw new ArgumentException( "A command id is required.", nameof( commandId ) ) : commandId;

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key == input.Kind && input.KeyCode is WidgetKey.Enter or WidgetKey.Space ) {
			host.Enqueue( new WidgetInvokedAction( this.Id, this.CommandId ) ); return true;
		}
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer?.Button == CursesMouseButton.Primary ) {
			if ( CursesMouseAction.Press == input.Pointer.Action ) { this.pointerPressed = true; return true; }
			if ( CursesMouseAction.Release == input.Pointer.Action ) {
				bool invoke = this.pointerPressed && this.Bounds.Contains( input.Pointer.Row, input.Pointer.Column ); if ( invoke ) host.EnsureActionCapacity(); this.pointerPressed = false;
				if ( invoke ) host.Enqueue( new WidgetInvokedAction( this.Id, this.CommandId ) );
				return invoke;
			}
		}
		return false;
	}

	internal override void OnPointerCaptureCanceled() => this.pointerPressed = false;

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		string text = CursesText.TruncateToColumns( $"[ {this.Text} ]", area.Columns, screen.TextWidthProvider );
		screen.StandardWindow.Move( area.Row, area.Column );
		screen.StandardWindow.Write( text, !this.IsEffectivelyEnabled ? theme.Disabled : focused ? theme.Focused : theme.Normal );
	}
}

