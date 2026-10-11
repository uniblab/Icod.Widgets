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

internal sealed class WidgetTextHistory( int maximumGroups, long maximumRetainedUnits ) {
	internal readonly record struct EditShape( long Start, long DeleteLength, long InsertLength );
	internal sealed record Group(
		WidgetPieceTree.Node? BeforeRoot,
		WidgetPieceTree.Node? AfterRoot,
		long BeforeStateId,
		long AfterStateId,
		WidgetTextEditContext Context,
		IReadOnlyList<WidgetTextChange> ForwardChanges,
		IReadOnlyList<WidgetTextChange> BackwardChanges,
		long RetainedUnits,
		long CoalescenceGeneration,
		EditShape? Shape );

	private readonly List<Group> groups = [];
	private int cursor;
	private long retainedUnits;

	internal Group? LastApplied => this.cursor == this.groups.Count && 0 < this.cursor ? this.groups[ this.cursor - 1 ] : null;
	internal Group? UndoGroup => 0 < this.cursor ? this.groups[ this.cursor - 1 ] : null;
	internal Group? RedoGroup => this.cursor < this.groups.Count ? this.groups[ this.cursor ] : null;
	internal bool CanUndo => 0 < this.cursor;
	internal bool CanRedo => this.cursor < this.groups.Count;

	internal void Commit( Group group, bool replacesLast ) {
		while ( this.groups.Count > this.cursor ) { this.retainedUnits -= this.groups[ ^1 ].RetainedUnits; this.groups.RemoveAt( this.groups.Count - 1 ); }
		if ( replacesLast ) { this.retainedUnits -= this.groups[ ^1 ].RetainedUnits; this.groups[ ^1 ] = group; }
		else { this.groups.Add( group ); this.cursor++; }
		this.retainedUnits = checked( this.retainedUnits + group.RetainedUnits );
		while ( this.groups.Count > maximumGroups || this.retainedUnits > maximumRetainedUnits ) {
			this.retainedUnits -= this.groups[ 0 ].RetainedUnits; this.groups.RemoveAt( 0 ); this.cursor--;
		}
	}

	internal Group TakeUndo() { Group result = this.groups[ this.cursor - 1 ]; this.cursor--; return result; }
	internal Group TakeRedo() { Group result = this.groups[ this.cursor ]; this.cursor++; return result; }
}
