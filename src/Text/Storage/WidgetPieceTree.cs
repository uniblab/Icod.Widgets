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

using System.Text;

internal static class WidgetPieceTree {
	private const int LeafTarget = 65_536;

	internal abstract class Node {
		protected Node( long length, long breakCount, bool startsWithLf, bool endsWithCr, int height, int leafCount ) {
			this.Length = length; this.BreakCount = breakCount; this.StartsWithLf = startsWithLf;
			this.EndsWithCr = endsWithCr; this.Height = height; this.LeafCount = leafCount;
		}
		internal long Length { get; }
		internal long BreakCount { get; }
		internal bool StartsWithLf { get; }
		internal bool EndsWithCr { get; }
		internal int Height { get; }
		internal int LeafCount { get; }
	}

	private sealed class Leaf : Node {
		internal Leaf( string buffer, int offset, int count ) : base(
			count, CountBreaks( buffer.AsSpan( offset, count ) ), '\n' == buffer[ offset ], '\r' == buffer[ offset + count - 1 ], 1, 1 ) {
			this.Buffer = buffer; this.Offset = offset; this.Count = count;
		}
		internal string Buffer { get; }
		internal int Offset { get; }
		internal int Count { get; }
	}

	private sealed class Branch : Node {
		internal Branch( Node left, Node right ) : base(
			checked( left.Length + right.Length ),
			checked( left.BreakCount + BreakCount( right, left.EndsWithCr ) ),
			left.StartsWithLf, right.EndsWithCr, 1 + Math.Max( left.Height, right.Height ),
			checked( left.LeafCount + right.LeafCount ) ) {
			this.Left = left; this.Right = right;
		}
		internal Node Left { get; }
		internal Node Right { get; }
	}

	private readonly record struct Break( long Offset, int Length );

	internal static Node? Create( IEnumerable<ReadOnlyMemory<char>> chunks, long maximumLength ) {
		ArgumentNullException.ThrowIfNull( chunks );
		Node? root = null; long length = 0; StringBuilder staging = new( LeafTarget );
		void Flush() { if ( 0 == staging.Length ) return; string value = staging.ToString(); staging.Clear(); root = Join( root, new Leaf( value, 0, value.Length ) ); }
		foreach ( ReadOnlyMemory<char> memory in chunks ) {
			if ( memory.Length > maximumLength - length ) throw new ArgumentException( "The source exceeds the configured document length.", nameof( chunks ) );
			length += memory.Length; ReadOnlySpan<char> remaining = memory.Span;
			while ( !remaining.IsEmpty ) {
				int count = Math.Min( LeafTarget - staging.Length, remaining.Length );
				staging.Append( remaining[ ..count ] ); remaining = remaining[ count.. ];
				if ( LeafTarget == staging.Length ) Flush();
			}
		}
		Flush(); return root;
	}

	internal static Node? FromString( string text ) {
		if ( 0 == text.Length ) return null;
		Node? root = null;
		for ( int offset = 0; offset < text.Length; offset += LeafTarget ) {
			int count = Math.Min( LeafTarget, text.Length - offset ); root = Join( root, new Leaf( text, offset, count ) );
		}
		return root;
	}

	internal static Node? Rebuild( Node? source ) {
		if ( source is null ) return null;
		Node? rebuilt = null; StringBuilder staging = new( LeafTarget );
		void Flush() { if ( 0 == staging.Length ) return; string value = staging.ToString(); staging.Clear(); rebuilt = Join( rebuilt, new Leaf( value, 0, value.Length ) ); }
		void AppendNode( Node node ) {
			if ( node is Branch branch ) { AppendNode( branch.Left ); AppendNode( branch.Right ); return; }
			Leaf leaf = (Leaf)node; ReadOnlySpan<char> remaining = leaf.Buffer.AsSpan( leaf.Offset, leaf.Count );
			while ( !remaining.IsEmpty ) { int count = Math.Min( LeafTarget - staging.Length, remaining.Length ); staging.Append( remaining[ ..count ] ); remaining = remaining[ count.. ]; if ( LeafTarget == staging.Length ) Flush(); }
		}
		AppendNode( source ); Flush(); return rebuilt;
	}

	internal static string Read( Node? root, long offset, int length ) {
		if ( 0 == length ) return string.Empty;
		StringBuilder result = new( length ); Append( root!, offset, length, result ); return result.ToString();
	}

