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

/// <summary>Owns exact UTF-16 text, indexed logical lines, history state, and immutable snapshots.</summary>
public sealed class WidgetTextDocument {
	private readonly object snapshotGate = new();
	private readonly WidgetTextDocumentOptions options;
	private readonly WidgetTextHistory history;
	private WidgetPieceTree.Node? root;
	private int liveSnapshots;
	private long nextStateId;
	private long savedStateId;
	private long coalescenceGeneration;
	private TimeSpan? lastEditTimestamp;

	public WidgetTextDocument( IEnumerable<ReadOnlyMemory<char>> chunks, WidgetTextDocumentOptions? options = null ) {
		this.options = options ?? new WidgetTextDocumentOptions(); this.options.Validate();
		this.root = WidgetPieceTree.Create( chunks, this.options.MaximumDocumentLength ); this.Id = Guid.NewGuid();
		this.history = new WidgetTextHistory( this.options.MaximumHistoryGroups, this.options.MaximumHistoryRetainedUnits );
	}

	public Guid Id { get; }
	public long Length => this.root?.Length ?? 0;
	public long LineCount => checked( ( this.root?.BreakCount ?? 0 ) + 1 );
	public long Revision { get; private set; }
	public long StateId { get; private set; }
	public bool IsModified => this.StateId != this.savedStateId;
	public bool CanUndo => this.history.CanUndo;
	public bool CanRedo => this.history.CanRedo;
	public event EventHandler<WidgetTextDocumentChangedEventArgs>? Changed;

	public string Read( long offset, int length ) {
		this.ValidateRange( offset, length ); return WidgetPieceTree.Read( this.root, offset, length );
	}

	public WidgetTextLine GetLine( long index ) => WidgetPieceTree.GetLine( this.root, index );
	public long GetLineIndex( long offset ) {
		if ( offset < 0 || offset > this.Length ) throw new ArgumentOutOfRangeException( nameof( offset ) );
		return WidgetPieceTree.GetLineIndex( this.root, offset );
	}
	public bool IsTextElementBoundary( long offset ) => this.IsTextElementBoundary( this.root, offset );

