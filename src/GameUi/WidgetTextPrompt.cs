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

/// <summary>Accepts bounded single-line text in a dialog.</summary>
public class WidgetTextPrompt : WidgetDialog {
	protected WidgetTextInput Editor { get; }
	public WidgetTextPrompt( string id, string title, string value = "" ) : base( id, title ) => this.Editor = new WidgetTextInput( id + ".input", value );
	public string Value => this.Editor.Value;
	public int MaxTextElements { get => this.Editor.MaxTextElements; set => this.Editor.MaxTextElements = value; }
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key == input.Kind && WidgetKey.Escape == input.KeyCode ) { this.Finish( host, WidgetDialogOutcome.Cancel ); return true; }
		if ( WidgetInputKind.Key == input.Kind && WidgetKey.Enter == input.KeyCode ) return this.Accept( host );
		return this.Editor.OnInput( input, host );
	}
	protected virtual bool Accept( WidgetHost host ) { this.Finish( host, new WidgetDialogResult( WidgetDialogOutcome.Accepted, this.Value ) ); return true; }
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) { CursesRectangle area = this.RenderFrame( screen, theme ); if ( area.IsEmpty ) return; this.Editor.Arrange( new CursesRectangle( area.Row, area.Column, 1, area.Columns ) ); this.Editor.OnRender( screen, theme, true ); }
}

