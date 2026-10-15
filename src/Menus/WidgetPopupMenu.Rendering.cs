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

public partial class WidgetPopupMenu {
	private int firstHeading;

	internal CursesRectangle BarBounds( CursesScreen screen ) => new CursesRectangle( this.Bounds.Row, this.Bounds.Column, Math.Min( 1, this.Bounds.Rows ), this.Bounds.Columns ).Intersect( screen.Bounds );

	private IEnumerable<( int Index, CursesRectangle Area )> HeadingAreas( CursesScreen screen ) {
		CursesRectangle bar = this.BarBounds( screen ); if ( bar.IsEmpty ) yield break;
		int Width( int index ) => Math.Min( bar.Columns, CursesText.MeasureColumns( Label( this.rootItems[ index ] ), screen.TextWidthProvider ) + 2 );
		int selected = this.IsOpen && this.levels.Count > 0 ? this.levels[ 0 ].Cursor : -1;
		if ( selected >= 0 ) {
			if ( selected < this.firstHeading ) this.firstHeading = selected;
			int used = 0;
			for ( int index = selected; index >= this.firstHeading; index-- ) {
				if ( this.rootItems[ index ].Kind == WidgetMenuItemKind.Separator ) continue;
				used += Width( index );
				if ( used > bar.Columns ) { this.firstHeading = index + 1; break; }
			}
		}
		int column = bar.Column;
		for ( int index = this.firstHeading; index < this.rootItems.Length && column < bar.Column + bar.Columns; index++ ) {
			if ( this.rootItems[ index ].Kind == WidgetMenuItemKind.Separator ) continue;
			int width = Math.Min( Width( index ), bar.Column + bar.Columns - column );
			yield return ( index, new( bar.Row, column, 1, width ) ); column += width;
		}
	}

	private int HitHeading( int row, int column, CursesScreen screen ) {
		foreach ( var heading in this.HeadingAreas( screen ) ) if ( heading.Area.Contains( row, column ) ) return heading.Index;
		return -1;
	}

	private int FirstPopupLevel => this.IsMenuBar ? 1 : 0;
	internal CursesRectangle GetPopupBounds( CursesScreen screen ) => this.GetPopupLayers( screen ).FirstOrDefault().Bounds;

