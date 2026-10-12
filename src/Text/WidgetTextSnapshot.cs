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

/// <summary>Retains one immutable document state until explicitly disposed.</summary>
public sealed class WidgetTextSnapshot : IDisposable {
	private sealed record State( WidgetPieceTree.Node? Root );
	private readonly Guid id;
	private readonly long revision;
	private readonly long stateId;
	private readonly Action release;
	private State? state;

	internal WidgetTextSnapshot( Guid id, long revision, long stateId, WidgetPieceTree.Node? root, Action release ) {
		this.id = id; this.revision = revision; this.stateId = stateId; this.state = new State( root ); this.release = release;
	}

	~WidgetTextSnapshot() => this.Dispose( false );

	public Guid Id { get { _ = this.GetState(); return this.id; } }
	public long Revision { get { _ = this.GetState(); return this.revision; } }
	public long StateId { get { _ = this.GetState(); return this.stateId; } }
	public long Length { get { State current = this.GetState(); return current.Root?.Length ?? 0; } }
	public long LineCount { get { State current = this.GetState(); return checked( ( current.Root?.BreakCount ?? 0 ) + 1 ); } }

	public string Read( long offset, int length ) {
		State current = this.GetState(); long available = current.Root?.Length ?? 0;
		if ( 0 > offset ) throw new ArgumentOutOfRangeException( nameof( offset ) );
		if ( 0 > length ) throw new ArgumentOutOfRangeException( nameof( length ) );
		if ( offset > available || length > available - offset ) throw new ArgumentOutOfRangeException( nameof( length ) );
		return WidgetPieceTree.Read( current.Root, offset, length );
	}

	public WidgetTextLine GetLine( long index ) { State current = this.GetState(); return WidgetPieceTree.GetLine( current.Root, index ); }

	public void Dispose() { this.Dispose( true ); GC.SuppressFinalize( this ); }

	private void Dispose( bool disposing ) {
		if ( Interlocked.Exchange( ref this.state, null ) is null ) return;
		this.release();
	}

	private State GetState() => Volatile.Read( ref this.state ) ?? throw new ObjectDisposedException( nameof( WidgetTextSnapshot ) );
}
