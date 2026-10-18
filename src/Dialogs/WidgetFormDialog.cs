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

using System.Globalization;
using Icod.DCurses;

/// <summary>Composes child controls with caller-validated actions and explicit cancellation.</summary>
public sealed class WidgetFormDialog : WidgetDialog {
	private readonly Guid dialogId = Guid.NewGuid();
	private readonly string? initialFocusId;
	private readonly WidgetDialogActionSet actions;
	private WidgetDialogValidationRequest? pending;
	private long generation;
	private int pressed = -1;
	private string? message;
	public WidgetFormDialog( string id, string title, IReadOnlyList<WidgetDialogAction> actions, string? initialFocusId = null ) : base( id, title ) {
		this.actions = new WidgetDialogActionSet( actions, allowValidation: true ); this.initialFocusId = initialFocusId;
	}
	public IReadOnlyList<WidgetDialogAction> Actions => this.actions.Actions;
	public string? Error { get; private set; }
	public string? Message => this.message;
	internal override string? InitialFocusId => this.initialFocusId;
	public static WidgetFormDialog SaveDontSaveCancel( string id, string title, string message ) => new( id, title, [
		new( "save", "Save", WidgetDialogOutcome.Save ),
		new( "dont-save", "Don't Save", WidgetDialogOutcome.DontSave ),
		new( "cancel", "Cancel", WidgetDialogOutcome.Cancel ) { IsDefault = true, IsCancel = true }
	] ) { message = message ?? throw new ArgumentNullException( nameof( message ) ) };

