/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using Icod.DCurses;

/// <summary>Selects one existing current directory after caller-applied validation.</summary>
public sealed class WidgetFolderPicker : WidgetDialog {
	private readonly Guid pickerId = Guid.NewGuid();
	private long validationGeneration;
	private long pendingValidation;
	private int cursorIndex;

	public WidgetFolderPicker( string id, IWidgetFileSystemProvider provider, string startLocation ) : base( id, "Select folder" ) {
		this.Provider = provider ?? throw new ArgumentNullException( nameof( provider ) );
		if ( string.IsNullOrWhiteSpace( startLocation ) ) throw new ArgumentException( "A start location is required.", nameof( startLocation ) );
		this.StartLocation = startLocation; this.Browser = new WidgetBrowser();
	}

	public IWidgetFileSystemProvider Provider { get; }
	public WidgetBrowser Browser { get; }
	public string StartLocation { get; }
	public string? ValidationError { get; private set; }

	public WidgetBrowserRequest RequestDirectory( string location ) { this.pendingValidation = 0; this.ValidationError = null; return this.Browser.RequestDirectory( location ); }
	public bool ApplyLoadResult( WidgetBrowserLoadResult result ) { bool applied = this.Browser.ApplyLoadResult( result ); if ( applied && result.Error is null ) this.cursorIndex = 0; return applied; }

	public WidgetPickerValidationRequest RequestAccept() {
		string location = this.Browser.Snapshot?.Location ?? throw new InvalidOperationException( "A directory must load before it can be selected." );
		this.ValidationError = null; this.pendingValidation = checked( ++this.validationGeneration );
		return new WidgetPickerValidationRequest( this.pickerId, this.pendingValidation, new[] { location }, WidgetFileSelectionKind.Directory );
	}

	public bool ApplyValidationResult( WidgetPickerValidationResult result ) {
		ArgumentNullException.ThrowIfNull( result );
		if ( !this.IsOpen || result.Request.PickerId != this.pickerId || 0 == this.pendingValidation || result.Request.Generation != this.pendingValidation ) return false;
		this.pendingValidation = 0;
		if ( !result.Validation.IsValid ) { this.ValidationError = $"Selection is no longer available: {string.Join( ", ", result.Validation.FailedPaths )}"; return true; }
		string[] paths = result.Request.Paths.ToArray();
		if ( paths.Length != 1 || this.Browser.Snapshot is null || !this.Provider.PathComparer.Equals( paths[ 0 ], this.Browser.Snapshot.Location ) ) { this.ValidationError = "The selected directory changed before validation completed."; return true; }
		this.Finish( this.Host!, new WidgetDialogResult( WidgetDialogOutcome.Accepted, paths ) ); return true;
	}

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key == input.Kind && WidgetKey.Escape == input.KeyCode ) { this.pendingValidation = 0; this.Finish( host, WidgetDialogOutcome.Cancel ); return true; }
		if ( WidgetInputKind.Key != input.Kind ) return false;
		IReadOnlyList<WidgetFileSystemEntry> entries = this.Browser.Snapshot?.Entries ?? [];
		if ( input.KeyCode is WidgetKey.Up or WidgetKey.Down or WidgetKey.Home or WidgetKey.End or WidgetKey.PageUp or WidgetKey.PageDown ) {
			this.cursorIndex = MoveCursor( input.KeyCode, this.cursorIndex, entries.Count, Math.Max( 1, this.Bounds.Rows - 4 ) ); return true;
		}
		if ( input.KeyCode is WidgetKey.Left or WidgetKey.Backspace && this.Browser.Snapshot?.ParentLocation is { } parent ) { this.QueueDirectory( parent, host ); return true; }
		if ( WidgetKey.Enter == input.KeyCode && this.cursorIndex < entries.Count && entries[ this.cursorIndex ].IsDirectory ) { this.QueueDirectory( entries[ this.cursorIndex ].Path, host ); return true; }
		if ( WidgetKey.Space == input.KeyCode ) { host.EnsureActionCapacity(); host.Enqueue( new WidgetPickerValidationRequestedAction( this.Id, this.RequestAccept() ) ); return true; }
		return false;
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		base.OnRender( screen, theme, focused ); CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.Rows < 2 || 0 == area.Columns ) return;
		string location = this.Browser.Snapshot?.Location ?? this.Browser.PendingLocation ?? this.StartLocation; WriteLine( screen, area, 1, location, theme.Normal );
		string? error = this.ValidationError ?? this.Browser.Error?.Message; if ( error is not null && area.Rows >= 3 ) WriteLine( screen, area, 2, error, theme.Error );
		IReadOnlyList<WidgetFileSystemEntry> entries = this.Browser.Snapshot?.Entries ?? []; int rows = Math.Max( 0, area.Rows - 4 ); int first = Math.Clamp( this.cursorIndex - rows + 1, 0, Math.Max( 0, entries.Count - rows ) );
		for ( int row = 0; row < rows && first + row < entries.Count; row++ ) { int index = first + row; WidgetFileSystemEntry entry = entries[ index ]; WriteLine( screen, area, 3 + row, $"{( entry.IsDirectory ? "/ " : "  " )}{entry.DisplayName}", index == this.cursorIndex ? theme.Focused : theme.Normal ); }
		if ( area.Rows >= 4 ) WriteLine( screen, area, area.Rows - 1, "Enter open  Space select  Esc cancel", theme.Border );
	}

	internal override void OnClosed() => this.pendingValidation = 0;

	private void QueueDirectory( string location, WidgetHost host ) { host.EnsureActionCapacity(); host.Enqueue( new WidgetBrowserLoadRequestedAction( this.Id, this.RequestDirectory( location ) ) ); }
	private static int MoveCursor( WidgetKey key, int current, int count, int page ) { if ( 0 == count ) return 0; return key switch { WidgetKey.Up => Math.Max( 0, current - 1 ), WidgetKey.Down => Math.Min( count - 1, current + 1 ), WidgetKey.Home => 0, WidgetKey.End => count - 1, WidgetKey.PageUp => Math.Max( 0, current - page ), _ => Math.Min( count - 1, current + page ) }; }
	private static void WriteLine( CursesScreen screen, CursesRectangle area, int relativeRow, string text, CursesStyle style ) { if ( relativeRow < 0 || relativeRow >= area.Rows ) return; screen.StandardWindow.Move( area.Row + relativeRow, area.Column ); screen.StandardWindow.Write( CursesText.TruncateToColumns( text, area.Columns, screen.TextWidthProvider ), style ); }
}
