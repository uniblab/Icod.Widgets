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

/// <summary>Selects one existing current directory after caller-applied validation.</summary>
public sealed class WidgetFolderPicker : WidgetDialog {
	private readonly WidgetBrowserView view = new();
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
	public bool ApplyLoadResult( WidgetBrowserLoadResult result ) {
		if ( this.HasCompleted || !this.Browser.ApplyLoadResult( result ) ) return false;
		if ( result.Error is null && result.Request.IsRoots ) this.view.SetRoots( result.Snapshot!.Entries );
		else if ( result.Error is null ) this.view.SetListing( this.Browser.Snapshot?.Entries.Where( entry => entry.IsDirectory ) ?? [] ); return true;
	}
	/// <summary>Displays provider roots returned by application-owned I/O.</summary>
	public void SetRoots( IReadOnlyList<WidgetFileSystemEntry> roots ) { if ( !this.HasCompleted ) this.view.SetRoots( roots ); }

	public WidgetPickerValidationRequest RequestAccept() {
		if ( this.Browser.PendingLocation is not null ) throw new InvalidOperationException( "Wait for the directory to finish loading." );
		string location = this.Browser.Snapshot?.Location ?? throw new InvalidOperationException( "A directory must load before it can be selected." );
		this.ValidationError = null; this.pendingValidation = checked( ++this.validationGeneration );
		return new WidgetPickerValidationRequest( this.pickerId, this.pendingValidation, new[] { location }, WidgetFileSelectionKind.Directory );
	}

	public bool ApplyValidationResult( WidgetPickerValidationResult result ) {
		ArgumentNullException.ThrowIfNull( result );
		if ( !this.IsOpen || result.Request.PickerId != this.pickerId || 0 == this.pendingValidation || result.Request.Generation != this.pendingValidation ) return false;
		if ( result.Validation.IsValid ) this.Host!.EnsureActionCapacity();
		this.pendingValidation = 0;
		if ( !result.Validation.IsValid ) { this.ValidationError = $"Selection is no longer available: {string.Join( ", ", result.Validation.FailedPaths )}"; return true; }
		string[] paths = result.Request.Paths.ToArray();
		if ( paths.Length != 1 || this.Browser.Snapshot is null || !this.Provider.PathComparer.Equals( paths[ 0 ], this.Browser.Snapshot.Location ) ) { this.ValidationError = "The selected directory changed before validation completed."; return true; }
		this.Finish( this.Host!, new WidgetDialogResult( WidgetDialogOutcome.Accepted, paths ) ); return true;
	}

	internal override void OnClosed() { this.pendingValidation = 0; this.Browser.InvalidatePending(); }

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key == input.Kind && WidgetKey.Escape == input.KeyCode ) { this.pendingValidation = 0; this.Finish( host, WidgetDialogOutcome.Cancel ); return true; }
		return this.view.Process( input, host, this, this.Browser,
			location => { host.EnsureActionCapacity(); host.Enqueue( new WidgetBrowserLoadRequestedAction( this.Id, this.RequestDirectory( location ) ) ); },
			() => { host.EnsureActionCapacity(); try { host.Enqueue( new WidgetPickerValidationRequestedAction( this.Id, this.RequestAccept() ) ); } catch ( InvalidOperationException error ) { this.ValidationError = error.Message; } },
			() => { host.EnsureActionCapacity(); this.pendingValidation = 0; host.Enqueue( new WidgetBrowserLoadRequestedAction( this.Id, this.Browser.RequestRoots() ) ); },
			null, null,
			null, null );
	}
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) =>
		this.view.Render( screen, theme, this, this.Browser, this.ValidationError, null, null );
}
