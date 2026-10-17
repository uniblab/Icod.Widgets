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

namespace Icod.Widgets.Tests.Editing;

using Icod.DCurses;
using Icod.Widgets.Samples;
using Xunit;

public sealed class WidgetCaretTests {
	[Fact]
	public void CaretMatchesUnicodeGeometry() {
		using Fixture fixture = new( "\t界" ); fixture.View.SetSelection( 1, 1 );

		WidgetCaretIntent intent = Assert.IsType<WidgetCaretIntent>( fixture.Host.GetCaretIntent() );

		Assert.Equal( ( 1, 6 ), ( intent.Row, intent.Column ) );
		Assert.True( intent.IsVisible );
		Assert.True( intent.FocusGeneration > 0 );
	}

	[Fact]
	public void HiddenOffscreenOrModalCaretIsAbsent() {
		using Fixture offscreen = new( string.Join( '\n', Enumerable.Range( 0, 10 ) ) ); offscreen.View.SetSelection( offscreen.Document.Length, offscreen.Document.Length );
		Assert.Null( offscreen.Host.GetCaretIntent() );

		using Fixture hidden = new( "text" ); hidden.Editor.IsVisible = false; Assert.Null( hidden.Host.GetCaretIntent() );

		using Fixture modal = new( "text" ); using WidgetModalLease lease = modal.Host.ShowModal( WidgetMessageDialog.Ok( "dialog", "Title", "Message" ) );
		Assert.Null( modal.Host.GetCaretIntent() );
	}

	[Fact]
	public void InsertAndOverwriteHaveDistinctStyleIntent() {
		using Fixture fixture = new( "text" );

		Assert.Equal( CursesCursorStyle.SteadyBar, fixture.Host.GetCaretIntent()!.PreferredStyle );
		fixture.Host.Process( WidgetInput.Key( WidgetKey.Insert ), TimeSpan.Zero );

		Assert.Equal( CursesCursorStyle.SteadyBlock, fixture.Host.GetCaretIntent()!.PreferredStyle );
	}

