/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using Icod.DCurses;

/// <summary>Displays a message with explicit safe-default choices.</summary>
public sealed class WidgetMessageDialog : WidgetDialog {
	private readonly ( string Label, WidgetDialogOutcome Outcome )[] choices;
	private int selectedIndex;
	private WidgetMessageDialog( string id, string title, string message, ( string, WidgetDialogOutcome )[] choices, int defaultIndex ) : base( id, title ) {
		this.Message = message ?? throw new ArgumentNullException( nameof( message ) ); this.choices = choices; this.selectedIndex = defaultIndex;
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
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		for ( int row = 0; row < area.Rows; row++ ) WidgetBrowserView.WriteLine( screen, area, row, "", theme.Normal );
		WidgetBrowserView.WriteLine( screen, area, 0, this.Title, theme.Border );
		if ( area.Rows < 2 ) return;
		CursesTextLayout layout = CursesTextLayout.Create( this.Message, new CursesTextLayoutOptions( area.Columns ) { WrapMode = CursesTextWrapMode.Word, WidthProvider = screen.TextWidthProvider, DefaultStyle = theme.Normal } );
		screen.StandardWindow.PresentTextLayout( layout, 0, Math.Min( layout.Lines.Count, Math.Max( 0, area.Rows - 2 ) ), area.Row + 1, area.Column );
		string choices = string.Join( "  ", this.choices.Select( ( choice, index ) => index == this.selectedIndex ? $"[{choice.Label}]" : choice.Label ) );
		WidgetBrowserView.WriteLine( screen, area, area.Rows - 1, choices + " (Esc cancels)", theme.Focused );
	}
}

