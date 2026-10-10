/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

/// <summary>Selects one existing current directory after caller-applied validation.</summary>
public sealed class WidgetFolderPicker : WidgetDialog {
	private readonly Guid pickerId = Guid.NewGuid();
	private long validationGeneration;
	private long pendingValidation;

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
	public bool ApplyLoadResult( WidgetBrowserLoadResult result ) => this.Browser.ApplyLoadResult( result );

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
		return false;
	}
}