	public void SetActionEnabled( string actionId, bool enabled ) {
		WidgetDialogAction? action = this.actions.Actions.FirstOrDefault( candidate => StringComparer.Ordinal.Equals( candidate.Id, actionId ) );
		if ( action is not null && action.IsEnabled == enabled ) return;
		this.actions.SetEnabled( actionId, enabled ); this.Invalidate();
	}
	public bool ApplyValidationResult( WidgetDialogValidationResult result ) {
		ArgumentNullException.ThrowIfNull( result );
		if ( !this.IsOpen || this.Host is null || this.pending is null || result.Request != this.pending ) return false;
		WidgetDialogAction action = this.actions.Actions.First( action => action.Id == this.pending.ActionId ); this.pending = null;
		if ( !result.IsValid ) { this.Error = result.Error ?? "Validation failed."; return true; }
		this.Error = null; this.Finish( this.Host, new WidgetDialogResult( action.Outcome, action.Value ) ); return true;
	}
	internal override void OnClosed() { this.Invalidate(); this.pressed = -1; }
	protected override void OnCaptureCanceled() => this.pressed = -1;
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is { Button: CursesMouseButton.Primary } pointer ) {
			CursesRectangle area = this.GetContentBounds( host.Screen ); int row = area.BottomExclusive - 1;
			int index = pointer.Row == row && area.Contains( pointer.Row, pointer.Column ) ? this.HitAction( pointer.Column - area.Column, host.Screen ) : -1;
			if ( CursesMouseAction.Press == pointer.Action ) { this.pressed = index; return index >= 0; }
			if ( CursesMouseAction.Release == pointer.Action ) { bool match = index >= 0 && index == this.pressed; this.pressed = -1; if ( match ) { this.Select( index ); this.Activate( host, this.actions.Selected ); } return match; }
		}
		this.pressed = -1;
		if ( this.actions.TryGetMnemonic( input, out WidgetDialogAction mnemonicAction ) ) { this.Activate( host, mnemonicAction ); return true; }
		if ( WidgetInputKind.Key != input.Kind || CursesKeyEventPhase.Release == input.KeyPhase ) return false;
		if ( WidgetKey.Escape == input.KeyCode ) {
			if ( this.actions.CancelAction is { } cancel ) this.Activate( host, cancel );
			else { this.EnsureActionCapacity(); this.Finish( host, WidgetDialogOutcome.Cancel ); }
			return true;
		}
		if ( input.KeyCode is WidgetKey.Left or WidgetKey.Up ) { this.actions.Move( -1, false ); return true; }
		if ( input.KeyCode == WidgetKey.Tab ) { this.actions.Move( 1, true ); return true; }
		if ( input.KeyCode is WidgetKey.Right or WidgetKey.Down ) { this.actions.Move( 1, false ); return true; }
		if ( WidgetKey.Enter == input.KeyCode ) { this.Activate( host, this.actions.Selected ); return true; }
		return false;
	}
	private int HitAction( int column, CursesScreen screen ) {
		int at = 0;
		for ( int index = 0; index < this.actions.Actions.Count; index++ ) {
			WidgetDialogAction action = this.actions.Actions[ index ]; int width = CursesText.MeasureColumns( ActionLabel( action ), screen.TextWidthProvider );
			if ( column >= at && column < at + width ) return action.IsEnabled ? index : -1;
			at += width + 1;
		}
		return -1;
	}
	private void Activate( WidgetHost host, WidgetDialogAction action ) {
		if ( !action.IsEnabled || this.pending is not null && !action.IsCancel ) return;
		if ( action.RequiresValidation ) {
			this.EnsureActionCapacity(); this.Error = null; this.pending = new WidgetDialogValidationRequest( this.dialogId, checked( ++this.generation ), action.Id );
			this.EmitAction( new WidgetDialogValidationRequestedAction( this.Id, this.pending ) ); return;
		}
		this.Finish( host, new WidgetDialogResult( action.Outcome, action.Value ) );
	}
	private static string ActionLabel( WidgetDialogAction action ) => $"[{WidgetBrowserView.Sanitize( action.Label )}]";
	private void Select( int index ) {
		while ( this.actions.SelectedIndex < index && this.actions.Move( 1, false ) ) { }
		while ( this.actions.SelectedIndex > index && this.actions.Move( -1, false ) ) { }
	}
	private void Invalidate() { this.pending = null; this.Error = null; this.generation = checked( this.generation + 1 ); }
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.RenderFrame( screen, theme ); if ( area.IsEmpty ) return;
		if ( this.message is not null ) WidgetBrowserView.WriteLine( screen, area, 0, this.message, this.Style( theme, WidgetStyleRole.Text ) );
		if ( this.Error is not null ) WidgetBrowserView.WriteLine( screen, area, Math.Max( 0, area.Rows - 2 ), this.Error, this.Style( theme, WidgetStyleRole.Error ) );
		string[] labels = this.actions.Actions.Select( ActionLabel ).ToArray();
		WidgetBrowserView.WriteLine( screen, area, area.Rows - 1, string.Join( " ", labels ), this.Style( theme, WidgetStyleRole.DialogAction ) );
		int column = area.Column;
		for ( int index = 0; index < labels.Length && column < area.Column + area.Columns; index++ ) {
			WidgetDialogAction action = this.actions.Actions[ index ]; string label = labels[ index ];
			CursesStyle style = this.Style( theme, !action.IsEnabled ? WidgetStyleRole.DialogDisabledAction :
				index == this.actions.SelectedIndex && focused ? WidgetStyleRole.DialogFocusedAction :
				action.IsDefault ? WidgetStyleRole.DialogDefaultAction : WidgetStyleRole.DialogAction );
			screen.StandardWindow.Move( area.BottomExclusive - 1, column );
			screen.StandardWindow.Write( CursesText.TruncateToColumns( label, area.Column + area.Columns - column, screen.TextWidthProvider ), style );
			this.RenderMnemonic( screen, area.BottomExclusive - 1, column + 1, area.RightExclusive, action, style );
			column += CursesText.MeasureColumns( label, screen.TextWidthProvider ) + 1;
		}
	}
	private void RenderMnemonic( CursesScreen screen, int row, int captionColumn, int right, WidgetDialogAction action, CursesStyle style ) {
		if ( !action.IsEnabled || action.Mnemonic is not char mnemonic ) return;
		string caption = WidgetBrowserView.Sanitize( action.Label ); TextElementEnumerator elements = StringInfo.GetTextElementEnumerator( caption ); int offset = 0;
		while ( elements.MoveNext() ) {
			string element = elements.GetTextElement(); int width = CursesText.MeasureColumns( element, screen.TextWidthProvider );
			if ( char.ToUpperInvariant( element[ 0 ] ) == char.ToUpperInvariant( mnemonic ) ) {
				if ( captionColumn + offset + width <= right ) { screen.StandardWindow.Move( row, captionColumn + offset ); screen.StandardWindow.Write( element, style.WithAttributes( style.Attributes | CursesTextAttributes.Underline ) ); }
				return;
			}
			offset += width;
		}
	}
}
