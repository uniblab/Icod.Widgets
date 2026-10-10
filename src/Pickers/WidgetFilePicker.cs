/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using Icod.DCurses;

/// <summary>Selects one or more existing files after explicit provider validation.</summary>
public sealed class WidgetFilePicker : WidgetDialog {
	private readonly Guid pickerId = Guid.NewGuid();
	private readonly WidgetFileFilter[] filters;
	private long validationGeneration;
	private long pendingValidation;
	private WidgetFileFilter selectedFilter;
	private int cursorIndex;

	public WidgetFilePicker( string id, IWidgetFileSystemProvider provider, string startLocation, bool allowMultiple, IReadOnlyList<WidgetFileFilter> filters ) : base( id, allowMultiple ? "Select files" : "Select file" ) {
		this.Provider = provider ?? throw new ArgumentNullException( nameof( provider ) );
		if ( string.IsNullOrWhiteSpace( startLocation ) ) throw new ArgumentException( "A start location is required.", nameof( startLocation ) );
		ArgumentNullException.ThrowIfNull( filters ); if ( 0 == filters.Count ) throw new ArgumentException( "At least one filter is required.", nameof( filters ) );
		this.filters = filters.ToArray(); if ( this.filters.Select( filter => filter.Name ).Distinct( StringComparer.Ordinal ).Count() != this.filters.Length ) throw new ArgumentException( "Filter names must be unique.", nameof( filters ) );
		this.StartLocation = startLocation; this.AllowMultiple = allowMultiple; this.Browser = new WidgetBrowser(); this.Basket = new WidgetFileSelectionBasket( provider.PathComparer ); this.Basket.Changed += this.InvalidateValidation; this.selectedFilter = this.filters[ 0 ];
	}

	public IWidgetFileSystemProvider Provider { get; }
	public WidgetBrowser Browser { get; }
	public WidgetFileSelectionBasket Basket { get; }
	public string StartLocation { get; }
	public bool AllowMultiple { get; }
	public IReadOnlyList<WidgetFileFilter> Filters => this.filters;
	public WidgetFileFilter SelectedFilter => this.selectedFilter;
	public string? ValidationError { get; private set; }
	public IReadOnlyList<WidgetFileSystemEntry> VisibleEntries => this.Browser.Snapshot?.Entries.Where( entry => entry.IsDirectory || this.selectedFilter.Matches( entry.Path, this.Provider.PathComparer ) ).ToArray() ?? [];

	public WidgetBrowserRequest RequestDirectory( string location ) { this.pendingValidation = 0; this.ValidationError = null; return this.Browser.RequestDirectory( location ); }
	public bool ApplyLoadResult( WidgetBrowserLoadResult result ) { bool applied = this.Browser.ApplyLoadResult( result ); if ( applied && result.Error is null ) this.cursorIndex = 0; return applied; }
	public void SelectFilter( string name ) { this.selectedFilter = this.filters.FirstOrDefault( filter => StringComparer.Ordinal.Equals( filter.Name, name ) ) ?? throw new ArgumentException( "Unknown filter.", nameof( name ) ); this.cursorIndex = 0; }

	public void ToggleFile( string path ) {
		WidgetFileSystemEntry? entry = this.Browser.Snapshot?.Entries.FirstOrDefault( item => this.Provider.PathComparer.Equals( item.Path, path ) );
		if ( entry is null || entry.IsDirectory ) throw new ArgumentException( "The path is not a file in the current snapshot.", nameof( path ) );
		this.pendingValidation = 0;
		if ( !this.AllowMultiple ) { this.Basket.Clear(); _ = this.Basket.Toggle( entry.Path ); } else _ = this.Basket.Toggle( entry.Path );
		this.ValidationError = null;
	}

	public WidgetPickerValidationRequest RequestAccept() {
		string[] paths = this.Basket.Paths.ToArray(); if ( 0 == paths.Length ) throw new InvalidOperationException( "Select at least one file before accepting." );
		if ( !this.AllowMultiple && 1 != paths.Length ) throw new InvalidOperationException( "Single-file mode accepts exactly one path." );
		this.pendingValidation = checked( ++this.validationGeneration ); this.ValidationError = null;
		return new WidgetPickerValidationRequest( this.pickerId, this.pendingValidation, paths, WidgetFileSelectionKind.File );
	}

