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

/// <summary>Composes child controls with caller-validated actions and explicit cancellation.</summary>
public sealed class WidgetFormDialog : WidgetDialog {
	private readonly Guid dialogId = Guid.NewGuid();
	private readonly string? initialFocusId;
	private WidgetDialogAction[] actions;
	private WidgetDialogValidationRequest? pending;
	private long generation;
	private int selected;
	private int pressed = -1;
	private string? message;
	public WidgetFormDialog( string id, string title, IReadOnlyList<WidgetDialogAction> actions, string? initialFocusId = null ) : base( id, title ) {
		ArgumentNullException.ThrowIfNull( actions );
		if ( actions.Count == 0 || actions.Count > WidgetLimits.MaximumItems ) throw new ArgumentOutOfRangeException( nameof( actions ) );
		this.actions = actions.ToArray(); HashSet<string> ids = new( StringComparer.Ordinal );
		foreach ( WidgetDialogAction action in this.actions ) if ( action is null || string.IsNullOrWhiteSpace( action.Id ) || action.Label is null || !ids.Add( action.Id ) )
			throw new ArgumentException( "Dialog actions require unique nonempty IDs and nonnull labels.", nameof( actions ) );
		if ( 1 != this.actions.Count( action => action.IsDefault ) || 1 < this.actions.Count( action => action.IsCancel ) )
			throw new ArgumentException( "A form needs one default action and at most one cancel action.", nameof( actions ) );
		this.selected = Array.FindIndex( this.actions, action => action.IsDefault ); this.initialFocusId = initialFocusId;
	}
	public IReadOnlyList<WidgetDialogAction> Actions => Array.AsReadOnly( this.actions );
	public string? Error { get; private set; }
	public string? Message => this.message;
	internal override string? InitialFocusId => this.initialFocusId;
	public static WidgetFormDialog SaveDontSaveCancel( string id, string title, string message ) => new( id, title, [
		new( "save", "Save", WidgetDialogOutcome.Save ),
		new( "dont-save", "Don't Save", WidgetDialogOutcome.DontSave ),
		new( "cancel", "Cancel", WidgetDialogOutcome.Cancel ) { IsDefault = true, IsCancel = true }
	] ) { message = message ?? throw new ArgumentNullException( nameof( message ) ) };

	public void SetActionEnabled( string actionId, bool enabled ) {
		int index = Array.FindIndex( this.actions, action => StringComparer.Ordinal.Equals( action.Id, actionId ) );
		if ( index < 0 ) throw new ArgumentException( "Unknown action ID.", nameof( actionId ) );
		if ( this.actions[ index ].IsEnabled == enabled ) return;
		this.actions[ index ] = this.actions[ index ] with { IsEnabled = enabled }; this.Invalidate();
	}
	public bool ApplyValidationResult( WidgetDialogValidationResult result ) {
		ArgumentNullException.ThrowIfNull( result );
		if ( !this.IsOpen || this.Host is null || this.pending is null || result.Request != this.pending ) return false;
		WidgetDialogAction action = this.actions.First( action => action.Id == this.pending.ActionId ); this.pending = null;
		if ( !result.IsValid ) { this.Error = result.Error ?? "Validation failed."; return true; }
		this.Error = null; this.Finish( this.Host, new WidgetDialogResult( action.Outcome, action.Value ) ); return true;
	}
	internal override void OnClosed() { this.Invalidate(); this.pressed = -1; }
	protected override void OnCaptureCanceled() => this.pressed = -1;
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key == input.Kind ) {
			if ( WidgetKey.Escape == input.KeyCode ) { this.Finish( host, WidgetDialogOutcome.Cancel ); return true; }
			if ( input.KeyCode is WidgetKey.Left or WidgetKey.Right ) {
				int direction = WidgetKey.Right == input.KeyCode ? 1 : -1;
				for ( int step = 1; step <= this.actions.Length; step++ ) {
					int index = ( this.selected + direction * step + this.actions.Length * 2 ) % this.actions.Length;
					if ( this.actions[ index ].IsEnabled ) { this.selected = index; return true; }
				}
				return true;
			}
			if ( WidgetKey.Enter == input.KeyCode ) { this.Activate( host, this.actions[ this.selected ] ); return true; }
		}
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is { Button: CursesMouseButton.Primary } pointer ) {
			CursesRectangle area = this.GetContentBounds( host.Screen ); int row = area.BottomExclusive - 1;
			int index = pointer.Row == row && area.Contains( pointer.Row, pointer.Column ) ? this.HitAction( pointer.Column - area.Column ) : -1;
			if ( CursesMouseAction.Press == pointer.Action ) { this.pressed = index; return index >= 0; }
			if ( CursesMouseAction.Release == pointer.Action ) { bool match = index >= 0 && index == this.pressed; this.pressed = -1; if ( match ) { this.selected = index; this.Activate( host, this.actions[ index ] ); } return match; }
		}
		return false;
	}
	private int HitAction( int column ) {
		int at = 0; for ( int i = 0; i < this.actions.Length; i++ ) { int length = this.actions[ i ].Label.Length + 3; if ( column >= at && column < at + length ) return i; at += length + 1; }
		return -1;
	}
	private void Activate( WidgetHost host, WidgetDialogAction action ) {
		if ( !action.IsEnabled || this.pending is not null ) return;
		if ( action.RequiresValidation ) {
			this.EnsureActionCapacity(); this.Error = null; this.pending = new WidgetDialogValidationRequest( this.dialogId, checked( ++this.generation ), action.Id );
			this.EmitAction( new WidgetDialogValidationRequestedAction( this.Id, this.pending ) ); return;
		}
		this.Finish( host, new WidgetDialogResult( action.Outcome, action.Value ) );
	}
	private void Invalidate() { this.pending = null; this.Error = null; this.generation = checked( this.generation + 1 ); }
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.RenderFrame( screen, theme ); if ( area.IsEmpty ) return;
		if ( this.message is not null ) WidgetBrowserView.WriteLine( screen, area, 0, this.message, this.Style( theme, WidgetStyleRole.Text ) );
		if ( this.Error is not null ) WidgetBrowserView.WriteLine( screen, area, Math.Max( 0, area.Rows - 2 ), this.Error, this.Style( theme, WidgetStyleRole.Error ) );
		string[] labels = this.actions.Select( ( action, i ) => i == this.selected ? $"[{action.Label}]" : $" {action.Label} " ).ToArray();
		WidgetBrowserView.WriteLine( screen, area, area.Rows - 1, string.Join( " ", labels ), this.Style( theme, WidgetStyleRole.DialogAction ) );
		int column = area.Column;
		for ( int index = 0; index < labels.Length && column < area.Column + area.Columns; index++ ) {
			string label = WidgetBrowserView.Sanitize( labels[ index ] );
			CursesStyle style = this.Style( theme, !this.actions[ index ].IsEnabled ? WidgetStyleRole.DialogDisabledAction :
				index == this.selected && focused ? WidgetStyleRole.DialogFocusedAction :
				this.actions[ index ].IsDefault ? WidgetStyleRole.DialogDefaultAction : WidgetStyleRole.DialogAction );
			screen.StandardWindow.Move( area.BottomExclusive - 1, column );
			screen.StandardWindow.Write( CursesText.TruncateToColumns( label, area.Column + area.Columns - column, screen.TextWidthProvider ), style );
			column += CursesText.MeasureColumns( label, screen.TextWidthProvider ) + 1;
		}
	}
}