	public WidgetTextEditResult Apply( IReadOnlyList<WidgetTextEdit> edits, long expectedRevision, WidgetTextEditContext context ) {
		ArgumentNullException.ThrowIfNull( edits ); ArgumentNullException.ThrowIfNull( context );
		if ( expectedRevision != this.Revision ) return this.Result( WidgetTextEditStatus.Stale );
		if ( TimeSpan.Zero > context.Timestamp || this.lastEditTimestamp.HasValue && context.Timestamp < this.lastEditTimestamp.Value )
			throw new ArgumentOutOfRangeException( nameof( context ), "Edit time must be nonnegative and monotonic." );
		if ( !Enum.IsDefined( context.Kind ) ) throw new ArgumentOutOfRangeException( nameof( context ) );
		if ( 0 == edits.Count ) { this.coalescenceGeneration++; return this.Result( WidgetTextEditStatus.NoChange ); }

		List<WidgetTextEdit> effective = []; long insertedUnits = 0; long deletedUnits = 0;
		foreach ( WidgetTextEdit edit in edits ) {
			if ( edit is null ) throw new ArgumentException( "An edit cannot be null.", nameof( edits ) );
			if ( edit.InsertText is null ) throw new ArgumentException( "Inserted text cannot be null.", nameof( edits ) );
			if ( edit.Start < 0 || edit.DeleteLength < 0 || edit.Start > this.Length || edit.DeleteLength > this.Length - edit.Start ||
				!this.IsTextElementBoundary( this.root, edit.Start ) || !this.IsTextElementBoundary( this.root, edit.Start + edit.DeleteLength ) )
				return this.Result( WidgetTextEditStatus.InvalidRange );
			if ( edit.InsertText.Length > this.options.MaximumSingleEditLength || edit.DeleteLength > this.options.MaximumSingleEditLength )
				return this.Result( WidgetTextEditStatus.LimitExceeded );
			bool same = edit.DeleteLength == edit.InsertText.Length && edit.DeleteLength <= int.MaxValue &&
				WidgetPieceTree.ContentEquals( this.root, edit.Start, edit.InsertText );
			if ( !same ) {
				insertedUnits = checked( insertedUnits + edit.InsertText.Length ); deletedUnits = checked( deletedUnits + edit.DeleteLength ); effective.Add( edit );
			}
		}
		if ( insertedUnits > this.options.MaximumSingleEditLength || deletedUnits > this.options.MaximumSingleEditLength )
			return this.Result( WidgetTextEditStatus.LimitExceeded );
		if ( 0 == effective.Count ) { if ( !context.AllowCoalescing ) this.coalescenceGeneration++; return this.Result( WidgetTextEditStatus.NoChange ); }
		effective.Sort( static ( left, right ) => left.Start.CompareTo( right.Start ) );
		for ( int index = 1; index < effective.Count; index++ ) {
			WidgetTextEdit previous = effective[ index - 1 ]; WidgetTextEdit current = effective[ index ];
			if ( current.Start < previous.Start + previous.DeleteLength || current.Start == previous.Start && ( 0 == current.DeleteLength || 0 == previous.DeleteLength ) )
				return this.Result( WidgetTextEditStatus.InvalidRange );
		}
		long nextLength;
		try { nextLength = checked( this.Length - deletedUnits + insertedUnits ); }
		catch ( OverflowException ) { return this.Result( WidgetTextEditStatus.LimitExceeded ); }
		if ( nextLength > this.options.MaximumDocumentLength || 0 == this.options.MaximumHistoryGroups ||
			insertedUnits + deletedUnits > this.options.MaximumHistoryRetainedUnits ) return this.Result( WidgetTextEditStatus.LimitExceeded );
		if ( !this.IsSelectionValid( this.root, context.BeforeAnchor, context.BeforeCaret ) ) return this.Result( WidgetTextEditStatus.InvalidRange );

		WidgetPieceTree.Node? candidate = this.root;
		for ( int index = effective.Count - 1; index >= 0; index-- ) {
			WidgetTextEdit edit = effective[ index ]; candidate = WidgetPieceTree.Replace( candidate, edit.Start, edit.DeleteLength, edit.InsertText );
		}
		if ( !this.IsSelectionValid( candidate, context.AfterAnchor, context.AfterCaret ) ) return this.Result( WidgetTextEditStatus.InvalidRange );
		if ( ExceedsFragmentationLimit( candidate ) ) candidate = WidgetPieceTree.Rebuild( candidate );
		if ( ExceedsFragmentationLimit( candidate ) ) return this.Result( WidgetTextEditStatus.LimitExceeded );

		long revision; long stateId;
		try { revision = checked( this.Revision + 1 ); stateId = checked( this.nextStateId + 1 ); }
		catch ( OverflowException ) { return this.Result( WidgetTextEditStatus.LimitExceeded ); }
		( IReadOnlyList<WidgetTextChange> forward, IReadOnlyList<WidgetTextChange> backward ) = BuildChanges( effective );
		WidgetTextHistory.EditShape? shape = GetShape( effective );
		long retained = checked( insertedUnits + deletedUnits );
		WidgetTextHistory.Group group = new( this.root, candidate, this.StateId, stateId, context, forward, backward,
			retained, this.coalescenceGeneration, shape );
		bool coalesced = this.TryCoalesce( group, out WidgetTextHistory.Group committed );
		if ( !context.AllowCoalescing ) this.coalescenceGeneration++;

		this.history.Commit( committed, coalesced ); this.root = candidate; this.Revision = revision;
		this.StateId = stateId; this.nextStateId = stateId; this.lastEditTimestamp = context.Timestamp;
		this.OnChanged( WidgetTextChangeReason.Apply, forward );
		return new WidgetTextEditResult( WidgetTextEditStatus.Applied, revision, stateId, context.ViewId,
			new WidgetTextSelection( context.AfterAnchor, context.AfterCaret ) );
	}

	public WidgetTextEditResult Undo() {
		WidgetTextHistory.Group? available = this.history.UndoGroup; if ( available is null ) return this.Result( WidgetTextEditStatus.NoChange );
		long revision;
		try { revision = checked( this.Revision + 1 ); } catch ( OverflowException ) { return this.Result( WidgetTextEditStatus.LimitExceeded ); }
		WidgetTextHistory.Group group = this.history.TakeUndo(); this.root = group.BeforeRoot; this.Revision = revision; this.StateId = group.BeforeStateId;
		this.coalescenceGeneration++; this.OnChanged( WidgetTextChangeReason.Undo, group.BackwardChanges );
		return new WidgetTextEditResult( WidgetTextEditStatus.Applied, revision, this.StateId, group.Context.ViewId,
			new WidgetTextSelection( group.Context.BeforeAnchor, group.Context.BeforeCaret ) );
	}

