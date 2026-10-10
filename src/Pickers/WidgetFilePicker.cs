/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;
using Icod.DCurses;

/// <summary>Selects one or more existing files after explicit provider validation.</summary>
public sealed class WidgetFilePicker : WidgetDialog {
	private readonly WidgetBrowserView view = new();
	private readonly Guid pickerId = Guid.NewGuid();
	private readonly WidgetFileFilter[] filters;
	private long validationGeneration;
	private long pendingValidation;
	private WidgetFileFilter selectedFilter;

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
	public bool ApplyLoadResult( WidgetBrowserLoadResult result ) {
		if ( this.HasCompleted || !this.Browser.ApplyLoadResult( result ) ) return false;
		if ( result.Error is null && result.Request.IsRoots ) this.view.SetRoots( result.Snapshot!.Entries );
		else if ( result.Error is null ) this.view.SetListing( this.VisibleEntries ); return true;
	}
	/// <summary>Displays provider roots returned by application-owned I/O.</summary>
	public void SetRoots( IReadOnlyList<WidgetFileSystemEntry> roots ) { if ( !this.HasCompleted ) this.view.SetRoots( roots ); }
	public void SelectFilter( string name ) { this.selectedFilter = this.filters.FirstOrDefault( filter => StringComparer.Ordinal.Equals( filter.Name, name ) ) ?? throw new ArgumentException( "Unknown filter.", nameof( name ) ); this.view.SetListing( this.VisibleEntries ); }

	public void ToggleFile( string path ) {
		WidgetFileSystemEntry? entry = this.Browser.Snapshot?.Entries.FirstOrDefault( item => this.Provider.PathComparer.Equals( item.Path, path ) );
		if ( entry is null || entry.IsDirectory ) throw new ArgumentException( "The path is not a file in the current snapshot.", nameof( path ) );
		this.pendingValidation = 0;
		if ( !this.AllowMultiple ) { this.Basket.Clear(); _ = this.Basket.Toggle( entry.Path ); } else _ = this.Basket.Toggle( entry.Path );
		this.ValidationError = null;
	}

	public WidgetPickerValidationRequest RequestAccept() {
		if ( this.Browser.PendingLocation is not null ) throw new InvalidOperationException( "Wait for the directory to finish loading." );
		string[] paths = this.Basket.Paths.ToArray(); if ( 0 == paths.Length ) throw new InvalidOperationException( "Select at least one file before accepting." );
		if ( !this.AllowMultiple && 1 != paths.Length ) throw new InvalidOperationException( "Single-file mode accepts exactly one path." );
		this.pendingValidation = checked( ++this.validationGeneration ); this.ValidationError = null;
		return new WidgetPickerValidationRequest( this.pickerId, this.pendingValidation, paths, WidgetFileSelectionKind.File );
	}

	public bool ApplyValidationResult( WidgetPickerValidationResult result ) {
		ArgumentNullException.ThrowIfNull( result ); if ( !this.IsOpen || result.Request.PickerId != this.pickerId || 0 == this.pendingValidation || result.Request.Generation != this.pendingValidation ) return false;
		if ( result.Validation.IsValid ) this.Host!.EnsureActionCapacity();
		this.pendingValidation = 0;
		if ( !result.Request.Paths.SequenceEqual( this.Basket.Paths, this.Provider.PathComparer ) ) return false;
		if ( !result.Validation.IsValid ) { this.ValidationError = $"Selection is no longer available: {string.Join( ", ", result.Validation.FailedPaths )}"; return true; }
		string[] paths = result.Request.Paths.ToArray(); this.Finish( this.Host!, new WidgetDialogResult( WidgetDialogOutcome.Accepted, paths ) ); return true;
	}

	private void InvalidateValidation() { this.pendingValidation = 0; this.ValidationError = null; }
	internal override void OnClosed() { this.pendingValidation = 0; this.Browser.InvalidatePending(); }

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key == input.Kind && WidgetKey.Escape == input.KeyCode ) { this.pendingValidation = 0; this.Finish( host, WidgetDialogOutcome.Cancel ); return true; }
		return this.view.Process( input, host, this, this.Browser,
			location => { host.EnsureActionCapacity(); host.Enqueue( new WidgetBrowserLoadRequestedAction( this.Id, this.RequestDirectory( location ) ) ); },
			() => { host.EnsureActionCapacity(); try { host.Enqueue( new WidgetPickerValidationRequestedAction( this.Id, this.RequestAccept() ) ); } catch ( InvalidOperationException error ) { this.ValidationError = error.Message; } },
			() => { host.EnsureActionCapacity(); this.pendingValidation = 0; host.Enqueue( new WidgetBrowserLoadRequestedAction( this.Id, this.Browser.RequestRoots() ) ); },
			this.ToggleFile, () => this.SelectFilter( this.filters[ ( Array.IndexOf( this.filters, this.selectedFilter ) + 1 ) % this.filters.Length ].Name ),
			this.Basket.Paths, path => { this.pendingValidation = 0; this.Basket.Remove( path ); } );
	}
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) =>
		this.view.Render( screen, theme, this, this.Browser, this.ValidationError, this.SelectedFilter.Name, this.Basket.Paths );
}