	public bool ApplyValidationResult( WidgetPickerValidationResult result ) {
		ArgumentNullException.ThrowIfNull( result ); if ( !this.IsOpen || result.Request.PickerId != this.pickerId || 0 == this.pendingValidation || result.Request.Generation != this.pendingValidation ) return false;
		this.pendingValidation = 0;
		if ( !result.Validation.IsValid ) { this.ValidationError = $"Selection is no longer available: {string.Join( ", ", result.Validation.FailedPaths )}"; return true; }
		string[] paths = result.Request.Paths.ToArray(); this.Finish( this.Host!, new WidgetDialogResult( WidgetDialogOutcome.Accepted, paths ) ); return true;
	}

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key == input.Kind && WidgetKey.Escape == input.KeyCode ) { this.pendingValidation = 0; this.Finish( host, WidgetDialogOutcome.Cancel ); return true; }
		if ( WidgetInputKind.Key != input.Kind ) return false;
		IReadOnlyList<WidgetFileSystemEntry> entries = this.VisibleEntries;
		if ( input.KeyCode is WidgetKey.Up or WidgetKey.Down or WidgetKey.Home or WidgetKey.End or WidgetKey.PageUp or WidgetKey.PageDown ) { this.cursorIndex = MoveCursor( input.KeyCode, this.cursorIndex, entries.Count, Math.Max( 1, this.Bounds.Rows - 5 ) ); return true; }
		if ( input.KeyCode is WidgetKey.Left or WidgetKey.Backspace && this.Browser.Snapshot?.ParentLocation is { } parent ) { this.QueueDirectory( parent, host ); return true; }
		if ( WidgetKey.Character == input.KeyCode && input.Character is { Value: 70 or 102 } ) { int index = ( Array.IndexOf( this.filters, this.selectedFilter ) + 1 ) % this.filters.Length; this.SelectFilter( this.filters[ index ].Name ); return true; }
		if ( WidgetKey.Character == input.KeyCode && input.Character is { Value: 65 or 97 } ) return this.QueueAcceptance( host );
		if ( this.cursorIndex >= entries.Count ) return false;
		WidgetFileSystemEntry current = entries[ this.cursorIndex ];
		if ( WidgetKey.Enter == input.KeyCode && current.IsDirectory ) { this.QueueDirectory( current.Path, host ); return true; }
		if ( input.KeyCode is WidgetKey.Enter or WidgetKey.Space && !current.IsDirectory ) {
			this.ToggleFile( current.Path );
			return this.AllowMultiple || WidgetKey.Space == input.KeyCode ? true : this.QueueAcceptance( host );
		}
		return false;
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		base.OnRender( screen, theme, focused ); CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.Rows < 2 || 0 == area.Columns ) return;
		string location = this.Browser.Snapshot?.Location ?? this.Browser.PendingLocation ?? this.StartLocation; WriteLine( screen, area, 1, location, theme.Normal );
		string status = this.ValidationError ?? this.Browser.Error?.Message ?? $"Filter: {this.SelectedFilter.Name}  Selected: {this.Basket.Paths.Count}"; if ( area.Rows >= 3 ) WriteLine( screen, area, 2, status, this.ValidationError is null && this.Browser.Error is null ? theme.Border : theme.Error );
		IReadOnlyList<WidgetFileSystemEntry> entries = this.VisibleEntries; int rows = Math.Max( 0, area.Rows - 4 ); int first = Math.Clamp( this.cursorIndex - rows + 1, 0, Math.Max( 0, entries.Count - rows ) );
		for ( int row = 0; row < rows && first + row < entries.Count; row++ ) { int index = first + row; WidgetFileSystemEntry entry = entries[ index ]; string marker = entry.IsDirectory ? "/ " : this.Basket.Paths.Any( path => this.Provider.PathComparer.Equals( path, entry.Path ) ) ? "* " : "  "; WriteLine( screen, area, 3 + row, marker + entry.DisplayName, index == this.cursorIndex ? theme.Focused : theme.Normal ); }
		if ( area.Rows >= 4 ) WriteLine( screen, area, area.Rows - 1, "Enter open  Space select  A accept  F filter  Esc cancel", theme.Border );
	}

	internal override void OnClosed() => this.pendingValidation = 0;

	private void InvalidateValidation() { this.pendingValidation = 0; this.ValidationError = null; }
	private void QueueDirectory( string location, WidgetHost host ) { host.EnsureActionCapacity(); host.Enqueue( new WidgetBrowserLoadRequestedAction( this.Id, this.RequestDirectory( location ) ) ); }
	private bool QueueAcceptance( WidgetHost host ) { if ( 0 == this.Basket.Paths.Count ) { this.ValidationError = "Select at least one file before accepting."; return true; } host.EnsureActionCapacity(); host.Enqueue( new WidgetPickerValidationRequestedAction( this.Id, this.RequestAccept() ) ); return true; }
	private static int MoveCursor( WidgetKey key, int current, int count, int page ) { if ( 0 == count ) return 0; return key switch { WidgetKey.Up => Math.Max( 0, current - 1 ), WidgetKey.Down => Math.Min( count - 1, current + 1 ), WidgetKey.Home => 0, WidgetKey.End => count - 1, WidgetKey.PageUp => Math.Max( 0, current - page ), _ => Math.Min( count - 1, current + page ) }; }
	private static void WriteLine( CursesScreen screen, CursesRectangle area, int relativeRow, string text, CursesStyle style ) { if ( relativeRow < 0 || relativeRow >= area.Rows ) return; screen.StandardWindow.Move( area.Row + relativeRow, area.Column ); screen.StandardWindow.Write( CursesText.TruncateToColumns( text, area.Columns, screen.TextWidthProvider ), style ); }
}