	public WidgetTextEditResult Redo() {
		WidgetTextHistory.Group? available = this.history.RedoGroup; if ( available is null ) return this.Result( WidgetTextEditStatus.NoChange );
		long revision;
		try { revision = checked( this.Revision + 1 ); } catch ( OverflowException ) { return this.Result( WidgetTextEditStatus.LimitExceeded ); }
		WidgetTextHistory.Group group = this.history.TakeRedo(); this.root = group.AfterRoot; this.Revision = revision; this.StateId = group.AfterStateId;
		this.coalescenceGeneration++; this.OnChanged( WidgetTextChangeReason.Redo, group.ForwardChanges );
		return new WidgetTextEditResult( WidgetTextEditStatus.Applied, revision, this.StateId, group.Context.ViewId,
			new WidgetTextSelection( group.Context.AfterAnchor, group.Context.AfterCaret ) );
	}

	public void MarkSaved( WidgetTextSnapshot snapshot ) {
		ArgumentNullException.ThrowIfNull( snapshot );
		if ( snapshot.Id != this.Id ) throw new ArgumentException( "The snapshot belongs to another document.", nameof( snapshot ) );
		this.savedStateId = snapshot.StateId; this.coalescenceGeneration++;
	}

	public WidgetTextSnapshot CreateSnapshot() {
		lock ( this.snapshotGate ) {
			if ( this.options.MaximumSnapshots <= this.liveSnapshots ) throw new InvalidOperationException( "The document snapshot limit has been reached." );
			this.liveSnapshots++;
		}
		try { return new WidgetTextSnapshot( this.Id, this.Revision, this.StateId, this.root, this.ReleaseSnapshot ); }
		catch { this.ReleaseSnapshot(); throw; }
	}

	private void ValidateRange( long offset, int length ) {
		if ( 0 > offset ) throw new ArgumentOutOfRangeException( nameof( offset ) );
		if ( 0 > length ) throw new ArgumentOutOfRangeException( nameof( length ) );
		if ( offset > this.Length || length > this.Length - offset ) throw new ArgumentOutOfRangeException( nameof( length ) );
	}

	private bool TryCoalesce( WidgetTextHistory.Group current, out WidgetTextHistory.Group result ) {
		WidgetTextHistory.Group? previous = this.history.LastApplied; result = current;
		if ( previous is null || !previous.Context.AllowCoalescing || !current.Context.AllowCoalescing ||
			previous.Context.Kind != current.Context.Kind || previous.Context.ViewId != current.Context.ViewId ||
			previous.CoalescenceGeneration != current.CoalescenceGeneration || current.Context.Timestamp - previous.Context.Timestamp > TimeSpan.FromMilliseconds( 750 ) ||
			previous.Context.AfterAnchor != current.Context.BeforeAnchor || previous.Context.AfterCaret != current.Context.BeforeCaret ||
			previous.Shape is not { } oldShape || current.Shape is not { } newShape || !TryMergeShape( current.Context.Kind, oldShape, newShape, out WidgetTextHistory.EditShape mergedShape ) ||
			previous.RetainedUnits > this.options.MaximumHistoryRetainedUnits - current.RetainedUnits ) return false;
		WidgetTextChange mergedChange = new( mergedShape.Start, mergedShape.DeleteLength, mergedShape.InsertLength );
		WidgetTextEditContext mergedContext = previous.Context with {
			AfterAnchor = current.Context.AfterAnchor, AfterCaret = current.Context.AfterCaret, Timestamp = current.Context.Timestamp
		};
		result = new WidgetTextHistory.Group( previous.BeforeRoot, current.AfterRoot, previous.BeforeStateId, current.AfterStateId,
			mergedContext, [ mergedChange ], [ new WidgetTextChange( mergedShape.Start, mergedShape.InsertLength, mergedShape.DeleteLength ) ],
			previous.RetainedUnits + current.RetainedUnits, current.CoalescenceGeneration, mergedShape );
		return true;
	}

	private bool IsSelectionValid( WidgetPieceTree.Node? candidate, long anchor, long caret ) =>
		this.IsTextElementBoundary( candidate, anchor ) && this.IsTextElementBoundary( candidate, caret );

