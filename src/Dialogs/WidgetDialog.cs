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
	internal CursesRectangle GetContentBounds( CursesScreen screen ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds );
		return area.Rows < 3 || area.Columns < 3
			? new CursesRectangle( area.Row + Math.Min( 1, area.Rows ), area.Column, Math.Max( 0, area.Rows - 1 ), area.Columns )
			: new CursesRectangle( area.Row + 1, area.Column + 1, area.Rows - 2, area.Columns - 2 );
	}

	internal CursesRectangle RenderFrame( CursesScreen screen, WidgetTheme theme, string? caption = null ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return default;
		for ( int row = 0; row < area.Rows; row++ ) WidgetBrowserView.WriteLine( screen, area, row, string.Empty, theme.Normal );
		string title = WidgetBrowserView.Sanitize( caption ?? this.Title );
		if ( area.Rows < 3 || area.Columns < 3 ) {
			WidgetBrowserView.WriteLine( screen, area, 0, title, theme.Border );
			return this.GetContentBounds( screen );
		}
		bool useBoxDrawing = 6 == CursesText.MeasureColumns( "┌─┐│└┘", screen.TextWidthProvider );
		char topLeft = useBoxDrawing ? '┌' : '+'; char topRight = useBoxDrawing ? '┐' : '+';
		char bottomLeft = useBoxDrawing ? '└' : '+'; char bottomRight = useBoxDrawing ? '┘' : '+';
		char horizontal = useBoxDrawing ? '─' : '-'; char vertical = useBoxDrawing ? '│' : '|';
		int maximumTitleColumns = Math.Max( 0, area.Columns - 5 );
		string clippedTitle = CursesText.TruncateToColumns( title, maximumTitleColumns, screen.TextWidthProvider );
		int titleColumns = CursesText.MeasureColumns( clippedTitle, screen.TextWidthProvider );
		string top = 0 == titleColumns ? topLeft + new string( horizontal, area.Columns - 2 ) + topRight :
			topLeft + new string( horizontal, 1 ) + " " + clippedTitle + " " + new string( horizontal, area.Columns - titleColumns - 5 ) + topRight;
		screen.StandardWindow.Move( area.Row, area.Column ); screen.StandardWindow.Write( top, theme.Border );
		int right = area.Column + area.Columns - 1;
		for ( int row = area.Row + 1; row < area.BottomExclusive - 1; row++ ) { screen.StandardWindow.Move( row, area.Column ); screen.StandardWindow.Write( vertical.ToString(), theme.Border ); screen.StandardWindow.Move( row, right ); screen.StandardWindow.Write( vertical.ToString(), theme.Border ); }
		screen.StandardWindow.Move( area.BottomExclusive - 1, area.Column ); screen.StandardWindow.Write( bottomLeft + new string( horizontal, area.Columns - 2 ) + bottomRight, theme.Border );
		return this.GetContentBounds( screen );
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) => _ = this.RenderFrame( screen, theme );
}

