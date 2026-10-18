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

/// <summary>Displays a message with explicit safe-default choices.</summary>
public sealed class WidgetMessageDialog : WidgetDialog {
	private readonly WidgetDialogActionSet actions;
	private int pressedIndex = -1;
	public WidgetMessageDialog( string id, string title, string message, IReadOnlyList<WidgetDialogAction> actions ) : base( id, title ) {
		this.Message = message ?? throw new ArgumentNullException( nameof( message ) );
		this.actions = new WidgetDialogActionSet( actions, allowValidation: false );
	}
	public string Message { get; }
	public IReadOnlyList<WidgetDialogAction> Actions => this.actions.Actions;
	public WidgetDialogOutcome SelectedOutcome => this.actions.Selected.Outcome;
	public static WidgetMessageDialog Ok( string id, string title, string message ) => new( id, title, message, [
		new( "ok", "OK", WidgetDialogOutcome.Ok ) { IsDefault = true }
	] );
	[Obsolete(
		"Use the WidgetMessageDialog action-array constructor so captions, "
		+ "mnemonics, default, and cancel behavior are explicit."
		, false
	)]
	public static WidgetMessageDialog YesNo( string id, string title, string message ) => new( id, title, message, [
		new( "yes", "Yes", WidgetDialogOutcome.Yes ),
		new( "no", "No", WidgetDialogOutcome.No ) { IsDefault = true }
	] );
	public static WidgetMessageDialog OkCancel( string id, string title, string message ) => new( id, title, message, [
		new( "ok", "OK", WidgetDialogOutcome.Ok ),
		new( "cancel", "Cancel", WidgetDialogOutcome.Cancel ) { IsDefault = true, IsCancel = true }
	] );
	public void SetActionEnabled( string actionId, bool enabled ) => this.actions.SetEnabled( actionId, enabled );

	internal override void OnClosed() => this.pressedIndex = -1;
	protected override void OnCaptureCanceled() => this.pressedIndex = -1;
	protected override void OnArrange( CursesRectangle bounds ) { this.pressedIndex = -1; base.OnArrange( bounds ); }

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is { } pointer ) {
			int index = this.HitAction( pointer, host.Screen );
			if ( CursesMouseAction.Press == pointer.Action ) { this.pressedIndex = CursesMouseButton.Primary == pointer.Button ? index : -1; return this.pressedIndex >= 0; }
			if ( CursesMouseAction.Release == pointer.Action ) {
				bool activate = CursesMouseButton.Primary == pointer.Button && index >= 0 && index == this.pressedIndex;
				this.pressedIndex = -1;
				if ( activate ) { this.Select( index ); this.Activate( host, this.actions.Selected ); }
				return activate;
			}
			return false;
		}
		this.pressedIndex = -1;
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

	private void Activate( WidgetHost host, WidgetDialogAction action ) {
		if ( this.HasCompleted || !action.IsEnabled ) return;
		this.EnsureActionCapacity(); this.Finish( host, new WidgetDialogResult( action.Outcome, action.Value ) );
	}
	private static string ActionLabel( WidgetDialogAction action ) => $"[{WidgetBrowserView.Sanitize( action.Label )}]";
	private void Select( int index ) {
		while ( this.actions.SelectedIndex < index && this.actions.Move( 1, false ) ) { }
		while ( this.actions.SelectedIndex > index && this.actions.Move( -1, false ) ) { }
	}
	private int HitAction( CursesMouseEvent pointer, CursesScreen screen ) {
		CursesRectangle area = this.GetContentBounds( screen );
		if ( area.IsEmpty || pointer.Row != area.BottomExclusive - 1 || !area.Contains( pointer.Row, pointer.Column ) ) return -1;
		int column = area.Column;
		for ( int index = 0; index < this.actions.Actions.Count; index++ ) {
			int width = CursesText.MeasureColumns( ActionLabel( this.actions.Actions[ index ] ), screen.TextWidthProvider );
			if ( pointer.Column >= column && pointer.Column < Math.Min( area.RightExclusive, column + width ) )
				return this.actions.Actions[ index ].IsEnabled ? index : -1;
			column += width + 2;
		}
		return -1;
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.RenderFrame( screen, theme ); if ( area.IsEmpty ) return;
		CursesTextLayout layout = CursesTextLayout.Create( this.Message, new CursesTextLayoutOptions( area.Columns ) { WrapMode = CursesTextWrapMode.Word, WidthProvider = screen.TextWidthProvider, DefaultStyle = this.Style( theme, WidgetStyleRole.Text ) } );
		screen.StandardWindow.PresentTextLayout( layout, 0, Math.Min( layout.Lines.Count, Math.Max( 0, area.Rows - 1 ) ), area.Row, area.Column );
		string[] labels = this.actions.Actions.Select( ActionLabel ).ToArray();
		WidgetBrowserView.WriteLine( screen, area, area.Rows - 1, string.Join( "  ", labels ) + " (Esc cancels)", this.Style( theme, WidgetStyleRole.DialogAction ) );
		int column = area.Column;
		for ( int index = 0; index < labels.Length && column < area.RightExclusive; index++ ) {
			WidgetDialogAction action = this.actions.Actions[ index ]; string label = labels[ index ];
			CursesStyle style = this.Style( theme, index == this.actions.SelectedIndex && focused ? WidgetStyleRole.DialogFocusedAction :
				!action.IsEnabled ? WidgetStyleRole.DialogDisabledAction :
				action.IsDefault ? WidgetStyleRole.DialogDefaultAction : WidgetStyleRole.DialogAction );
			screen.StandardWindow.Move( area.BottomExclusive - 1, column );
			screen.StandardWindow.Write( CursesText.TruncateToColumns( label, area.RightExclusive - column, screen.TextWidthProvider ), style );
			this.RenderMnemonic( screen, area.BottomExclusive - 1, column + 1, area.RightExclusive, action, style );
			column += CursesText.MeasureColumns( label, screen.TextWidthProvider ) + 2;
		}
	}

	private void RenderMnemonic( CursesScreen screen, int row, int captionColumn, int right, WidgetDialogAction action, CursesStyle style ) {
		if ( !action.IsEnabled || action.Mnemonic is not char mnemonic ) return;
		string caption = WidgetBrowserView.Sanitize( action.Label );
		TextElementEnumerator elements = StringInfo.GetTextElementEnumerator( caption ); int offset = 0;
		while ( elements.MoveNext() ) {
			string element = elements.GetTextElement(); int width = CursesText.MeasureColumns( element, screen.TextWidthProvider );
			if ( char.ToUpperInvariant( element[ 0 ] ) == char.ToUpperInvariant( mnemonic ) ) {
				if ( captionColumn + offset + width <= right ) {
					screen.StandardWindow.Move( row, captionColumn + offset );
					screen.StandardWindow.Write( element, style.WithAttributes( style.Attributes | CursesTextAttributes.Underline ) );
				}
				return;
			}
			offset += width;
		}
	}
}

