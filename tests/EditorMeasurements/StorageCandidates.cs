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

namespace Icod.Widgets.EditorMeasurements;

internal interface IStorageCandidate {
	string Name { get; }
	int Length { get; }
	int FragmentCount { get; }
	string Read( int offset, int length );
	void Replace( int offset, int deleteLength, string insertText );
	object CaptureSnapshot();
	string SnapshotText( object snapshot );
}

internal sealed class GapBufferCandidate : IStorageCandidate {
	private char[] buffer;
	private int gapStart;
	private int gapEnd;

	internal GapBufferCandidate( string text ) {
		this.buffer = new char[ Math.Max( text.Length + 256, 512 ) ];
		text.CopyTo( 0, this.buffer, 0, text.Length );
		this.gapStart = text.Length;
		this.gapEnd = this.buffer.Length;
	}

	public string Name => "gap-buffer";
	public int Length => this.buffer.Length - ( this.gapEnd - this.gapStart );
	public int FragmentCount => 1;

	public string Read( int offset, int length ) {
		if ( offset < 0 || length < 0 || offset > this.Length - length ) throw new ArgumentOutOfRangeException();
		char[] result = new char[ length ];
		int gapLength = this.gapEnd - this.gapStart;
		for ( int index = 0; index < length; index++ ) {
			int source = offset + index;
			result[ index ] = this.buffer[ source < this.gapStart ? source : source + gapLength ];
		}
		return new string( result );
	}

	public void Replace( int offset, int deleteLength, string insertText ) {
		ArgumentNullException.ThrowIfNull( insertText );
		if ( offset < 0 || deleteLength < 0 || offset > this.Length - deleteLength ) throw new ArgumentOutOfRangeException();
		this.MoveGap( offset );
		this.gapEnd += deleteLength;
		this.EnsureGap( insertText.Length );
		insertText.CopyTo( 0, this.buffer, this.gapStart, insertText.Length );
		this.gapStart += insertText.Length;
	}

	public object CaptureSnapshot() => string.Concat(
		new string( this.buffer, 0, this.gapStart ),
		new string( this.buffer, this.gapEnd, this.buffer.Length - this.gapEnd ) );
	public string SnapshotText( object snapshot ) => (string)snapshot;

	private void MoveGap( int offset ) {
		if ( offset < this.gapStart ) {
			int count = this.gapStart - offset;
			Array.Copy( this.buffer, offset, this.buffer, this.gapEnd - count, count );
			this.gapStart -= count;
			this.gapEnd -= count;
		} else if ( offset > this.gapStart ) {
			int count = offset - this.gapStart;
			Array.Copy( this.buffer, this.gapEnd, this.buffer, this.gapStart, count );
			this.gapStart += count;
			this.gapEnd += count;
		}
	}

	private void EnsureGap( int count ) {
		int gap = this.gapEnd - this.gapStart;
		if ( gap >= count ) return;
		int newLength = Math.Max( checked( this.Length + count + 256 ), checked( this.buffer.Length * 2 ) );
		char[] replacement = new char[ newLength ];
		Array.Copy( this.buffer, 0, replacement, 0, this.gapStart );
		int suffix = this.buffer.Length - this.gapEnd;
		int replacementGapEnd = newLength - suffix;
		Array.Copy( this.buffer, this.gapEnd, replacement, replacementGapEnd, suffix );
		this.buffer = replacement;
		this.gapEnd = replacementGapEnd;
	}
}

internal sealed class PieceTableCandidate : IStorageCandidate {
	private enum Source { Original, Added }
	private readonly record struct Piece( Source Source, int Start, int Length );
	private readonly string original;
	private readonly List<string> additions = [];
	private readonly List<Piece> pieces = [];
	private sealed record PieceSnapshot( Piece[] Pieces, string[] Additions );

	internal PieceTableCandidate( string text ) {
		this.original = text;
		if ( text.Length > 0 ) this.pieces.Add( new Piece( Source.Original, 0, text.Length ) );
	}

	public string Name => "piece-table";
	public int Length => this.pieces.Sum( static piece => piece.Length );
	public int FragmentCount => this.pieces.Count;

	public string Read( int offset, int length ) {
		if ( offset < 0 || length < 0 || offset > this.Length - length ) throw new ArgumentOutOfRangeException();
		char[] result = new char[ length ];
		int sourcePosition = 0;
		int resultPosition = 0;
		foreach ( Piece piece in this.pieces ) {
			int localStart = Math.Max( 0, offset - sourcePosition );
			int available = piece.Length - localStart;
			if ( available > 0 && sourcePosition + piece.Length > offset ) {
				int count = Math.Min( available, length - resultPosition );
				string source = this.GetSource( piece, this.additions );
				source.CopyTo( piece.Start + localStart, result, resultPosition, count );
				resultPosition += count;
				if ( resultPosition == length ) break;
			}
			sourcePosition += piece.Length;
		}
		return new string( result );
	}

	public void Replace( int offset, int deleteLength, string insertText ) {
		ArgumentNullException.ThrowIfNull( insertText );
		if ( offset < 0 || deleteLength < 0 || offset > this.Length - deleteLength ) throw new ArgumentOutOfRangeException();
		this.SplitAt( offset );
		this.SplitAt( offset + deleteLength );
		int position = 0;
		int insertionIndex = 0;
		while ( insertionIndex < this.pieces.Count && position < offset ) position += this.pieces[ insertionIndex++ ].Length;
		int remaining = deleteLength;
		while ( remaining > 0 ) {
			Piece removed = this.pieces[ insertionIndex ];
			remaining -= removed.Length;
			this.pieces.RemoveAt( insertionIndex );
		}
		if ( insertText.Length > 0 ) {
			int addition = this.additions.Count;
			this.additions.Add( insertText );
			this.pieces.Insert( insertionIndex, new Piece( (Source)( addition + 1 ), 0, insertText.Length ) );
		}
	}

	public object CaptureSnapshot() => new PieceSnapshot( this.pieces.ToArray(), this.additions.ToArray() );

	public string SnapshotText( object snapshot ) {
		PieceSnapshot captured = (PieceSnapshot)snapshot;
		char[] result = new char[ captured.Pieces.Sum( static piece => piece.Length ) ];
		int position = 0;
		foreach ( Piece piece in captured.Pieces ) {
			string source = this.GetSource( piece, captured.Additions );
			source.CopyTo( piece.Start, result, position, piece.Length );
			position += piece.Length;
		}
		return new string( result );
	}

	private string GetSource( Piece piece, IReadOnlyList<string> additions ) =>
		piece.Source == Source.Original ? this.original : additions[ (int)piece.Source - 1 ];

	private void SplitAt( int offset ) {
		if ( offset <= 0 || offset >= this.Length ) return;
		int position = 0;
		for ( int index = 0; index < this.pieces.Count; index++ ) {
			Piece piece = this.pieces[ index ];
			if ( offset > position && offset < position + piece.Length ) {
				int left = offset - position;
				this.pieces[ index ] = piece with { Length = left };
				this.pieces.Insert( index + 1, piece with { Start = piece.Start + left, Length = piece.Length - left } );
				return;
			}
			position += piece.Length;
		}
	}
}
