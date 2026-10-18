/*
	Icod.Widgets
	Owns validated dialog action state shared by dialog controls.
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

using System.Collections.ObjectModel;

internal sealed class WidgetDialogActionSet {
	private readonly WidgetDialogAction[] actions;
	internal WidgetDialogActionSet( IReadOnlyList<WidgetDialogAction> actions, bool allowValidation ) {
		ArgumentNullException.ThrowIfNull( actions );
		if ( actions.Count == 0 || actions.Count > WidgetLimits.MaximumItems )
			throw new ArgumentOutOfRangeException( nameof( actions ) );
		this.actions = actions.ToArray();
		HashSet<string> ids = new( StringComparer.Ordinal );
		HashSet<char> mnemonics = [];
		foreach ( WidgetDialogAction action in this.actions ) {
			if ( action is null || string.IsNullOrWhiteSpace( action.Id ) || action.Label is null || !ids.Add( action.Id ) )
				throw new ArgumentException( "Dialog actions require unique nonempty IDs and nonnull labels.", nameof( actions ) );
			if ( action.Mnemonic is char mnemonic && !mnemonics.Add( char.ToUpperInvariant( mnemonic ) ) )
				throw new ArgumentException( "Dialog action mnemonics must be unique.", nameof( actions ) );
			if ( !allowValidation && action.RequiresValidation )
				throw new ArgumentException( "Message dialog actions cannot require validation.", nameof( actions ) );
		}
		if ( 1 != this.actions.Count( action => action.IsDefault ) || 1 < this.actions.Count( action => action.IsCancel ) )
			throw new ArgumentException( "A dialog needs one default action and at most one cancel action.", nameof( actions ) );
		this.SelectedIndex = Array.FindIndex( this.actions, action => action.IsDefault );
		if ( !this.Selected.IsEnabled ) throw new ArgumentException( "The default dialog action must be enabled.", nameof( actions ) );
		this.Actions = Array.AsReadOnly( this.actions );
	}

	internal ReadOnlyCollection<WidgetDialogAction> Actions { get; }
	internal int SelectedIndex { get; private set; }
	internal WidgetDialogAction Selected => this.actions[ this.SelectedIndex ];
	internal WidgetDialogAction? CancelAction => this.actions.FirstOrDefault( action => action.IsCancel );

	internal void SetEnabled( string actionId, bool enabled ) {
		int index = Array.FindIndex( this.actions, action => StringComparer.Ordinal.Equals( action.Id, actionId ) );
		if ( index < 0 ) throw new ArgumentException( "Unknown action ID.", nameof( actionId ) );
		if ( this.actions[ index ].IsEnabled == enabled ) return;
		if ( !enabled && 1 == this.actions.Count( action => action.IsEnabled ) )
			throw new InvalidOperationException( "A dialog must retain at least one enabled action." );
		this.actions[ index ] = this.actions[ index ] with { IsEnabled = enabled };
		if ( index == this.SelectedIndex && !enabled ) this.Move( 1, true );
	}

	internal bool Move( int direction, bool wrap ) {
		if ( 0 == direction ) throw new ArgumentOutOfRangeException( nameof( direction ) );
		int step = direction < 0 ? -1 : 1;
		for ( int offset = 1; offset <= this.actions.Length; offset++ ) {
			int index = this.SelectedIndex + step * offset;
			if ( wrap ) index = ( index % this.actions.Length + this.actions.Length ) % this.actions.Length;
			else if ( index < 0 || index >= this.actions.Length ) return false;
			if ( this.actions[ index ].IsEnabled ) { this.SelectedIndex = index; return true; }
		}
		return false;
	}

	internal bool TryGetMnemonic( WidgetInput input, out WidgetDialogAction action ) {
		ArgumentNullException.ThrowIfNull( input );
		action = null!;
		if ( WidgetInputModifiers.None != input.Modifiers ) return false;
		if ( WidgetInputKind.Key == input.Kind && ( WidgetKey.Character != input.KeyCode ||
			input.KeyPhase is not null and not Icod.DCurses.CursesKeyEventPhase.Press ) ) return false;
		if ( input.Kind is not ( WidgetInputKind.Key or WidgetInputKind.Text ) || input.Character is not { } character ||
			character.Value > char.MaxValue ) return false;
		char candidate = char.ToUpperInvariant( (char)character.Value );
		WidgetDialogAction? match = this.actions.FirstOrDefault( candidateAction => candidateAction.IsEnabled &&
			candidateAction.Mnemonic is char mnemonic && char.ToUpperInvariant( mnemonic ) == candidate );
		if ( match is null ) return false;
		action = match; return true;
	}
}
