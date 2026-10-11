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

namespace Icod.Widgets.Tests.Core;

using Icod.DCurses;
using Xunit;

public sealed class WidgetEditingRoutingTests {
	[Fact]
	public void FocusedEditorGetsTabButFieldTraverses() {
		using WidgetHost host = CreateHost();
		WidgetStack stack = new( "root", WidgetOrientation.Vertical );
		RoutingProbe editor = new( "editor" ) { TakesTab = true };
		WidgetTextInput field = new( "field" );
		RoutingProbe next = new( "next" );
		stack.Add( editor ); stack.Add( field ); stack.Add( next );
		host.Attach( stack ); host.Arrange( new CursesRectangle( 0, 0, 9, 20 ) );

		Assert.True( host.Focus( editor ) );
		Assert.True( host.Process( WidgetInput.Key( WidgetKey.Tab ), TimeSpan.Zero ).Handled );
		Assert.Equal( 1, editor.TabCount );
		Assert.Same( editor, host.FocusedWidget );

		Assert.True( host.Focus( field ) );
		Assert.True( host.Process( WidgetInput.Key( WidgetKey.Tab ), TimeSpan.FromMilliseconds( 1 ) ).Handled );
		Assert.Same( next, host.FocusedWidget );
	}

	[Fact]
	public void DisabledEditCommandDoesNotFallThrough() {
		using WidgetHost host = CreateHost();
		RoutingProbe editor = new( "editor" ) { EditEnabled = false };
		host.Attach( editor ); editor.Arrange( new CursesRectangle( 0, 0, 2, 10 ) ); host.Focus( editor );

		WidgetCommandResult result = host.ExecuteEditCommand( WidgetEditCommand.Paste, TimeSpan.Zero );

		Assert.Equal( WidgetCommandResult.Disabled, result );
		Assert.Equal( 1, editor.CanExecuteCount );
		Assert.Equal( 0, editor.ExecuteCount );
	}

	[Fact]
	public void CapturedMovesStayWithOwner() {
		using WidgetHost host = CreateHost();
		RoutingProbe first = new( "first" ); RoutingProbe second = new( "second" );
		host.Attach( first ); host.Attach( second );
		first.Arrange( new CursesRectangle( 0, 0, 2, 10 ) ); second.Arrange( new CursesRectangle( 0, 10, 2, 10 ) );

		host.Process( Pointer( CursesMouseAction.Press, 1, 0 ), TimeSpan.Zero );
		host.Process( Pointer( CursesMouseAction.Move, 11, 0 ), TimeSpan.FromMilliseconds( 1 ) );
		host.Process( Pointer( CursesMouseAction.Release, 11, 0 ), TimeSpan.FromMilliseconds( 2 ) );

		Assert.Equal( [ CursesMouseAction.Press, CursesMouseAction.Move, CursesMouseAction.Release ], first.PointerActions );
		Assert.Empty( second.PointerActions );
	}

	[Fact]
	public void CancellationEndsCaptureExactlyOnce() {
		using WidgetHost host = CreateHost();
		RoutingProbe probe = new( "probe" ); host.Attach( probe ); probe.Arrange( new CursesRectangle( 0, 0, 2, 10 ) );
		host.Process( Pointer( CursesMouseAction.Press, 1, 0 ), TimeSpan.Zero );

		probe.IsVisible = false;
		probe.IsEnabled = false;
		host.Detach( probe );

		Assert.Equal( 1, probe.CancellationCount );
	}

	[Fact]
	public void BackwardTickDoesNotMutate() {
		using WidgetHost host = CreateHost();
		RoutingProbe probe = new( "probe" ); host.Attach( probe ); probe.Arrange( new CursesRectangle( 0, 0, 2, 10 ) );
		host.Update( TimeSpan.FromSeconds( 2 ) );

		Assert.Throws<ArgumentOutOfRangeException>( () => host.Update( TimeSpan.FromSeconds( 1 ) ) );

		Assert.Equal( 1, probe.UpdateCount );
		Assert.Equal( TimeSpan.FromSeconds( 2 ), host.CurrentTime );
	}

	[Fact]
	public void ReleaseCannotActivate() {
		using WidgetHost host = CreateHost();
		WidgetButton button = new( "button", "Save", "save" );
		host.Attach( button ); button.Arrange( new CursesRectangle( 0, 0, 1, 12 ) ); host.Focus( button );
		WidgetInput release = WidgetInput.Key( WidgetKey.Enter ) with { KeyPhase = CursesKeyEventPhase.Release };

		WidgetDispatchResult result = host.Process( release, TimeSpan.Zero );

		Assert.False( result.Handled );
		Assert.Empty( result.Actions );
	}

	[Fact]
	public void ExternalActionCapacityFailureIsAtomic() {
		using WidgetHost host = CreateHost();
		RoutingProbe probe = new( "probe" ) { ActionsToEmit = 2 };
		host.Attach( probe ); probe.Arrange( new CursesRectangle( 0, 0, 2, 10 ) ); host.Focus( probe );
		for ( int index = 0; index < WidgetLimits.MaximumPendingActions - 1; index++ )
			host.Enqueue( new WidgetInvokedAction( "seed", index.ToString() ) );

		Assert.Throws<InvalidOperationException>( () => host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ) );

		Assert.False( probe.Mutated );
		Assert.Equal( WidgetLimits.MaximumPendingActions - 1, host.DrainActions().Count );
	}

	private static WidgetInput Pointer( CursesMouseAction action, int column, int row ) =>
		WidgetInput.PointerInput( new CursesMouseEvent( action, CursesMouseButton.Primary, column, row ) );

	private static WidgetHost CreateHost() => new( new CursesScreen( 20, 9 ), WidgetTheme.Default );

	private sealed class RoutingProbe( string id ) : Widget( id ), IWidgetEditTarget {
		public override bool IsFocusable => true;
		public override bool AcceptsTab => this.TakesTab;
		public bool TakesTab { get; init; }
		public bool EditEnabled { get; init; } = true;
		public int ActionsToEmit { get; init; }
		public int TabCount { get; private set; }
		public int CancellationCount { get; private set; }
		public int UpdateCount { get; private set; }
		public int CanExecuteCount { get; private set; }
		public int ExecuteCount { get; private set; }
		public bool Mutated { get; private set; }
		public List<CursesMouseAction> PointerActions { get; } = [];

		public bool CanExecuteEditCommand( WidgetEditCommand command ) { this.CanExecuteCount++; return this.EditEnabled; }
		public WidgetCommandResult ExecuteEditCommand( WidgetEditCommand command, TimeSpan timestamp ) { this.ExecuteCount++; return WidgetCommandResult.Executed; }

		protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
			if ( WidgetKey.Tab == input.KeyCode ) { this.TabCount++; return true; }
			if ( input.Pointer is { } pointer ) { this.PointerActions.Add( pointer.Action ); return true; }
			if ( WidgetKey.Enter != input.KeyCode ) return false;
			this.EnsureActionCapacity( this.ActionsToEmit );
			this.Mutated = true;
			for ( int index = 0; index < this.ActionsToEmit; index++ ) this.EmitAction( new WidgetInvokedAction( this.Id, index.ToString() ) );
			return true;
		}

		protected override void OnCaptureCanceled() => this.CancellationCount++;
		protected override void OnUpdate( TimeSpan timestamp ) => this.UpdateCount++;
	}
}
