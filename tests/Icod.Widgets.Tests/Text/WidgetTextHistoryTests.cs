/*
	Icod.Widgets.Tests
	Automated tests for the Icod.Widgets library.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.Widgets.Tests.Text;

using Xunit;

public sealed class WidgetTextHistoryTests {
	private static readonly Guid ViewId = new( "11111111-1111-1111-1111-111111111111" );

	[Fact]
	public void BatchIsAtomic() {
		WidgetTextDocument document = Create( "abcdef" );
		WidgetTextEditResult result = document.Apply( [
			new WidgetTextEdit( 1, 2, "X" ), new WidgetTextEdit( 2, 1, "Y" )
		], document.Revision, Context( WidgetTextEditKind.Programmatic, 0, 0, 0, 0 ) );

		Assert.Equal( WidgetTextEditStatus.InvalidRange, result.Status );
		Assert.Equal( "abcdef", Text( document ) );
		Assert.Equal( 0, document.Revision );
	}

	[Fact]
	public void BatchUsesCommonRevisionCoordinates() {
		WidgetTextDocument document = Create( "abcdef" );
		WidgetTextEditResult result = document.Apply( [
			new WidgetTextEdit( 1, 1, "X" ), new WidgetTextEdit( 4, 1, "YZ" )
		], 0, Context( WidgetTextEditKind.Programmatic, 0, 0, 0, 0 ) );

		Assert.Equal( WidgetTextEditStatus.Applied, result.Status );
		Assert.Equal( "aXcdYZf", Text( document ) );
		document.Undo();
		Assert.Equal( "abcdef", Text( document ) );
	}

	[Fact]
	public void OnePasteOneUndo() {
		WidgetTextDocument document = Create( "ac" );
		WidgetTextEditResult applied = document.Apply( [ new WidgetTextEdit( 1, 0, "B\r\n" ) ], 0,
			Context( WidgetTextEditKind.Paste, 1, 1, 4, 4 ) );

		WidgetTextEditResult undone = document.Undo();

		Assert.Equal( WidgetTextEditStatus.Applied, applied.Status );
		Assert.Equal( "ac", Text( document ) );
		Assert.Equal( new WidgetTextSelection( 1, 1 ), undone.Selection );
		Assert.Equal( ViewId, undone.ViewId );
	}

	[Fact]
	public void UndoBranchDoesNotReuseStateId() {
		WidgetTextDocument document = Create( "" );
		long first = ApplyInsert( document, "a", TimeSpan.Zero ).StateId;
		long abandoned = ApplyInsert( document, "b", TimeSpan.FromMilliseconds( 1 ) ).StateId;
		document.Undo();

		long branch = ApplyInsert( document, "c", TimeSpan.FromMilliseconds( 2 ) ).StateId;

		Assert.Equal( first, document.Undo().StateId );
		Assert.True( branch > abandoned );
		Assert.Equal( WidgetTextEditStatus.Applied, document.Redo().Status );
		Assert.Equal( "ac", Text( document ) );
		Assert.Equal( WidgetTextEditStatus.NoChange, document.Redo().Status );
	}

	[Fact]
	public void UndoToCheckpointClearsModified() {
		WidgetTextDocument document = Create( "a" );
		ApplyInsert( document, "b", TimeSpan.Zero );
		using WidgetTextSnapshot saved = document.CreateSnapshot();
		document.MarkSaved( saved );
		Assert.False( document.IsModified );
		ApplyInsert( document, "c", TimeSpan.FromMilliseconds( 1 ) );

		document.Undo();

		Assert.Equal( "ab", Text( document ) );
		Assert.False( document.IsModified );
	}

	[Fact]
	public void LateSaveDoesNotCleanNewerText() {
		WidgetTextDocument document = Create( "a" );
		ApplyInsert( document, "b", TimeSpan.Zero );
		using WidgetTextSnapshot saving = document.CreateSnapshot();
		ApplyInsert( document, "c", TimeSpan.FromMilliseconds( 1 ) );

		document.MarkSaved( saving );

		Assert.True( document.IsModified );
		Assert.Equal( "abc", Text( document ) );
	}

	[Fact]
	public void ForeignSnapshotCannotBecomeCheckpoint() {
		WidgetTextDocument document = Create( "one" ); WidgetTextDocument other = Create( "two" );
		using WidgetTextSnapshot snapshot = other.CreateSnapshot();

		Assert.Throws<ArgumentException>( () => document.MarkSaved( snapshot ) );
		Assert.False( document.IsModified );
	}

	[Fact]
	public void StaleRevisionDoesNotMutate() {
		WidgetTextDocument document = Create( "a" ); ApplyInsert( document, "b", TimeSpan.Zero );

		WidgetTextEditResult result = document.Apply( [ new WidgetTextEdit( 0, 1, "x" ) ], 0,
			Context( WidgetTextEditKind.Programmatic, 0, 0, 1, 1, TimeSpan.FromMilliseconds( 1 ) ) );

		Assert.Equal( WidgetTextEditStatus.Stale, result.Status );
		Assert.Equal( "ab", Text( document ) );
		Assert.Equal( 1, document.Revision );
	}

	[Fact]
	public void TrimDoesNotCleanDocument() {
		WidgetTextDocument document = new( [ "".AsMemory() ], new WidgetTextDocumentOptions { MaximumHistoryGroups = 1 } );
		ApplyInsert( document, "a", TimeSpan.Zero );
		ApplyInsert( document, "b", TimeSpan.FromMilliseconds( 1 ) );

		document.Undo();

		Assert.Equal( "a", Text( document ) );
		Assert.True( document.IsModified );
		Assert.Equal( WidgetTextEditStatus.NoChange, document.Undo().Status );
	}

	[Fact]
	public void OversizedUndoGroupRejectsBeforeMutation() {
		WidgetTextDocument document = new( [ "ab".AsMemory() ], new WidgetTextDocumentOptions { MaximumHistoryRetainedUnits = 3 } );

		WidgetTextEditResult result = document.Apply( [ new WidgetTextEdit( 0, 2, "cd" ) ], 0,
			Context( WidgetTextEditKind.Programmatic, 0, 0, 2, 2 ) );

		Assert.Equal( WidgetTextEditStatus.LimitExceeded, result.Status );
		Assert.Equal( "ab", Text( document ) );
		Assert.Equal( 0, document.Revision );
	}

	[Fact]
	public void BoundaryFailureDoesNotSplitClusterOrCrLf() {
		const string value = "a\r\ne\u0301\ud83d\ude42";
		WidgetTextDocument document = Create( value );
		WidgetTextEdit[] invalid = [
			new WidgetTextEdit( 2, 1, "" ),
			new WidgetTextEdit( 4, 1, "" ),
			new WidgetTextEdit( 6, 1, "" )
		];

		foreach ( WidgetTextEdit edit in invalid ) {
			WidgetTextEditResult result = document.Apply( [ edit ], document.Revision,
				Context( WidgetTextEditKind.Delete, edit.Start, edit.Start, edit.Start, edit.Start ) );
			Assert.Equal( WidgetTextEditStatus.InvalidRange, result.Status );
			Assert.Equal( value, Text( document ) );
		}
	}

	[Fact]
	public void SnapshotSurvivesLaterEditsAndUndo() {
		WidgetTextDocument document = Create( "original" );
		using WidgetTextSnapshot snapshot = document.CreateSnapshot();
		document.Apply( [ new WidgetTextEdit( 0, 8, "changed" ) ], 0, Context( WidgetTextEditKind.Programmatic, 0, 0, 7, 7 ) );
		document.Undo(); document.Redo();

		Assert.Equal( "original", snapshot.Read( 0, 8 ) );
		Assert.Equal( "changed", Text( document ) );
	}

	[Fact]
	public void ExplicitTypingCoalescesAndCheckpointBreaksTheGroup() {
		WidgetTextDocument document = Create( "" );
		ApplyTyping( document, "a", TimeSpan.Zero );
		ApplyTyping( document, "b", TimeSpan.FromMilliseconds( 100 ) );
		using WidgetTextSnapshot checkpoint = document.CreateSnapshot(); document.MarkSaved( checkpoint );
		ApplyTyping( document, "c", TimeSpan.FromMilliseconds( 200 ) );

		document.Undo();
		Assert.Equal( "ab", Text( document ) );
		document.Undo();
		Assert.Equal( "", Text( document ) );
	}

	[Fact]
	public void ChangeNotificationObservesCommittedState() {
		WidgetTextDocument document = Create( "abc" ); WidgetTextDocumentChangedEventArgs? observed = null;
		document.Changed += ( _, args ) => { Assert.Equal( args.Revision, document.Revision ); Assert.Equal( "aXc", Text( document ) ); observed = args; };

		document.Apply( [ new WidgetTextEdit( 1, 1, "X" ) ], 0, Context( WidgetTextEditKind.Programmatic, 1, 1, 2, 2 ) );

		Assert.NotNull( observed );
		Assert.Equal( new WidgetTextChange( 1, 1, 1 ), Assert.Single( observed.Changes ) );
	}

	[Fact]
	public void RandomizedEditUndoRedoMatchesStringOracle() {
		Random random = new( 1105 ); WidgetTextDocument document = Create( "seed\n" );
		List<string> states = [ "seed\n" ]; int cursor = 0; TimeSpan timestamp = TimeSpan.Zero;
		for ( int iteration = 0; iteration < 500; iteration++ ) {
			int operation = random.Next( 5 );
			if ( operation < 3 ) {
				string current = states[ cursor ]; int start = random.Next( current.Length + 1 ); int delete = start < current.Length ? random.Next( Math.Min( 2, current.Length - start ) + 1 ) : 0;
				while ( delete > 0 && !IsSimpleBoundary( current, start + delete ) ) delete--;
				while ( start > 0 && !IsSimpleBoundary( current, start ) ) start--;
				string insert = random.Next( 4 ) switch { 0 => "x", 1 => "\n", 2 => "yz", _ => "" };
				timestamp += TimeSpan.FromMilliseconds( 1 );
				WidgetTextEditResult result = document.Apply( [ new WidgetTextEdit( start, delete, insert ) ], document.Revision,
					Context( WidgetTextEditKind.Programmatic, start, start, start + insert.Length, start + insert.Length, timestamp ) );
				if ( WidgetTextEditStatus.Applied == result.Status ) { states.RemoveRange( cursor + 1, states.Count - cursor - 1 ); states.Add( current.Remove( start, delete ).Insert( start, insert ) ); cursor++; }
			} else if ( 3 == operation ) { WidgetTextEditResult result = document.Undo(); if ( WidgetTextEditStatus.Applied == result.Status ) cursor--; }
			else { WidgetTextEditResult result = document.Redo(); if ( WidgetTextEditStatus.Applied == result.Status ) cursor++; }
			Assert.Equal( states[ cursor ], Text( document ) );
			Assert.Equal( 1 + states[ cursor ].Count( static value => '\n' == value ), document.LineCount );
		}
	}

	[Fact]
	public void FragmentationMaintenanceKeepsDocumentEditable() {
		WidgetTextDocument document = Create( "" ); TimeSpan timestamp = TimeSpan.Zero;
		for ( int index = 0; index < 4_200; index++ ) {
			timestamp += TimeSpan.FromTicks( 1 );
			WidgetTextEditResult result = document.Apply( [ new WidgetTextEdit( 0, 0, "x" ) ], document.Revision,
				Context( WidgetTextEditKind.Programmatic, 0, 0, 1, 1, timestamp ) );
			Assert.Equal( WidgetTextEditStatus.Applied, result.Status );
		}

		Assert.Equal( 4_200, document.Length );
		Assert.Equal( new string( 'x', 4_200 ), Text( document ) );
	}

	private static WidgetTextEditResult ApplyInsert( WidgetTextDocument document, string value, TimeSpan timestamp ) {
		long offset = document.Length; return document.Apply( [ new WidgetTextEdit( offset, 0, value ) ], document.Revision,
			Context( WidgetTextEditKind.Programmatic, offset, offset, offset + value.Length, offset + value.Length, timestamp ) );
	}

	private static WidgetTextEditResult ApplyTyping( WidgetTextDocument document, string value, TimeSpan timestamp ) {
		long offset = document.Length; return document.Apply( [ new WidgetTextEdit( offset, 0, value ) ], document.Revision,
			Context( WidgetTextEditKind.Typing, offset, offset, offset + value.Length, offset + value.Length, timestamp, true ) );
	}

	private static WidgetTextEditContext Context( WidgetTextEditKind kind, long beforeAnchor, long beforeCaret, long afterAnchor, long afterCaret,
		TimeSpan? timestamp = null, bool allowCoalescing = false ) =>
		new( ViewId, beforeAnchor, beforeCaret, afterAnchor, afterCaret, kind, timestamp ?? TimeSpan.Zero, allowCoalescing );

	private static WidgetTextDocument Create( string text ) => new( [ text.AsMemory() ] );
	private static string Text( WidgetTextDocument document ) => document.Read( 0, checked( (int)document.Length ) );
	private static bool IsSimpleBoundary( string text, int offset ) => offset <= 0 || offset >= text.Length || !( '\r' == text[ offset - 1 ] && '\n' == text[ offset ] );
}
