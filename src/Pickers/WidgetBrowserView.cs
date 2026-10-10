/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;
using Icod.DCurses;

// Shared presentation; provider work remains in the application loop.
internal sealed class WidgetBrowserView {
	private WidgetFileSystemEntry[] entries = [];
	private int cursor;
	private int offset;
	private WidgetTextInput? pathEditor;
	private bool roots;
	private bool basket;
	private int visibleRows = 1;
	private CursesRectangle listingBounds;
	private string? pressedPath;
	internal string? CurrentPath => this.entries.Length == 0 ? null : this.entries[ this.cursor ].Path;

	internal void SetEntries( IEnumerable<WidgetFileSystemEntry> items ) {
		string? previous = this.CurrentPath;
		this.entries = items.ToArray(); this.cursor = Math.Max( 0, Array.FindIndex( this.entries, entry => entry.Path == previous ) ); this.offset = 0;
	}
	internal void SetRoots( IReadOnlyList<WidgetFileSystemEntry> items ) {
		ArgumentNullException.ThrowIfNull( items );
		if ( items.Count > WidgetLimits.MaximumItems || items.Any( entry => entry is null || !entry.IsDirectory ) ) throw new ArgumentException( "Roots must be bounded directory entries.", nameof( items ) );
		this.roots = true; this.basket = false; this.SetEntries( items );
	}
	internal void SetListing( IEnumerable<WidgetFileSystemEntry> items ) { this.roots = false; this.basket = false; this.SetEntries( items ); }

	internal bool Process( WidgetInput input, WidgetHost host, WidgetDialog owner, WidgetBrowser browser,
		Action<string> navigate, Action accept, Action? requestRoots, Action<string>? toggle, Action? filter,
		IReadOnlyList<string>? selected, Action<string>? remove ) {
		if ( this.pathEditor is not null ) {
			if ( input.KeyCode == WidgetKey.Escape ) { this.pathEditor = null; return true; }
			if ( input.KeyCode == WidgetKey.Enter ) { string path = this.pathEditor.Value; this.pathEditor = null; if ( !string.IsNullOrWhiteSpace( path ) ) navigate( path ); return true; }
			return this.pathEditor.OnInput( input, host );
		}
		bool control = ( input.Modifiers & WidgetInputModifiers.Control ) != 0;
		int ch = input.Character?.Value ?? 0;
		if ( control && ch is 'l' or 'L' ) { this.pathEditor = new WidgetTextInput( owner.Id + ".path" ); return true; }
		if ( control && ch is 'r' or 'R' ) { requestRoots?.Invoke(); return true; }
		if ( ch is 'f' or 'F' && filter is not null ) { filter(); return true; }
		if ( control && ch is 'b' or 'B' && selected is not null ) { this.basket = true; this.roots = false; this.SetEntries( selected.Select( path => new WidgetFileSystemEntry( path, path, false ) ) ); return true; }
		if ( ( control && ( input.KeyCode == WidgetKey.Enter || ch is 's' or 'S' ) ) || ch is 'a' or 'A' || ( input.KeyCode == WidgetKey.Space && toggle is null ) ) { accept(); return true; }
		if ( input.KeyCode == WidgetKey.Backspace || input.KeyCode == WidgetKey.Up && ( input.Modifiers & WidgetInputModifiers.Alt ) != 0 ) { if ( browser.Snapshot?.ParentLocation is { } parent ) navigate( parent ); return true; }
		if ( input.Kind == WidgetInputKind.Pointer && input.Pointer is { } pointer ) {
			int index = this.offset + pointer.Row - this.listingBounds.Row;
			if ( !this.listingBounds.Contains( pointer.Row, pointer.Column ) || index < 0 || index >= this.entries.Length ) { this.pressedPath = null; return true; }
			if ( pointer.Action == CursesMouseAction.Press ) { this.cursor = index; this.pressedPath = this.CurrentPath; return true; }
			if ( pointer.Action == CursesMouseAction.Release && this.pressedPath == this.entries[ index ].Path ) { this.pressedPath = null; this.Activate( navigate, toggle, remove ); return true; }
			return true;
		}
		if ( input.KeyCode is WidgetKey.Enter or WidgetKey.Space or WidgetKey.Delete ) {
			if ( this.entries.Length == 0 ) return true;
			if ( input.KeyCode == WidgetKey.Enter ) {
				bool acceptSingle = !this.basket && !this.entries[ this.cursor ].IsDirectory && owner is WidgetFilePicker { AllowMultiple: false };
				if ( acceptSingle ) host.EnsureActionCapacity();
				this.Activate( navigate, toggle, remove ); if ( acceptSingle ) accept();
			}
			else if ( !this.entries[ this.cursor ].IsDirectory ) { if ( this.basket ) { remove?.Invoke( this.entries[ this.cursor ].Path ); this.RemoveCurrent(); } else toggle?.Invoke( this.entries[ this.cursor ].Path ); }
			return true;
		}
		this.cursor = input.KeyCode switch {
			WidgetKey.Up => Math.Max( 0, this.cursor - 1 ), WidgetKey.Down => Math.Min( this.entries.Length - 1, this.cursor + 1 ),
			WidgetKey.Home => 0, WidgetKey.End => this.entries.Length - 1,
			WidgetKey.PageUp => Math.Max( 0, this.cursor - this.visibleRows ), WidgetKey.PageDown => Math.Min( this.entries.Length - 1, this.cursor + this.visibleRows ), _ => this.cursor
		};
		this.cursor = Math.Max( 0, this.cursor ); this.EnsureVisible();
		return input.KeyCode is WidgetKey.Up or WidgetKey.Down or WidgetKey.Home or WidgetKey.End or WidgetKey.PageUp or WidgetKey.PageDown;
	}
	private void Activate( Action<string> navigate, Action<string>? toggle, Action<string>? remove ) {
		WidgetFileSystemEntry entry = this.entries[ this.cursor ];
		if ( entry.IsDirectory ) navigate( entry.Path );
		else if ( this.basket ) { remove?.Invoke( entry.Path ); this.RemoveCurrent(); }
		else toggle?.Invoke( entry.Path );
	}
	private void RemoveCurrent() { this.entries = this.entries.Where( ( entry, index ) => index != this.cursor ).ToArray(); this.cursor = Math.Max( 0, Math.Min( this.cursor, this.entries.Length - 1 ) ); }
	private void EnsureVisible() { if ( this.cursor < this.offset ) this.offset = this.cursor; if ( this.cursor >= this.offset + this.visibleRows ) this.offset = this.cursor - this.visibleRows + 1; }

