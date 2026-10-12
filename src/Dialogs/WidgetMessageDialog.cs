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

/// <summary>Displays a message with explicit safe-default choices.</summary>
public sealed class WidgetMessageDialog : WidgetDialog {
	private readonly ( string Label, WidgetDialogOutcome Outcome )[] choices;
	private readonly int defaultIndex;
	private int selectedIndex;
	private WidgetMessageDialog( string id, string title, string message, ( string, WidgetDialogOutcome )[] choices, int defaultIndex ) : base( id, title ) {
		this.Message = message ?? throw new ArgumentNullException( nameof( message ) ); this.choices = choices; this.selectedIndex = this.defaultIndex = defaultIndex;
	}
	public string Message { get; }
	public WidgetDialogOutcome SelectedOutcome => this.choices[ this.selectedIndex ].Outcome;
	public static WidgetMessageDialog Ok( string id, string title, string message ) => new( id, title, message, [ ( "OK", WidgetDialogOutcome.Ok ) ], 0 );
	public static WidgetMessageDialog YesNo( string id, string title, string message ) => new( id, title, message, [ ( "Yes", WidgetDialogOutcome.Yes ), ( "No", WidgetDialogOutcome.No ) ], 1 );
	public static WidgetMessageDialog OkCancel( string id, string title, string message ) => new( id, title, message, [ ( "OK", WidgetDialogOutcome.Ok ), ( "Cancel", WidgetDialogOutcome.Cancel ) ], 1 );

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key != input.Kind ) return false;
		if ( WidgetKey.Escape == input.KeyCode ) { this.Finish( host, WidgetDialogOutcome.Cancel ); return true; }
		if ( input.KeyCode is WidgetKey.Left or WidgetKey.Up ) { this.selectedIndex = Math.Max( 0, this.selectedIndex - 1 ); return true; }
		if ( input.KeyCode == WidgetKey.Tab ) { this.selectedIndex = ( this.selectedIndex + 1 ) % this.choices.Length; return true; }
		if ( input.KeyCode is WidgetKey.Right or WidgetKey.Down ) { this.selectedIndex = Math.Min( this.choices.Length - 1, this.selectedIndex + 1 ); return true; }
		if ( WidgetKey.Enter == input.KeyCode ) { this.Finish( host, this.SelectedOutcome ); return true; }
		return false;
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.RenderFrame( screen, theme ); if ( area.IsEmpty ) return;
		CursesTextLayout layout = CursesTextLayout.Create( this.Message, new CursesTextLayoutOptions( area.Columns ) { WrapMode = CursesTextWrapMode.Word, WidthProvider = screen.TextWidthProvider, DefaultStyle = this.Style( theme, WidgetStyleRole.Text ) } );
		screen.StandardWindow.PresentTextLayout( layout, 0, Math.Min( layout.Lines.Count, Math.Max( 0, area.Rows - 1 ) ), area.Row, area.Column );
		string[] choices = this.choices.Select( ( choice, index ) => index == this.selectedIndex ? $"[{choice.Label}]" : choice.Label ).ToArray();
		WidgetBrowserView.WriteLine( screen, area, area.Rows - 1, string.Join( "  ", choices ) + " (Esc cancels)", this.Style( theme, WidgetStyleRole.DialogAction ) );
		int column = area.Column;
		for ( int index = 0; index < choices.Length && column < area.Column + area.Columns; index++ ) {
			string label = WidgetBrowserView.Sanitize( choices[ index ] );
			CursesStyle style = this.Style( theme, index == this.selectedIndex && focused ? WidgetStyleRole.DialogFocusedAction :
				index == this.defaultIndex ? WidgetStyleRole.DialogDefaultAction : WidgetStyleRole.DialogAction );
			screen.StandardWindow.Move( area.BottomExclusive - 1, column );
			screen.StandardWindow.Write( CursesText.TruncateToColumns( label, area.Column + area.Columns - column, screen.TextWidthProvider ), style );
			column += CursesText.MeasureColumns( label, screen.TextWidthProvider ) + 2;
		}
	}
}