	internal static WidgetTextLine GetLine( Node? root, long index ) {
		long breakCount = root?.BreakCount ?? 0;
		if ( index < 0 || index > breakCount ) throw new ArgumentOutOfRangeException( nameof( index ) );
		long start = 0;
		if ( 0 < index ) { Break previous = CompleteBreakLength( root!, FindBreak( root!, index - 1, false, 0 ), 0 ); start = checked( previous.Offset + previous.Length ); }
		if ( index == breakCount ) return new WidgetTextLine( start, ( root?.Length ?? 0 ) - start, 0 );
		Break ending = CompleteBreakLength( root!, FindBreak( root!, index, false, 0 ), 0 );
		return new WidgetTextLine( start, ending.Offset - start, ending.Length );
	}

	internal static Node? Replace( Node? root, long start, long deleteLength, string insertText ) {
		Split( root, start, out Node? before, out Node? remainder );
		Split( remainder, deleteLength, out _, out Node? after );
		return Join( Join( before, FromString( insertText ) ), after );
	}

	internal static char CharacterAt( Node root, long offset ) => GetCharacter( root, offset );
	internal static long GetLineIndex( Node? root, long offset ) => root is null || 0 == offset ? 0 : CountBreaksBefore( root, offset, false );
	internal static bool ContentEquals( Node? root, long offset, string expected ) =>
		0 == expected.Length || Matches( root!, offset, expected.AsSpan() );

	private static void Append( Node node, long offset, int length, StringBuilder result ) {
		if ( 0 == length ) return;
		if ( node is Leaf leaf ) { result.Append( leaf.Buffer, checked( leaf.Offset + (int)offset ), length ); return; }
		Branch branch = (Branch)node; long leftAvailable = Math.Max( 0, branch.Left.Length - offset ); int fromLeft = (int)Math.Min( length, leftAvailable );
		if ( 0 < fromLeft ) Append( branch.Left, offset, fromLeft, result );
		if ( fromLeft < length ) Append( branch.Right, Math.Max( 0, offset - branch.Left.Length ), length - fromLeft, result );
	}

	private static char GetCharacter( Node node, long offset ) {
		while ( node is Branch branch ) { if ( offset < branch.Left.Length ) node = branch.Left; else { offset -= branch.Left.Length; node = branch.Right; } }
		Leaf leaf = (Leaf)node; return leaf.Buffer[ checked( leaf.Offset + (int)offset ) ];
	}

	private static bool Matches( Node node, long offset, ReadOnlySpan<char> expected ) {
		if ( node is Leaf leaf ) return expected.SequenceEqual( leaf.Buffer.AsSpan( checked( leaf.Offset + (int)offset ), expected.Length ) );
		Branch branch = (Branch)node; long leftAvailable = Math.Max( 0, branch.Left.Length - offset ); int fromLeft = (int)Math.Min( expected.Length, leftAvailable );
		if ( 0 < fromLeft && !Matches( branch.Left, offset, expected[ ..fromLeft ] ) ) return false;
		return fromLeft == expected.Length || Matches( branch.Right, Math.Max( 0, offset - branch.Left.Length ), expected[ fromLeft.. ] );
	}

	private static Break FindBreak( Node node, long index, bool previousWasCr, long baseOffset ) {
		if ( node is Leaf leaf ) {
			ReadOnlySpan<char> span = leaf.Buffer.AsSpan( leaf.Offset, leaf.Count ); bool prior = previousWasCr;
			for ( int offset = 0; offset < span.Length; offset++ ) {
				char value = span[ offset ]; bool isBreak = '\r' == value || '\n' == value && !prior;
				if ( isBreak && 0 == index ) return new Break( baseOffset + offset, '\r' == value ? 0 : 1 );
				if ( isBreak ) index--; prior = '\r' == value;
			}
			throw new InvalidOperationException( "The line-break index is inconsistent." );
		}
		Branch branch = (Branch)node; long leftBreaks = BreakCount( branch.Left, previousWasCr );
		if ( index < leftBreaks ) return FindBreak( branch.Left, index, previousWasCr, baseOffset );
		bool rightPreviousWasCr = 0 < branch.Left.Length ? branch.Left.EndsWithCr : previousWasCr;
		return FindBreak( branch.Right, index - leftBreaks, rightPreviousWasCr, baseOffset + branch.Left.Length );
	}