	internal void Render( CursesScreen screen, WidgetTheme theme, WidgetDialog owner, WidgetBrowser browser, string? error, string? filter, IReadOnlyList<string>? selected ) {
		CursesRectangle area = owner.RenderFrame( screen, theme ); if ( area.IsEmpty ) return;
		string location = this.roots ? "Roots / drives" : this.basket ? "Selected files (Enter/Space/Delete removes)" : browser.Snapshot?.Location ?? "No folder loaded";
		WriteLine( screen, area, 0, location + ( filter is null ? "" : " | " + filter ), theme.Normal );
		WriteLine( screen, area, 1, error ?? browser.Error?.Message ?? ( browser.PendingLocation is { } pending ? "Loading: " + pending : "Arrows move; Enter opens/toggles; Ctrl+S selects; Esc cancels" ), error is not null || browser.Error is not null ? theme.Error : theme.Normal );
		this.visibleRows = Math.Max( 1, area.Rows - 4 ); this.EnsureVisible();
		this.listingBounds = new CursesRectangle( area.Row + Math.Min( 2, area.Rows ), area.Column, Math.Max( 0, area.Rows - 4 ), area.Columns );
		for ( int row = 0; row < this.listingBounds.Rows && row + this.offset < this.entries.Length; row++ ) {
			WidgetFileSystemEntry entry = this.entries[ row + this.offset ];
			bool marked = selected?.Contains( entry.Path ) ?? false;
			WriteLine( screen, area, row + 2, ( marked ? "* " : "  " ) + ( entry.IsDirectory ? "[D] " : "    " ) + entry.DisplayName, row + this.offset == this.cursor ? theme.Focused : marked ? theme.Selected : theme.Normal );
		}
		WriteLine( screen, area, area.Rows - 2, selected is null ? "Select current folder with Space or Ctrl+S" : "Selected: " + string.Join( "; ", selected ), theme.Selected );
		WriteLine( screen, area, area.Rows - 1, "Backspace parent | Ctrl+L path | Ctrl+R roots | Ctrl+F filter | Ctrl+B basket", theme.Border );
		if ( this.pathEditor is not null ) { WriteLine( screen, area, 0, "Path (type/paste, Enter loads, Esc returns):", theme.Border ); this.pathEditor.Arrange( new CursesRectangle( area.Row + Math.Min( 1, area.Rows - 1 ), area.Column, 1, area.Columns ) ); this.pathEditor.OnRender( screen, theme, true ); }
	}
	internal static void WriteLine( CursesScreen screen, CursesRectangle area, int row, string text, CursesStyle style ) {
		if ( row < 0 || row >= area.Rows || area.Columns == 0 ) return;
		string safe = string.Concat( text.EnumerateRunes().Select( rune => System.Text.Rune.IsControl( rune ) ? " " : rune.ToString() ) );
		string value = CursesText.TruncateToColumns( safe, area.Columns, screen.TextWidthProvider );
		screen.StandardWindow.Move( area.Row + row, area.Column ); screen.StandardWindow.Write( new string( ' ', area.Columns ), style );
		screen.StandardWindow.Move( area.Row + row, area.Column ); screen.StandardWindow.Write( value, style );
	}
}