	private bool IsTextElementBoundary( WidgetPieceTree.Node? candidate, long offset ) {
		long length = candidate?.Length ?? 0; if ( offset < 0 || offset > length ) return false; if ( 0 == offset || offset == length ) return true;
		char previous = WidgetPieceTree.CharacterAt( candidate!, offset - 1 ); char current = WidgetPieceTree.CharacterAt( candidate!, offset );
		if ( '\r' == previous && '\n' == current || char.IsHighSurrogate( previous ) && char.IsLowSurrogate( current ) ) return false;
		if ( previous < 0x80 && current < 0x80 ) return true;
		int scan = this.options.MaximumTextElementScan; long start = Math.Max( 0, offset - scan ); long end = Math.Min( length, offset + scan );
		string slice = WidgetPieceTree.Read( candidate, start, checked( (int)( end - start ) ) ); int local = checked( (int)( offset - start ) );
		return 0 <= Array.BinarySearch( StringInfo.ParseCombiningCharacters( slice ), local );
	}

	private static bool ExceedsFragmentationLimit( WidgetPieceTree.Node? candidate ) {
		if ( candidate is null ) return false;
		long scaled = candidate.Length / 8; long limit = Math.Max( 4_096, Math.Min( 1_048_576, scaled ) ); return candidate.LeafCount > limit;
	}

	private static ( IReadOnlyList<WidgetTextChange> Forward, IReadOnlyList<WidgetTextChange> Backward ) BuildChanges( IReadOnlyList<WidgetTextEdit> edits ) {
		List<WidgetTextChange> forward = []; List<WidgetTextChange> backward = []; long delta = 0;
		foreach ( WidgetTextEdit edit in edits ) {
			forward.Add( new WidgetTextChange( edit.Start, edit.DeleteLength, edit.InsertText.Length ) ); long postStart = checked( edit.Start + delta );
			backward.Add( new WidgetTextChange( postStart, edit.InsertText.Length, edit.DeleteLength ) ); delta = checked( delta + edit.InsertText.Length - edit.DeleteLength );
		}
		return ( forward.ToArray(), backward.ToArray() );
	}

	private static WidgetTextHistory.EditShape? GetShape( IReadOnlyList<WidgetTextEdit> edits ) {
		if ( 1 != edits.Count ) return null; WidgetTextEdit edit = edits[ 0 ];
		return 0 == edit.DeleteLength || 0 == edit.InsertText.Length ? new WidgetTextHistory.EditShape( edit.Start, edit.DeleteLength, edit.InsertText.Length ) : null;
	}

	private static bool TryMergeShape( WidgetTextEditKind kind, WidgetTextHistory.EditShape previous, WidgetTextHistory.EditShape current, out WidgetTextHistory.EditShape merged ) {
		merged = default;
		if ( WidgetTextEditKind.Typing == kind && 0 == previous.DeleteLength && 0 == current.DeleteLength && 0 < previous.InsertLength && 0 < current.InsertLength &&
			current.Start == previous.Start + previous.InsertLength ) merged = new WidgetTextHistory.EditShape( previous.Start, 0, previous.InsertLength + current.InsertLength );
		else if ( WidgetTextEditKind.Backspace == kind && 0 == previous.InsertLength && 0 == current.InsertLength && 0 < previous.DeleteLength && 0 < current.DeleteLength &&
			current.Start + current.DeleteLength == previous.Start ) merged = new WidgetTextHistory.EditShape( current.Start, previous.DeleteLength + current.DeleteLength, 0 );
		else if ( WidgetTextEditKind.Delete == kind && 0 == previous.InsertLength && 0 == current.InsertLength && 0 < previous.DeleteLength && 0 < current.DeleteLength &&
			current.Start == previous.Start ) merged = new WidgetTextHistory.EditShape( previous.Start, previous.DeleteLength + current.DeleteLength, 0 );
		return 0 != merged.DeleteLength || 0 != merged.InsertLength;
	}

	private WidgetTextEditResult Result( WidgetTextEditStatus status ) => new( status, this.Revision, this.StateId );
	private void OnChanged( WidgetTextChangeReason reason, IReadOnlyList<WidgetTextChange> changes ) =>
		this.Changed?.Invoke( this, new WidgetTextDocumentChangedEventArgs( this.Id, this.Revision, this.StateId, reason, changes ) );

	private void ReleaseSnapshot() { lock ( this.snapshotGate ) { if ( 0 < this.liveSnapshots ) this.liveSnapshots--; } }
}
