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

/// <summary>Collects a name and directory, then asks an application provider to inspect a save target.</summary>
public sealed class WidgetSaveTargetPicker : WidgetDialog {
	private readonly Guid pickerId = Guid.NewGuid();
	private readonly WidgetBrowserView view = new();
	private WidgetSaveTargetRequest? pending;
	private long generation;
	public WidgetSaveTargetPicker( string id, IWidgetFileSystemProvider browserProvider, IWidgetSaveTargetProvider? targetProvider, string startLocation, string suggestedName = "" ) : base( id, "Save as" ) {
		this.BrowserProvider = browserProvider ?? throw new ArgumentNullException( nameof( browserProvider ) );
		this.TargetProvider = targetProvider; this.StartLocation = string.IsNullOrWhiteSpace( startLocation ) ? throw new ArgumentException( "A start location is required.", nameof( startLocation ) ) : startLocation;
		this.Browser = new WidgetBrowser(); this.NameInput = new WidgetTextInput( id + ".name", suggestedName ?? throw new ArgumentNullException( nameof( suggestedName ) ) ); this.Add( this.NameInput );
		if ( targetProvider is null ) this.ValidationError = "Save-target inspection is unsupported by this provider.";
	}
	public IWidgetFileSystemProvider BrowserProvider { get; }
	public IWidgetSaveTargetProvider? TargetProvider { get; }
	public WidgetBrowser Browser { get; }
	public WidgetTextInput NameInput { get; }
	public string StartLocation { get; }
	public string? ValidationError { get; private set; }
	internal override string? InitialFocusId => this.NameInput.Id;
	public WidgetBrowserRequest RequestDirectory( string location ) { this.Invalidate(); return this.Browser.RequestDirectory( location ); }
	public bool ApplyLoadResult( WidgetBrowserLoadResult result ) { if ( this.HasCompleted || !this.Browser.ApplyLoadResult( result ) ) return false; this.Invalidate(); return true; }
	public WidgetSaveTargetRequest RequestAccept() {
		if ( this.TargetProvider is null ) { this.ValidationError = "Save-target inspection is unsupported by this provider."; throw new InvalidOperationException( this.ValidationError ); }
		if ( this.Browser.PendingLocation is not null ) { this.ValidationError = "Wait for the directory to finish loading."; throw new InvalidOperationException( this.ValidationError ); }
		string name = this.NameInput.Value;
		if ( string.IsNullOrWhiteSpace( name ) || name.Length > 255 ) { this.ValidationError = "Enter a nonempty file name of at most 255 UTF-16 units."; throw new InvalidOperationException( this.ValidationError ); }
		this.ValidationError = null;
		this.pending = new WidgetSaveTargetRequest( this.pickerId, checked( ++this.generation ), this.Browser.Snapshot?.Location ?? this.StartLocation, name );
		return this.pending;
	}
	public bool ApplyInspectionResult( WidgetSaveTargetInspectionResult result ) {
		ArgumentNullException.ThrowIfNull( result );
		if ( !this.IsOpen || this.Host is null || this.pending is null || result.Request != this.pending ||
			!StringComparer.Ordinal.Equals( this.NameInput.Value, this.pending.FileName ) ||
			!this.BrowserProvider.PathComparer.Equals( this.Browser.Snapshot?.Location ?? this.StartLocation, this.pending.Directory ) || this.Browser.PendingLocation is not null ) return false;
		this.pending = null; WidgetSaveTargetObservation observation = result.Observation;
		if ( observation is null || !observation.IsValid || string.IsNullOrWhiteSpace( observation.TargetPath ) ) {
			this.ValidationError = observation?.Error ?? "The target could not be inspected."; return true;
		}
		this.Host.EnsureActionCapacity(); this.Finish( this.Host, new WidgetDialogResult( WidgetDialogOutcome.Accepted, new WidgetSaveTargetSelection( observation.TargetPath, observation.Exists ) ) ); return true;
	}
	internal override void OnClosed() { this.Invalidate(); this.Browser.InvalidatePending(); }
	private void Invalidate() { this.pending = null; this.generation = checked( this.generation + 1 ); this.ValidationError = null; }
	protected override void OnArrange( CursesRectangle bounds ) {
		CursesRectangle area = this.Host is null ? bounds : this.GetContentBounds( this.Host.Screen );
		this.NameInput.Arrange( new CursesRectangle( Math.Max( area.Row, area.BottomExclusive - 1 ), area.Column, Math.Min( 1, area.Rows ), area.Columns ) );
	}
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key == input.Kind && WidgetKey.Escape == input.KeyCode ) { this.Invalidate(); this.Finish( host, WidgetDialogOutcome.Cancel ); return true; }
		if ( WidgetInputKind.Key == input.Kind && WidgetKey.Enter == input.KeyCode ) {
			try { this.EnsureActionCapacity(); this.EmitAction( new WidgetSaveTargetInspectionRequestedAction( this.Id, this.RequestAccept() ) ); }
			catch ( InvalidOperationException ) { }
			return true;
		}
		return this.view.Process( input, host, this, this.Browser,
			location => { host.EnsureActionCapacity(); host.Enqueue( new WidgetBrowserLoadRequestedAction( this.Id, this.RequestDirectory( location ) ) ); },
			() => { try { host.EnsureActionCapacity(); host.Enqueue( new WidgetSaveTargetInspectionRequestedAction( this.Id, this.RequestAccept() ) ); } catch ( InvalidOperationException ) { } },
			() => { host.EnsureActionCapacity(); this.Invalidate(); host.Enqueue( new WidgetBrowserLoadRequestedAction( this.Id, this.Browser.RequestRoots() ) ); },
			_ => { }, () => { }, [], _ => { } );
	}
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) =>
		this.view.Render( screen, theme, this, this.Browser, this.ValidationError, "Save target", [] );
}