	internal IReadOnlyList<( int Level, CursesRectangle Bounds )> GetPopupLayers( CursesScreen screen ) {
		List<( int Level, CursesRectangle Bounds )> result = [];
		if ( !this.IsOpen || this.levels.Count == 0 || screen.Bounds.IsEmpty ) return result;
		CursesRectangle bar = this.BarBounds( screen );
		if ( this.IsMenuBar && bar.IsEmpty ) return result;
		if ( this.IsMenuBar && this.levels.Count == 1 ) { result.Add( ( 0, bar ) ); return result; }
		for ( int depth = this.FirstPopupLevel; depth < this.levels.Count; depth++ ) {
			Level level = this.levels[ depth ]; CursesRectangle bounds;
			if ( depth == 0 && !this.IsMenuBar ) {
				CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); int inset = Inset( area );
				bounds = new( area.Row, area.Column, Math.Min( area.Rows, level.Items.Count + inset * 2 ), area.Columns );
			} else {
				int labelWidth = level.Items.Max( item => CursesText.MeasureColumns( Prefix( item ) + Label( item ) + Suffix( item ), screen.TextWidthProvider ) );
				int shortcutWidth = level.Items.Max( item => item.Shortcut is { } shortcut ? CursesText.MeasureColumns( FormatShortcut( shortcut ), screen.TextWidthProvider ) : 0 );
				int width = Math.Min( screen.Columns, labelWidth + ( shortcutWidth > 0 ? shortcutWidth + 2 : 0 ) + 4 );
				int row, column;
				if ( result.Count == 0 ) {
					int anchor = this.HeadingAreas( screen ).FirstOrDefault( heading => heading.Index == this.levels[ 0 ].Cursor ).Area.Column;
					column = Math.Clamp( anchor, 0, screen.Columns - width ); row = Math.Min( bar.Row + 1, screen.Rows - 1 );
				} else {
					CursesRectangle parent = result[ ^1 ].Bounds; Level parentLevel = this.levels[ depth - 1 ];
					int inset = Inset( parent ); int rows = Math.Max( 0, parent.Rows - inset * 2 );
					bool fitsRight = parent.Column + parent.Columns + width <= screen.Columns;
					column = fitsRight ? parent.Column + parent.Columns : Math.Min( parent.Column, screen.Columns - width );
					row = fitsRight ? parent.Row + inset + parentLevel.Cursor - FirstVisibleItem( parentLevel, rows ) : parent.Row;
					row = Math.Clamp( row, 0, Math.Max( 0, screen.Rows - Math.Min( screen.Rows, level.Items.Count + 2 ) ) );
				}
				bounds = new( row, column, Math.Min( screen.Rows - row, level.Items.Count + 2 ), width );
			}
			result.Add( ( depth, bounds ) );
		}
		return result;
	}

	internal bool ContainsPopupPoint( int row, int column, CursesScreen screen ) =>
		this.GetPopupLayers( screen ).Any( layer => layer.Bounds.Contains( row, column ) );
	private static int Inset( CursesRectangle bounds ) => bounds.Rows >= 3 && bounds.Columns >= 3 ? 1 : 0;
	private static int FirstVisibleItem( Level level, int rows ) => rows > 0 ? Math.Clamp( level.Cursor - rows + 1, 0, Math.Max( 0, level.Items.Count - rows ) ) : 0;
	private ( int Level, int Index ) HitItem( int row, int column, CursesScreen screen ) {
		if ( this.IsMenuBar && this.levels.Count == 1 ) return ( -1, -1 );
		foreach ( var layer in this.GetPopupLayers( screen ).Reverse() ) {
			CursesRectangle area = layer.Bounds;
			if ( !area.Contains( row, column ) ) continue;
			int inset = Inset( area ); Level level = this.levels[ layer.Level ]; int rows = Math.Min( level.Items.Count, area.Rows - 2 * inset );
			int relative = row - area.Row - inset;
			// A child border occludes parent rows too.
			return relative >= 0 && relative < rows && column >= area.Column + inset && column < area.Column + area.Columns - inset
				? ( layer.Level, FirstVisibleItem( level, rows ) + relative ) : ( -1, -1 );
		}
		return ( -1, -1 );
	}

	internal void RenderBar( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.BarBounds( screen ); if ( area.IsEmpty ) return;
		screen.StandardWindow.FillRectangle( area.Row, area.Column, 1, area.Columns, CursesCell.Blank( this.Style( theme, WidgetStyleRole.MenuBarSurface ) ) );
		foreach ( var heading in this.HeadingAreas( screen ) ) {
			WidgetMenuItem item = this.rootItems[ heading.Index ];
			bool active = this.IsOpen && this.levels[ 0 ].Cursor == heading.Index;
			bool highlighted = theme.HighlightMenuHover && heading.Index == this.hoveredHeading;
			active |= focused && !this.IsOpen && heading.Index == this.firstHeading;
			CursesStyle style = this.Style( theme, !item.IsEnabled || !this.IsEffectivelyEnabled ? WidgetStyleRole.MenuBarDisabled : active ? WidgetStyleRole.MenuBarActive : highlighted ? WidgetStyleRole.MenuBarHover : WidgetStyleRole.MenuBarItem );
			screen.StandardWindow.FillRectangle( area.Row, heading.Area.Column, 1, heading.Area.Columns, CursesCell.Blank( style ) );
			int padding = heading.Area.Columns > 1 ? 1 : 0;
			string label = CursesText.TruncateToColumns( Label( item ), Math.Max( 0, heading.Area.Columns - padding * 2 ), screen.TextWidthProvider );
			Write( screen, area.Row, heading.Area.Column + padding, label, style );
			if ( item.IsEnabled && this.IsEffectivelyEnabled ) RenderMnemonic( screen, area.Row, heading.Area.Column + padding, label, item.Mnemonic, this.MnemonicStyle( theme, active ? WidgetStyleRole.MenuBarActiveMnemonic : WidgetStyleRole.MenuBarMnemonic, style, !highlighted || active ) );
		}
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		if ( this.IsMenuBar ) this.RenderBar( screen, theme, focused );
		if ( !this.IsOpen || this.levels.Count == 0 ) {
			this.RenderBounds = default;
			if ( !this.IsMenuBar ) WidgetBrowserView.WriteLine( screen, this.Bounds.Intersect( screen.Bounds ), 0, string.Join( " | ", this.rootItems.Where( item => item.Kind != WidgetMenuItemKind.Separator ).Select( Label ) ), this.Style( theme, focused ? WidgetStyleRole.PopupFocused : WidgetStyleRole.PopupItem ) );
			return;
		}
		this.RenderBounds = this.GetPopupBounds( screen );
		if ( this.RenderBounds.IsEmpty || this.IsMenuBar && this.levels.Count == 1 ) return;
		foreach ( var layer in this.GetPopupLayers( screen ) ) this.RenderPopupLevel( screen, theme, focused, layer.Level, layer.Bounds );
	}

	internal void RenderPopupLevel( CursesScreen screen, WidgetTheme theme, bool focused, int depth, CursesRectangle bounds ) {
		this.RenderBounds = this.GetPopupBounds( screen );
		if ( bounds.IsEmpty || this.IsMenuBar && depth == 0 ) return;
		Level level = this.levels[ depth ];
		int inset = Inset( bounds ); int columns = bounds.Columns - inset * 2;
		int rows = Math.Min( level.Items.Count, bounds.Rows - inset * 2 );
		int padding = this.IsMenuBar && columns >= 3 ? 1 : 0;
		screen.StandardWindow.FillRectangle( bounds.Row, bounds.Column, bounds.Rows, bounds.Columns, CursesCell.Blank( this.Style( theme, WidgetStyleRole.PopupSurface ) ) );
		if ( inset > 0 ) this.RenderPopupBorder( screen, theme, bounds );
		for ( int row = 0; row < rows; row++ ) {
			int index = FirstVisibleItem( level, rows ) + row; WidgetMenuItem item = level.Items[ index ];
			bool selected = index == level.Cursor && focused;
			bool highlighted = theme.HighlightMenuHover && depth == this.hoveredLevel && index == this.hoveredIndex;
			CursesStyle style = this.Style( theme, !item.IsEnabled ? WidgetStyleRole.PopupDisabled : selected ? WidgetStyleRole.PopupFocused : highlighted ? WidgetStyleRole.PopupHover : WidgetStyleRole.PopupItem );
			int line = bounds.Row + row + inset; int column = bounds.Column + inset;
			screen.StandardWindow.FillRectangle( line, column, 1, columns, CursesCell.Blank( style ) );
			if ( item.Kind == WidgetMenuItemKind.Separator ) { Write( screen, line, column, new string( CursesText.MeasureColumns( "─", screen.TextWidthProvider ) == 1 ? '─' : '-', columns ), this.Style( theme, WidgetStyleRole.PopupBorder ) ); continue; }
			string prefix = Prefix( item );
			string value = ComposeRow( item, prefix + Label( item ) + Suffix( item ), columns - padding * 2, screen );
			Write( screen, line, column + padding, value, style );
			// Selected command styles cover the shortcut and check mark as well as the label.
			if ( !selected && !highlighted && item.IsEnabled && item.Shortcut is { } shortcut && this.HasPartStyle( theme, WidgetStyleRole.PopupShortcut ) ) {
				string gesture = FormatShortcut( shortcut ); int width = CursesText.MeasureColumns( gesture, screen.TextWidthProvider );
				if ( columns - padding * 2 >= width + 2 ) Write( screen, line, column + columns - padding - width, gesture, this.Style( theme, WidgetStyleRole.PopupShortcut ) );
			}
			if ( !selected && !highlighted && item.IsEnabled && prefix.Length > 0 && columns - padding * 2 >= 4 && this.HasPartStyle( theme, WidgetStyleRole.PopupCheck ) ) Write( screen, line, column + padding, prefix, this.Style( theme, WidgetStyleRole.PopupCheck ) );
			if ( item.IsEnabled ) {
				int available = columns - padding * 2 - prefix.Length;
				if ( item.Shortcut is { } gesture ) available -= CursesText.MeasureColumns( FormatShortcut( gesture ), screen.TextWidthProvider ) + 2;
				string label = CursesText.TruncateToColumns( Label( item ), Math.Max( 0, available ), screen.TextWidthProvider );
				RenderMnemonic( screen, line, column + padding + prefix.Length, label, item.Mnemonic, this.MnemonicStyle( theme, selected ? WidgetStyleRole.PopupFocusedMnemonic : WidgetStyleRole.PopupMnemonic, style, !highlighted || selected ) );
			}
		}
	}

	private static string Label( WidgetMenuItem item ) => WidgetBrowserView.Sanitize( item.Text );
	private static string Prefix( WidgetMenuItem item ) => item.Kind == WidgetMenuItemKind.CheckableCommand ? item.IsChecked ? "[x] " : "[ ] " : string.Empty;
	private static string Suffix( WidgetMenuItem item ) => item.Kind == WidgetMenuItemKind.Submenu ? " >" : string.Empty;
	private static void Write( CursesScreen screen, int row, int column, string text, CursesStyle style ) {
		if ( text.Length == 0 ) return;
		screen.StandardWindow.Move( row, column ); screen.StandardWindow.Write( text, style );
	}
	private CursesStyle MnemonicStyle( WidgetTheme theme, WidgetStyleRole role, CursesStyle rowStyle, bool usePart ) =>
		usePart && this.HasPartStyle( theme, role ) ? this.Style( theme, role ) : rowStyle.WithAttributes( rowStyle.Attributes | CursesTextAttributes.Underline );
	private static void RenderMnemonic( CursesScreen screen, int row, int column, string label, char? mnemonic, CursesStyle style ) {
		if ( mnemonic is not { } key ) return;
		var elements = System.Globalization.StringInfo.GetTextElementEnumerator( label ); int offset = 0;
		while ( elements.MoveNext() ) {
			string element = elements.GetTextElement();
			if ( char.ToUpperInvariant( element[ 0 ] ) == char.ToUpperInvariant( key ) ) { Write( screen, row, column + offset, element, style ); return; }
			offset += CursesText.MeasureColumns( element, screen.TextWidthProvider );
		}
	}
	private void RenderPopupBorder( CursesScreen screen, WidgetTheme theme, CursesRectangle bounds ) {
		int right = bounds.Column + bounds.Columns - 1; int bottom = bounds.BottomExclusive - 1;
		bool box = CursesText.MeasureColumns( "┌─┐│└┘", screen.TextWidthProvider ) == 6;
		string horizontal = new( box ? '─' : '-', bounds.Columns - 2 ); CursesStyle style = this.Style( theme, WidgetStyleRole.PopupBorder );
		Write( screen, bounds.Row, bounds.Column, ( box ? "┌" : "+" ) + horizontal + ( box ? "┐" : "+" ), style );
		for ( int row = bounds.Row + 1; row < bottom; row++ ) { Write( screen, row, bounds.Column, box ? "│" : "|", style ); Write( screen, row, right, box ? "│" : "|", style ); }
		Write( screen, bottom, bounds.Column, ( box ? "└" : "+" ) + horizontal + ( box ? "┘" : "+" ), style );
	}
}