	[Fact]
	public void LineNumberGutterOffsetsCaretAndTextHitTesting() {
		using Fixture fixture = new( "abcd", new WidgetEditorOptions { ShowLineNumbers = true } ); fixture.View.SetSelection( 2, 2 );

		WidgetCaretIntent intent = Assert.IsType<WidgetCaretIntent>( fixture.Host.GetCaretIntent() );
		fixture.Host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press, CursesMouseButton.Primary, 5, 1 ) ), TimeSpan.Zero );

		Assert.Equal( 6, intent.Column );
		Assert.Equal( 1, fixture.View.Caret );
	}

	[Fact]
	public async Task StaleStyleCompletionIsDisposed() {
		FakeCaretSession session = new(); await using WidgetSampleCaretCoordinator coordinator = new( session );
		FakeLease stale = new(); FakeLease current = new();

		Task first = coordinator.ApplyAsync( Intent( CursesCursorStyle.SteadyBar, 1 ), CancellationToken.None ).AsTask();
		await session.WaitForAcquisitionsAsync( 1 );
		Task second = coordinator.ApplyAsync( Intent( CursesCursorStyle.SteadyBlock, 2 ), CancellationToken.None ).AsTask();
		session.CompleteNext( stale ); await session.WaitForAcquisitionsAsync( 2 ); session.CompleteNext( current ); await Task.WhenAll( first, second );

		Assert.Equal( 1, stale.DisposeCount );
		Assert.Equal( 0, current.DisposeCount );
	}

	[Fact]
	public async Task UnsupportedObservationDoesNotEmitGuessedReset() {
		FakeCaretSession session = new() { CompleteWithUnsupported = true }; await using WidgetSampleCaretCoordinator coordinator = new( session );

		await coordinator.ApplyAsync( Intent( CursesCursorStyle.SteadyBar, 1 ), CancellationToken.None );

		await coordinator.ApplyAsync( Intent( CursesCursorStyle.SteadyBar, 2 ), CancellationToken.None );

		Assert.Equal( 1, session.AcquireCount );
		Assert.Equal( 0, session.DisposedLeaseCount );
	}

	[Fact]
	public async Task CleanupRestoresOnce() {
		FakeCaretSession session = new(); WidgetSampleCaretCoordinator coordinator = new( session ); FakeLease lease = new();
		Task apply = coordinator.ApplyAsync( Intent( CursesCursorStyle.SteadyBar, 1 ), CancellationToken.None ).AsTask(); await session.WaitForAcquisitionsAsync( 1 ); session.CompleteNext( lease ); await apply;

		await coordinator.DisposeAsync(); await coordinator.DisposeAsync();

		Assert.Equal( 1, lease.DisposeCount );
		Assert.Equal( 1, session.HiddenCount );
	}

	private static WidgetCaretIntent Intent( CursesCursorStyle style, long generation ) => new( true, 1, 2, style, generation );

	private sealed class Fixture : IDisposable {
		internal Fixture( string text, WidgetEditorOptions? options = null ) {
			this.Document = new WidgetTextDocument( [ text.AsMemory() ] ); this.View = new WidgetTextViewState( this.Document ); this.Editor = new WidgetTextEditor( "editor", this.View, options );
			this.Host = new WidgetHost( new CursesScreen( 20, 5 ), WidgetTheme.Default ); this.Host.Attach( this.Editor ); this.Editor.Arrange( new CursesRectangle( 1, 2, 2, 10 ) ); Assert.True( this.Host.Focus( this.Editor ) );
		}
		internal WidgetTextDocument Document { get; }
		internal WidgetTextViewState View { get; }
		internal WidgetTextEditor Editor { get; }
		internal WidgetHost Host { get; }
		public void Dispose() { this.Host.Dispose(); this.View.Dispose(); }
	}

	private sealed class FakeLease : IAsyncDisposable {
		internal int DisposeCount { get; private set; }
		public ValueTask DisposeAsync() { this.DisposeCount++; return ValueTask.CompletedTask; }
	}

	private sealed class FakeCaretSession : IWidgetSampleCaretSession {
		private readonly Queue<TaskCompletionSource<IAsyncDisposable?>> pending = [];
		internal bool CompleteWithUnsupported { get; init; }
		internal int AcquireCount { get; private set; }
		internal int HiddenCount { get; private set; }
		internal int DisposedLeaseCount { get; private set; }
		public ValueTask<IAsyncDisposable?> AcquireStyleAsync( CursesCursorStyle style, CancellationToken cancellationToken ) {
			this.AcquireCount++; if ( this.CompleteWithUnsupported ) return ValueTask.FromResult<IAsyncDisposable?>( null );
			TaskCompletionSource<IAsyncDisposable?> source = new( TaskCreationOptions.RunContinuationsAsynchronously ); this.pending.Enqueue( source ); return new ValueTask<IAsyncDisposable?>( source.Task );
		}
		public ValueTask SetPositionAsync( int row, int column, CancellationToken cancellationToken ) => ValueTask.CompletedTask;
		public ValueTask SetVisibilityAsync( bool visible, CancellationToken cancellationToken ) { if ( !visible ) this.HiddenCount++; return ValueTask.CompletedTask; }
		internal void CompleteNext( FakeLease lease ) { this.pending.Dequeue().SetResult( new CountedLease( lease, this ) ); }
		internal async Task WaitForAcquisitionsAsync( int count ) { while ( this.AcquireCount < count ) await Task.Yield(); }
		private sealed class CountedLease( FakeLease inner, FakeCaretSession owner ) : IAsyncDisposable {
			public async ValueTask DisposeAsync() { owner.DisposedLeaseCount++; await inner.DisposeAsync(); }
		}
	}
}
