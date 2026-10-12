/*
	EditorMeasurements
	Measures candidate text stores for the Icod.Widgets editor foundation.
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

using System.Diagnostics;
using Icod.DCurses;
using Icod.Widgets;
using Icod.Widgets.EditorMeasurements;

string[] fixtures = [
	new string( 'a', 4096 ),
	string.Concat( Enumerable.Range( 0, 100_000 ).Select( static index => $"line-{index:D6}\r\n" ) ),
	new string( 'x', 10_000_000 ),
	"alpha\r\nbeta\ngamma\rδelta\t👩‍💻e\u0301\n"
];

foreach ( string fixture in fixtures ) {
	int edits = fixture.Length > 1_000_000 ? 64 : 2_000;
	List<(int Offset, int DeleteLength, string InsertText)> operations = [];
	int traceLength = fixture.Length;
	for ( int index = 0; index < edits; index++ ) {
		int offset = 0 == traceLength ? 0 : ( index % 2 == 0 ? Math.Min( traceLength, index % 1024 ) : traceLength - Math.Min( traceLength, index % 1024 ) );
		string insertion = index % 7 == 0 ? "👩‍💻" : "z";
		operations.Add( ( offset, 0, insertion ) );
		traceLength += insertion.Length;
		if ( index % 5 == 0 && traceLength > insertion.Length ) {
			int deleteAt = Math.Min( traceLength - 1, offset );
			operations.Add( ( deleteAt, 1, "" ) );
			traceLength--;
		}
	}
	string oracle = fixture;
	foreach ( ( int offset, int deleteLength, string insertText ) in operations ) oracle = oracle.Remove( offset, deleteLength ).Insert( offset, insertText );

	foreach ( Func<string, IStorageCandidate> factory in new Func<string, IStorageCandidate>[] {
		static text => new GapBufferCandidate( text ), static text => new PieceTableCandidate( text )
	} ) {
		IStorageCandidate candidate = factory( fixture );
		List<object> retainedSnapshots = [];
		long before = GC.GetAllocatedBytesForCurrentThread();
		Stopwatch stopwatch = Stopwatch.StartNew();
		for ( int index = 0; index < operations.Count; index++ ) {
			( int offset, int deleteLength, string insertText ) = operations[ index ];
			candidate.Replace( offset, deleteLength, insertText );
			if ( index % Math.Max( 1, operations.Count / 8 ) == 0 ) retainedSnapshots.Add( candidate.CaptureSnapshot() );
		}
		int sliceLength = Math.Min( 65_536, candidate.Length );
		_ = candidate.Read( Math.Max( 0, candidate.Length / 2 - sliceLength / 2 ), sliceLength );
		object snapshot = candidate.CaptureSnapshot();
		stopwatch.Stop();
		long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		if ( !StringComparer.Ordinal.Equals( oracle, candidate.SnapshotText( snapshot ) ) ) throw new InvalidOperationException( $"{candidate.Name} diverged from the string oracle." );
		Console.WriteLine( $"{candidate.Name,-12} fixture={fixture.Length,10} operations={operations.Count,5} elapsed-ms={stopwatch.Elapsed.TotalMilliseconds,9:F2} allocated={allocated,12} snapshots={retainedSnapshots.Count + 1,2} fragments={candidate.FragmentCount,6}" );
	}
}

Console.WriteLine( "production piece tree:" );
foreach ( string fixture in fixtures.Skip( 1 ) ) {
	IEnumerable<ReadOnlyMemory<char>> chunks = Chunk( fixture, 4_093 );
	long before = GC.GetAllocatedBytesForCurrentThread(); Stopwatch stopwatch = Stopwatch.StartNew();
	WidgetTextDocument document = new( chunks ); stopwatch.Stop(); long loadAllocation = GC.GetAllocatedBytesForCurrentThread() - before;
	before = GC.GetAllocatedBytesForCurrentThread();
	WidgetTextSnapshot[] snapshots = Enumerable.Range( 0, 16 ).Select( _ => document.CreateSnapshot() ).ToArray();
	long snapshotAllocation = GC.GetAllocatedBytesForCurrentThread() - before;
	WidgetTextLine finalLine = document.GetLine( document.LineCount - 1 );
	foreach ( WidgetTextSnapshot snapshot in snapshots ) snapshot.Dispose();
	Console.WriteLine( $"piece-tree   fixture={fixture.Length,10} lines={document.LineCount,7} load-ms={stopwatch.Elapsed.TotalMilliseconds,9:F2} load-allocated={loadAllocation,12} snapshots=16 snapshot-allocated={snapshotAllocation,8} final-line-start={finalLine.Start}" );
}

static IEnumerable<ReadOnlyMemory<char>> Chunk( string value, int size ) {
	for ( int offset = 0; offset < value.Length; offset += size ) yield return value.AsMemory( offset, Math.Min( size, value.Length - offset ) );
}

const int historyOperations = 5_000;
string historyFixture = fixtures[ 1 ]; WidgetTextDocument historyDocument = new( Chunk( historyFixture, 4_093 ) );
using WidgetTextSnapshot originalSnapshot = historyDocument.CreateSnapshot(); Guid measurementView = new( "22222222-2222-2222-2222-222222222222" );
long historyBefore = GC.GetAllocatedBytesForCurrentThread(); Stopwatch historyWatch = Stopwatch.StartNew();
for ( int index = 0; index < historyOperations; index++ ) {
	long offset = 0 == index % 2 ? 0 : historyDocument.Length;
	WidgetTextEditResult result = historyDocument.Apply( [ new WidgetTextEdit( offset, 0, "z" ) ], historyDocument.Revision,
		new WidgetTextEditContext( measurementView, offset, offset, offset + 1, offset + 1, WidgetTextEditKind.Programmatic, TimeSpan.FromTicks( index ) ) );
	if ( WidgetTextEditStatus.Applied != result.Status ) throw new InvalidOperationException( $"Production history measurement failed: {result.Status}." );
}
int undoCount = 0; while ( WidgetTextEditStatus.Applied == historyDocument.Undo().Status ) undoCount++;
historyWatch.Stop(); long historyAllocation = GC.GetAllocatedBytesForCurrentThread() - historyBefore;
if ( !StringComparer.Ordinal.Equals( historyFixture, originalSnapshot.Read( 0, historyFixture.Length ) ) ) throw new InvalidOperationException( "A retained production snapshot changed." );
Console.WriteLine( $"history      fixture={historyFixture.Length,10} edits={historyOperations,5} undo-groups={undoCount,5} elapsed-ms={historyWatch.Elapsed.TotalMilliseconds,9:F2} allocated={historyAllocation,12} retained-snapshot=exact" );

Console.WriteLine( "production editor stack:" );
foreach ( (string name, string text, long horizontalOffset) in new[] {
	( "many-lines", fixtures[ 1 ], 0L ),
	( "long-line", fixtures[ 2 ], 9_999_900L ),
	( "long-cluster", "a" + new string( '\u0301', 100_000 ) + "z", 0L )
} ) {
	long retainedBefore = GC.GetTotalMemory( true ); long allocatedBefore = GC.GetAllocatedBytesForCurrentThread(); Stopwatch cold = Stopwatch.StartNew();
	WidgetTextDocument document = new( Chunk( text, 4_093 ) ); using WidgetTextViewState view = new( document ); view.SetScroll( horizontalOffset, 0 );
	WidgetTextEditor editor = new( "editor", view ); using WidgetHost host = new( new CursesScreen( 120, 30 ), WidgetTheme.Default ); host.Attach( editor ); host.Arrange( host.Screen.Bounds ); host.Focus( editor ); host.Render(); cold.Stop();
	long coldAllocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore; int seekPasses = 0;
	if ( 0 < horizontalOffset ) for ( ; seekPasses < 160; seekPasses++ ) host.Render();
	GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); allocatedBefore = GC.GetAllocatedBytesForCurrentThread(); Stopwatch warm = Stopwatch.StartNew();
	for ( int index = 0; index < 100; index++ ) { host.Update( TimeSpan.FromMilliseconds( index ) ); host.Render(); }
	warm.Stop(); long warmAllocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore; long retainedAfter = GC.GetTotalMemory( true );
	Console.WriteLine( $"editor       fixture={name,-12} units={text.Length,10} cold-ms={cold.Elapsed.TotalMilliseconds,9:F2} cold-allocated={coldAllocated,12} seek-passes={seekPasses,3} warm100-ms={warm.Elapsed.TotalMilliseconds,9:F2} warm-allocated={warmAllocated,12} retained-delta={retainedAfter - retainedBefore,12}" );
}

WidgetTextDocumentOptions trimOptions = new() { MaximumHistoryGroups = 32, MaximumHistoryRetainedUnits = 4_096 };
WidgetTextDocument trimDocument = new( [ "seed".AsMemory() ], trimOptions ); Guid trimView = new( "33333333-3333-3333-3333-333333333333" );
using WidgetTextSnapshot retained = trimDocument.CreateSnapshot(); Stopwatch pasteWatch = Stopwatch.StartNew(); long pasteBefore = GC.GetAllocatedBytesForCurrentThread();
for ( int index = 0; index < 256; index++ ) {
	long offset = trimDocument.Length; WidgetTextEditResult result = trimDocument.Apply( [ new WidgetTextEdit( offset, 0, "paste\n" ) ], trimDocument.Revision,
		new WidgetTextEditContext( trimView, offset, offset, offset + 6, offset + 6, WidgetTextEditKind.Paste, TimeSpan.FromTicks( index ) ) );
	if ( WidgetTextEditStatus.Applied != result.Status ) throw new InvalidOperationException( $"Paste/trim measurement failed: {result.Status}." );
}
int trimmedUndo = 0; while ( WidgetTextEditStatus.Applied == trimDocument.Undo().Status ) trimmedUndo++; pasteWatch.Stop();
if ( retained.Read( 0, 4 ) != "seed" ) throw new InvalidOperationException( "Retained snapshot changed during paste/trim measurement." );
Console.WriteLine( $"paste-trim   pastes={256,5} undo-groups-retained={trimmedUndo,3} elapsed-ms={pasteWatch.Elapsed.TotalMilliseconds,9:F2} allocated={GC.GetAllocatedBytesForCurrentThread() - pasteBefore,12} retained-snapshot=exact" );
