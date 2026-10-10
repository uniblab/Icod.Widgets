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
	public static WidgetMessageDialog YesNo( string id, string title, string message ) => new( id, title, message, [ ( "Yes", WidgetDialogOutcome.Yes ), ( "No", WidgetDialogOutcome.No ) ], 1 );
	public static WidgetMessageDialog OkCancel( string id, string title, string message ) => new( id, title, message, [ ( "OK", WidgetDialogOutcome.Ok ), ( "Cancel", WidgetDialogOutcome.Cancel ) ], 1 );

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key != input.Kind ) return false;
		if ( WidgetKey.Escape == input.KeyCode ) { this.Finish( host, WidgetDialogOutcome.Cancel ); return true; }
		if ( input.KeyCode is WidgetKey.Left or WidgetKey.Up ) { this.selectedIndex = Math.Max( 0, this.selectedIndex - 1 ); return true; }
		if ( input.KeyCode is WidgetKey.Right or WidgetKey.Down ) { this.selectedIndex = Math.Min( this.choices.Length - 1, this.selectedIndex + 1 ); return true; }
		if ( WidgetKey.Enter == input.KeyCode ) { this.Finish( host, this.SelectedOutcome ); return true; }
		return false;
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		base.OnRender( screen, theme, focused ); CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.Rows < 2 || area.Columns == 0 ) return;
		string message = CursesText.TruncateToColumns( this.Message, area.Columns, screen.TextWidthProvider ); screen.StandardWindow.Move( area.Row + 1, area.Column ); screen.StandardWindow.Write( message, theme.Normal );
		if ( area.Rows < 3 ) return; string choices = string.Join( "  ", this.choices.Select( ( choice, index ) => index == this.selectedIndex ? $"[{choice.Label}]" : choice.Label ) );
		screen.StandardWindow.Move( area.Row + 2, area.Column ); screen.StandardWindow.Write( CursesText.TruncateToColumns( choices, area.Columns, screen.TextWidthProvider ), theme.Focused );
	}
}

