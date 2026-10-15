/*
	Icod.Widgets.Gallery.Sample / Icod.Widgets.Editor.Sample / Icod.Widgets.Roguelike.Sample
	Application-owned caret coordinator shared by the Icod.Widgets samples.
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

namespace Icod.Widgets.Samples;

using Icod.DCurses;

internal interface IWidgetSampleCaretSession {
	ValueTask<IAsyncDisposable?> AcquireStyleAsync( CursesCursorStyle style, CancellationToken cancellationToken );
	ValueTask SetPositionAsync( int row, int column, CancellationToken cancellationToken );
	ValueTask SetVisibilityAsync( bool visible, CancellationToken cancellationToken );
}

/// <summary>Serializes reversible cursor presentation while rejecting stale async acquisitions.</summary>
internal sealed class WidgetSampleCaretCoordinator : IAsyncDisposable {
	private sealed class SessionAdapter( CursesSession session ) : IWidgetSampleCaretSession {
		public async ValueTask<IAsyncDisposable?> AcquireStyleAsync( CursesCursorStyle style, CancellationToken cancellationToken ) {
			try { return await session.AcquireCursorStyleAsync( style, TimeSpan.FromMilliseconds( 250 ), cancellationToken ); }
			catch ( Exception error ) when ( error is NotSupportedException or TimeoutException ) { return null; }
		}
		public async ValueTask SetPositionAsync( int row, int column, CancellationToken cancellationToken ) => await session.SetCursorPositionAsync( row, column, cancellationToken );
		public async ValueTask SetVisibilityAsync( bool visible, CancellationToken cancellationToken ) =>
			await session.SetCursorVisibilityAsync( visible ? CursesCursorVisibility.Normal : CursesCursorVisibility.Hidden, cancellationToken );
	}

	private readonly IWidgetSampleCaretSession session;
	private readonly SemaphoreSlim gate = new( 1, 1 );
	private IAsyncDisposable? lease;
	private CursesCursorStyle? style;
	private CursesCursorStyle? unsupportedStyle;
	private long generation;
	private bool disposed;

	internal WidgetSampleCaretCoordinator( CursesSession session ) : this( new SessionAdapter( session ?? throw new ArgumentNullException( nameof( session ) ) ) ) { }
	internal WidgetSampleCaretCoordinator( IWidgetSampleCaretSession session ) => this.session = session ?? throw new ArgumentNullException( nameof( session ) );

	internal async ValueTask ApplyAsync( WidgetCaretIntent? intent, CancellationToken cancellationToken ) {
		if ( this.disposed ) throw new ObjectDisposedException( nameof( WidgetSampleCaretCoordinator ) ); long requested = Interlocked.Increment( ref this.generation );
		await this.gate.WaitAsync( cancellationToken );
		try {
			if ( this.disposed || requested != Volatile.Read( ref this.generation ) ) return;
			if ( intent is null || !intent.IsVisible ) {
				await this.session.SetVisibilityAsync( false, cancellationToken ); await this.ReleaseLeaseAsync(); return;
			}
			await this.session.SetPositionAsync( intent.Row, intent.Column, cancellationToken ); await this.session.SetVisibilityAsync( true, cancellationToken );
			if ( intent.PreferredStyle == this.style ) return; await this.ReleaseLeaseAsync();
			if ( intent.PreferredStyle == this.unsupportedStyle ) return;
			if ( intent.PreferredStyle is not { } requestedStyle ) return;
			IAsyncDisposable? acquired = await this.session.AcquireStyleAsync( requestedStyle, cancellationToken );
			if ( acquired is null ) { this.unsupportedStyle = requestedStyle; return; }
			if ( this.disposed || requested != Volatile.Read( ref this.generation ) ) { await acquired.DisposeAsync(); return; }
			this.lease = acquired; this.style = requestedStyle;
		} finally { this.gate.Release(); }
	}

	public async ValueTask DisposeAsync() {
		if ( this.disposed ) return; Interlocked.Increment( ref this.generation ); await this.gate.WaitAsync();
		try {
			if ( this.disposed ) return; this.disposed = true;
			try { await this.session.SetVisibilityAsync( false, CancellationToken.None ); } finally { await this.ReleaseLeaseAsync(); }
		} finally { this.gate.Release(); this.gate.Dispose(); }
	}

	private async ValueTask ReleaseLeaseAsync() {
		IAsyncDisposable? current = this.lease; this.lease = null; this.style = null; if ( current is not null ) await current.DisposeAsync();
	}
}
