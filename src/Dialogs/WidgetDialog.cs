/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using Icod.DCurses;

/// <summary>Base for explicit-result modal or modeless dialogs.</summary>
public abstract class WidgetDialog( string id, string title ) : Widget( id ) {
	public override bool IsFocusable => true;
	public string Title { get; } = title ?? throw new ArgumentNullException( nameof( title ) );
	public bool IsOpen { get; internal set; }
	internal bool HasCompleted { get; set; }
	internal virtual void OnOpened() { }
	internal virtual void OnClosed() { }
	internal void Finish( WidgetHost host, WidgetDialogOutcome outcome ) => host.CompleteDialog( this, outcome );
	internal void Finish( WidgetHost host, WidgetDialogResult result ) => host.CompleteDialog( this, result );

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		string title = CursesText.TruncateToColumns( this.Title, area.Columns, screen.TextWidthProvider );
		screen.StandardWindow.Move( area.Row, area.Column ); screen.StandardWindow.Write( title, theme.Border );
	}
}

