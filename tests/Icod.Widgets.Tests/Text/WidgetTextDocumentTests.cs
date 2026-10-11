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

public sealed class WidgetTextDocumentTests {
	[Fact]
	public void ChunkBoundariesPreserveExactText() {
		const string expected = "a\r\n\ud83d\ude42e\u0301\nb\rc";
		for ( int split = 0; split <= expected.Length; split++ ) {
			WidgetTextDocument document = new( [ expected.AsMemory( 0, split ), expected.AsMemory( split ) ] );
			Assert.Equal( expected, document.Read( 0, expected.Length ) );
			Assert.Equal( 4, document.LineCount );
		}
	}

	[Fact]
	public void CrLfAcrossStorageLeavesIsOneEnding() {
		string text = new string( 'a', 65_535 ) + "\r\nnext";
		WidgetTextDocument document = Create( text );

		Assert.Equal( 2, document.LineCount );
		Assert.Equal( new WidgetTextLine( 0, 65_535, 2 ), document.GetLine( 0 ) );
		Assert.Equal( new WidgetTextLine( 65_537, 4, 0 ), document.GetLine( 1 ) );
	}

	[Fact]
	public void ManyStorageLeavesRetainIndexedLines() {
		string text = string.Concat( Enumerable.Range( 0, 20_000 ).Select( static index => $"{index:D6}\r\n" ) );
		WidgetTextDocument document = Create( text );

		Assert.Equal( 20_001, document.LineCount );
		Assert.Equal( new WidgetTextLine( 80_000, 6, 2 ), document.GetLine( 10_000 ) );
		Assert.Equal( text.Substring( 65_500, 400 ), document.Read( 65_500, 400 ) );
	}

	[Fact]
	public void MixedEndingsIndexCorrectly() {
		WidgetTextDocument document = Create( "a\r\nb\nc\rd" );

		Assert.Equal( 4, document.LineCount );
		Assert.Equal( new WidgetTextLine( 0, 1, 2 ), document.GetLine( 0 ) );
		Assert.Equal( new WidgetTextLine( 3, 1, 1 ), document.GetLine( 1 ) );
		Assert.Equal( new WidgetTextLine( 5, 1, 1 ), document.GetLine( 2 ) );
		Assert.Equal( new WidgetTextLine( 7, 1, 0 ), document.GetLine( 3 ) );
	}

	[Fact]
	public void EmptyAndFinalNewlineHaveCorrectLines() {
		WidgetTextDocument empty = Create( string.Empty );
		WidgetTextDocument finalNewline = Create( "x\n" );

		Assert.Equal( 1, empty.LineCount );
		Assert.Equal( new WidgetTextLine( 0, 0, 0 ), empty.GetLine( 0 ) );
		Assert.Equal( 2, finalNewline.LineCount );
		Assert.Equal( new WidgetTextLine( 0, 1, 1 ), finalNewline.GetLine( 0 ) );
		Assert.Equal( new WidgetTextLine( 2, 0, 0 ), finalNewline.GetLine( 1 ) );
	}

	[Fact]
	public void SnapshotDisposeReleasesRetention() {
		WidgetTextDocument document = new( [ "text".AsMemory() ], new WidgetTextDocumentOptions { MaximumSnapshots = 1 } );
		WidgetTextSnapshot first = document.CreateSnapshot();
		Assert.Equal( "text", first.Read( 0, 4 ) );
		first.Dispose();

		Assert.Throws<ObjectDisposedException>( () => first.Read( 0, 1 ) );
		using WidgetTextSnapshot second = document.CreateSnapshot();
		Assert.Equal( document.Id, second.Id );
	}

	[Fact]
	public void SnapshotLimitRejectsWithoutEviction() {
		WidgetTextDocument document = new( [ "text".AsMemory() ], new WidgetTextDocumentOptions { MaximumSnapshots = 1 } );
		using WidgetTextSnapshot first = document.CreateSnapshot();

		Assert.Throws<InvalidOperationException>( () => document.CreateSnapshot() );
		Assert.Equal( "text", first.Read( 0, 4 ) );
	}

	[Fact]
	public void MutableInputIsNotBorrowed() {
		char[] source = "before".ToCharArray();
		WidgetTextDocument document = new( [ source.AsMemory() ] );

		source.AsSpan().Fill( 'x' );

		Assert.Equal( "before", document.Read( 0, 6 ) );
	}

	[Fact]
	public void SeededReadsAndLinesMatchOracle() {
		Random random = new( 1104 );
		string[] tokens = [ "a", "\r", "\n", "\r\n", "\ud83d\ude42", "e\u0301", "\0" ];
		string text = string.Concat( Enumerable.Range( 0, 500 ).Select( _ => tokens[ random.Next( tokens.Length ) ] ) );
		List<ReadOnlyMemory<char>> chunks = [];
		for ( int offset = 0; offset < text.Length; ) { int count = Math.Min( random.Next( 1, 12 ), text.Length - offset ); chunks.Add( text.AsMemory( offset, count ) ); offset += count; }
		WidgetTextDocument document = new( chunks );
		IReadOnlyList<WidgetTextLine> expectedLines = ParseLines( text );

		Assert.Equal( expectedLines.Count, document.LineCount );
		for ( int index = 0; index < expectedLines.Count; index++ ) Assert.Equal( expectedLines[ index ], document.GetLine( index ) );
		for ( int iteration = 0; iteration < 100; iteration++ ) {
			int start = random.Next( text.Length + 1 ); int length = random.Next( text.Length - start + 1 );
			Assert.Equal( text.Substring( start, length ), document.Read( start, length ) );
		}
	}

	private static WidgetTextDocument Create( string text ) => new( [ text.AsMemory() ] );

	private static IReadOnlyList<WidgetTextLine> ParseLines( string text ) {
		List<WidgetTextLine> result = []; int start = 0;
		for ( int offset = 0; offset < text.Length; offset++ ) {
			int ending = text[ offset ] switch { '\r' when offset + 1 < text.Length && '\n' == text[ offset + 1 ] => 2, '\r' or '\n' => 1, _ => 0 };
			if ( 0 == ending ) continue;
			result.Add( new WidgetTextLine( start, offset - start, ending ) ); offset += ending - 1; start = offset + 1;
		}
		result.Add( new WidgetTextLine( start, text.Length - start, 0 ) ); return result;
	}
}