	private static Break CompleteBreakLength( Node scope, Break value, long baseOffset ) {
		if ( 0 != value.Length ) return value;
		long relative = value.Offset - baseOffset;
		int length = relative + 1 < scope.Length && '\n' == GetCharacter( scope, relative + 1 ) ? 2 : 1;
		return value with { Length = length };
	}

	private static long BreakCount( Node node, bool previousWasCr ) =>
		node.BreakCount - ( previousWasCr && node.StartsWithLf ? 1 : 0 );

	private static long CountBreaks( ReadOnlySpan<char> text ) {
		long result = 0; bool previousWasCr = false;
		foreach ( char value in text ) { if ( '\r' == value || '\n' == value && !previousWasCr ) result++; previousWasCr = '\r' == value; }
		return result;
	}

	private static long CountBreaksBefore( Node node, long length, bool previousWasCr ) {
		if ( 0 == length ) return 0;
		if ( node is Leaf leaf ) {
			long result = 0; bool prior = previousWasCr; ReadOnlySpan<char> span = leaf.Buffer.AsSpan( leaf.Offset, checked( (int)length ) );
			foreach ( char value in span ) { if ( '\r' == value || '\n' == value && !prior ) result++; prior = '\r' == value; }
			return result;
		}
		Branch branch = (Branch)node;
		if ( length <= branch.Left.Length ) return CountBreaksBefore( branch.Left, length, previousWasCr );
		long left = BreakCount( branch.Left, previousWasCr ); bool priorRight = 0 < branch.Left.Length ? branch.Left.EndsWithCr : previousWasCr;
		return checked( left + CountBreaksBefore( branch.Right, length - branch.Left.Length, priorRight ) );
	}

	private static void Split( Node? node, long position, out Node? left, out Node? right ) {
		if ( node is null ) { left = null; right = null; return; }
		if ( 0 == position ) { left = null; right = node; return; }
		if ( node.Length == position ) { left = node; right = null; return; }
		if ( node is Leaf leaf ) {
			left = new Leaf( leaf.Buffer, leaf.Offset, checked( (int)position ) );
			right = new Leaf( leaf.Buffer, checked( leaf.Offset + (int)position ), checked( leaf.Count - (int)position ) ); return;
		}
		Branch branch = (Branch)node;
		if ( position < branch.Left.Length ) { Split( branch.Left, position, out Node? first, out Node? second ); left = first; right = Join( second, branch.Right ); }
		else { Split( branch.Right, position - branch.Left.Length, out Node? first, out Node? second ); left = Join( branch.Left, first ); right = second; }
	}

	private static Node? Join( Node? left, Node? right ) {
		if ( left is null ) return right; if ( right is null ) return left;
		if ( left is Leaf leftLeaf && right is Leaf rightLeaf && leftLeaf.Count <= LeafTarget - rightLeaf.Count &&
			ReferenceEquals( leftLeaf.Buffer, rightLeaf.Buffer ) && leftLeaf.Offset + leftLeaf.Count == rightLeaf.Offset )
			return new Leaf( leftLeaf.Buffer, leftLeaf.Offset, checked( leftLeaf.Count + rightLeaf.Count ) );
		if ( left.Height > right.Height + 1 ) { Branch branch = (Branch)left; return Balance( branch.Left, Join( branch.Right, right )! ); }
		if ( right.Height > left.Height + 1 ) { Branch branch = (Branch)right; return Balance( Join( left, branch.Left )!, branch.Right ); }
		return new Branch( left, right );
	}

	private static Node Balance( Node left, Node right ) {
		if ( left.Height > right.Height + 1 ) {
			Branch branch = (Branch)left;
			if ( branch.Left.Height >= branch.Right.Height ) return new Branch( branch.Left, new Branch( branch.Right, right ) );
			Branch inner = (Branch)branch.Right; return new Branch( new Branch( branch.Left, inner.Left ), new Branch( inner.Right, right ) );
		}
		if ( right.Height > left.Height + 1 ) {
			Branch branch = (Branch)right;
			if ( branch.Right.Height >= branch.Left.Height ) return new Branch( new Branch( left, branch.Left ), branch.Right );
			Branch inner = (Branch)branch.Left; return new Branch( new Branch( left, inner.Left ), new Branch( inner.Right, branch.Right ) );
		}
		return new Branch( left, right );
	}
}
