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
using Xunit;

public sealed class WidgetEditorPointerTests {
	[Fact]
	public void WheelScrollsThreeDisplayedRowsAndClampsCaret() {
		using Fixture fixture = new( new WidgetEditorOptions { ShowVerticalScrollBar = true } ); fixture.View.SetSelection( 0, 0 );

		WidgetDispatchResult result = fixture.Send( CursesMouseAction.WheelDown, 5, 0 );

		Assert.Equal( 3, fixture.View.VerticalOffset );
		Assert.Equal( fixture.Document.GetLine( 3 ).Start, fixture.View.Caret );
		Assert.IsType<WidgetTextSelectionChangedAction>( Assert.Single( result.Actions ) );
	}

	[Fact]
	public void ArrowClicksScrollOneDisplayedRow() {
		using Fixture fixture = new( new WidgetEditorOptions { ShowVerticalScrollBar = true } ); fixture.View.SetSelection( 0, 0 ); fixture.Host.Render();

		fixture.Send( CursesMouseAction.Press, 19, 1 );

		Assert.Equal( 1, fixture.View.VerticalOffset );
		Assert.Equal( fixture.Document.GetLine( 1 ).Start, fixture.View.Caret );
	}

	[Fact]
	public void TrackClicksPageAndClampCaret() {
		using Fixture fixture = new( new WidgetEditorOptions { ShowVerticalScrollBar = true } ); fixture.Editor.Arrange( new CursesRectangle( 0, 0, 6, 20 ) );
		fixture.View.SetSelection( 0, 0 ); fixture.Host.Render();

		fixture.Send( CursesMouseAction.Press, 19, 4 );

		Assert.Equal( 6, fixture.View.VerticalOffset );
		Assert.Equal( fixture.Document.GetLine( 6 ).Start, fixture.View.Caret );
	}

	[Fact]
	public void ThumbDragUsesDisplayedRowsAndReachesBothBoundaries() {
		using Fixture fixture = new( new WidgetEditorOptions { ShowVerticalScrollBar = true } ); fixture.Editor.Arrange( new CursesRectangle( 0, 0, 6, 20 ) ); fixture.Host.Render();

		fixture.Send( CursesMouseAction.Press, 19, 1 ); fixture.Send( CursesMouseAction.Move, 19, 4 ); fixture.Send( CursesMouseAction.Release, 19, 4 );
		Assert.Equal( fixture.Editor.VerticalScrollRange.Extent - fixture.Editor.VerticalScrollRange.Viewport, fixture.Editor.VerticalScrollRange.Offset );
		fixture.Host.Render(); fixture.Send( CursesMouseAction.Press, 19, 4 ); fixture.Send( CursesMouseAction.Move, 19, 1 ); fixture.Send( CursesMouseAction.Release, 19, 1 );

		Assert.Equal( 0, fixture.View.VerticalOffset );
	}

	[Fact]
	public void InexactThumbMovementIsMonotonic() {
		using Fixture fixture = new( new WidgetEditorOptions { ShowVerticalScrollBar = true, WrapMode = CursesTextWrapMode.Word } );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 6, 8 ) ); fixture.Host.Render(); long previous = fixture.Editor.VerticalScrollRange.Offset;
		fixture.Send( CursesMouseAction.Press, 7, 1 );

		for ( int row = 2; row <= 4; row++ ) {
			fixture.Send( CursesMouseAction.Move, 7, row );
			Assert.True( fixture.Editor.VerticalScrollRange.Offset >= previous ); previous = fixture.Editor.VerticalScrollRange.Offset;
		}
		fixture.Send( CursesMouseAction.Release, 7, 4 );
	}

	[Fact]
	public void CaretClampCollapsesSelectionAndEmitsOneSelectionAction() {
		using Fixture fixture = new( new WidgetEditorOptions { ShowVerticalScrollBar = true } ); fixture.View.SetSelection( 0, fixture.Document.GetLine( 1 ).Start );

		WidgetDispatchResult result = fixture.Send( CursesMouseAction.WheelDown, 5, 0 );

		Assert.False( fixture.View.HasSelection );
		Assert.IsType<WidgetTextSelectionChangedAction>( Assert.Single( result.Actions ) );
	}

	[Fact]
	public void VisibleCaretProducesNoSelectionAction() {
		using Fixture fixture = new( new WidgetEditorOptions { ShowVerticalScrollBar = true } ); fixture.Editor.Arrange( new CursesRectangle( 0, 0, 6, 20 ) );
		long caret = fixture.Document.GetLine( 4 ).Start; fixture.View.SetSelection( caret, caret );

		WidgetDispatchResult result = fixture.Send( CursesMouseAction.WheelDown, 5, 0 );

		Assert.Equal( caret, fixture.View.Caret );
		Assert.Empty( result.Actions );
	}

	[Fact]
	public void ScrollbarDragCancelsOnReleaseFocusLossDetachModalAndResize() {
		using Fixture released = new( new WidgetEditorOptions { ShowVerticalScrollBar = true } ); StartScrollbarDrag( released ); released.Send( CursesMouseAction.Release, 19, 1 ); AssertDragCanceled( released );
		using Fixture unfocused = new( new WidgetEditorOptions { ShowVerticalScrollBar = true } ); StartScrollbarDrag( unfocused ); unfocused.Host.Process( WidgetInput.FocusChanged( CursesFocusState.Unfocused ), TimeSpan.FromMilliseconds( 1 ) ); AssertDragCanceled( unfocused );
		using Fixture resized = new( new WidgetEditorOptions { ShowVerticalScrollBar = true } ); StartScrollbarDrag( resized ); resized.Host.Arrange( resized.Host.Screen.Bounds ); AssertDragCanceled( resized );
		using Fixture modal = new( new WidgetEditorOptions { ShowVerticalScrollBar = true } ); StartScrollbarDrag( modal ); using ( modal.Host.ShowModal( WidgetMessageDialog.Ok( "dialog", "Title", "Message" ) ) ) AssertDragCanceled( modal );
		using Fixture detached = new( new WidgetEditorOptions { ShowVerticalScrollBar = true } ); StartScrollbarDrag( detached ); Assert.True( detached.Host.Detach( detached.Editor ) ); long before = detached.View.VerticalOffset; detached.Editor.OnInput( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Move, CursesMouseButton.Primary, 19, 0 ) ), detached.Host ); Assert.Equal( before, detached.View.VerticalOffset );
	}

	[Fact]
	public void RapidWheelAndDragStayInRange() {
		using Fixture fixture = new( new WidgetEditorOptions { ShowVerticalScrollBar = true } ); fixture.Editor.Arrange( new CursesRectangle( 0, 0, 6, 20 ) ); fixture.Host.Render();

		for ( int index = 0; index < 50; index++ ) {
			fixture.Send( 0 == index % 2 ? CursesMouseAction.WheelDown : CursesMouseAction.WheelUp, 5, 0 );
			fixture.Send( CursesMouseAction.Press, 19, 1 ); fixture.Send( CursesMouseAction.Move, 19, 4 ); fixture.Send( CursesMouseAction.Release, 19, 4 );
		}

		Assert.InRange( fixture.Editor.VerticalScrollRange.Offset, 0, Math.Max( 0, fixture.Editor.VerticalScrollRange.Extent - fixture.Editor.VerticalScrollRange.Viewport ) );
	}

	[Fact]
	public void SelectionActionCapacityIsReservedBeforeCaretClamp() {
		using Fixture fixture = new( new WidgetEditorOptions { ShowVerticalScrollBar = true } ); fixture.View.SetSelection( 0, 0 );
		for ( int index = 0; index < WidgetLimits.MaximumPendingActions; index++ ) fixture.Host.Enqueue( new WidgetInvokedAction( "seed", index.ToString() ) );

		Assert.Throws<InvalidOperationException>( () => fixture.Host.Process( WidgetInput.PointerInput( new CursesMouseEvent(
			CursesMouseAction.WheelDown, CursesMouseButton.None, 5, 0 ) ), TimeSpan.Zero ) );

		Assert.Equal( 0, fixture.View.VerticalOffset );
		Assert.Equal( 0, fixture.View.Caret );
	}
	[Fact]
	public void DragOutsideExtendsSelection() {
		using Fixture fixture = new();

		fixture.Send( CursesMouseAction.Press, 1, 0 );
		fixture.Send( CursesMouseAction.Move, 5, 4 );
		fixture.Send( CursesMouseAction.Release, 5, 4 );

		Assert.Equal( 1, fixture.View.Anchor );
		Assert.True( fixture.View.Caret > fixture.View.Anchor );
	}

	[Fact]
	public void ShiftClickExtendsExistingAnchor() {
		using Fixture fixture = new(); fixture.View.SetSelection( 3, 3 );

		fixture.Send( CursesMouseAction.Press, 6, 1, CursesKeyModifiers.Shift );

		Assert.Equal( 3, fixture.View.Anchor );
		Assert.True( fixture.View.Caret > fixture.View.Anchor );
	}

	[Fact]
	public void WrappedScrollbarExtentIsMarkedEstimated() {
		using Fixture fixture = new( new WidgetEditorOptions { WrapMode = CursesTextWrapMode.Word } );

		fixture.Host.Render();

		Assert.False( fixture.Editor.VerticalScrollRange.IsExtentExact );
		Assert.True( fixture.Editor.HorizontalScrollRange.IsExtentExact );
	}

	[Fact]
	public void DecorativeGutterPressSequenceDoesNotChangeSelection() {
		using Fixture fixture = new( new WidgetEditorOptions { ShowLineNumbers = true } ); fixture.View.SetSelection( 5, 8 );

		fixture.Send( CursesMouseAction.Press, 1, 0 );
		fixture.Send( CursesMouseAction.Move, 10, 1 );
		fixture.Send( CursesMouseAction.Release, 10, 1 );

		Assert.Equal( ( 5L, 8L ), ( fixture.View.Anchor, fixture.View.Caret ) );
	}

	[Fact]
	public void TextOriginatedDragCrossingGutterStillExtendsSelectionAndAutoscrolls() {
		using Fixture fixture = new( new WidgetEditorOptions { ShowLineNumbers = true } ); fixture.View.SetScroll( 5, 0 );

		fixture.Send( CursesMouseAction.Press, 10, 0 ); long anchor = fixture.View.Anchor;
		fixture.Send( CursesMouseAction.Move, 1, 0 ); fixture.Host.Update( TimeSpan.FromMilliseconds( 200 ) );

		Assert.NotEqual( anchor, fixture.View.Caret );
		Assert.InRange( fixture.View.HorizontalOffset, 0, 4 );
	}

	[Fact]
	public void WheelOverGutterStillScrolls() {
		using Fixture fixture = new( new WidgetEditorOptions { ShowLineNumbers = true } );

		fixture.Send( CursesMouseAction.WheelDown, 1, 0 );

		Assert.Equal( 3, fixture.View.VerticalOffset );
	}

	[Fact]
	public void ScreenClippedEditorUsesRenderedViewportForPointerGeometry() {
		using Fixture fixture = new( new WidgetEditorOptions { ShowLineNumbers = true } );
		fixture.View.SetSelection( 5, 8 );
		fixture.Editor.Arrange( new CursesRectangle( 0, 18, 2, 20 ) );

		fixture.Send( CursesMouseAction.Press, 18, 0 );
		fixture.Send( CursesMouseAction.Release, 18, 0 );

		Assert.Equal( ( 0L, 0L ), ( fixture.View.Anchor, fixture.View.Caret ) );
	}

	[Fact]
	public void TickAutoscrollIsBoundedAndMonotonic() {
		using Fixture fixture = new(); fixture.Send( CursesMouseAction.Press, 0, 1 ); fixture.Send( CursesMouseAction.Move, 0, 5 );

		fixture.Host.Update( TimeSpan.FromSeconds( 1 ) ); long first = fixture.View.VerticalOffset;
		Assert.InRange( first, 1, 64 );
		Assert.Throws<ArgumentOutOfRangeException>( () => fixture.Host.Update( TimeSpan.FromMilliseconds( 500 ) ) );

		Assert.Equal( first, fixture.View.VerticalOffset );
	}

	[Fact]
	public void LossModalResizeDetachCancelDrag() {
		using Fixture resized = new(); StartDrag( resized ); resized.Host.Arrange( resized.Host.Screen.Bounds ); AssertNoAutoscroll( resized );
		using Fixture modal = new(); StartDrag( modal ); using ( modal.Host.ShowModal( WidgetMessageDialog.Ok( "dialog", "Title", "Message" ) ) ) AssertNoAutoscroll( modal );
		using Fixture hidden = new(); StartDrag( hidden ); hidden.Editor.IsVisible = false; AssertNoAutoscroll( hidden );
		using Fixture unfocused = new(); StartDrag( unfocused ); unfocused.Host.Process( WidgetInput.FocusChanged( CursesFocusState.Unfocused ), TimeSpan.FromMilliseconds( 2 ) ); AssertNoAutoscroll( unfocused );
		using Fixture detached = new(); StartDrag( detached ); Assert.True( detached.Host.Detach( detached.Editor ) ); AssertNoAutoscroll( detached );
	}

	[Fact]
	public void HorizontalScrollbarUsesSharedClickShiftDragWheelAndRepeatBehavior() {
		using HorizontalFixture fixture = new(); fixture.Host.Render();

		fixture.Send( CursesMouseAction.Press, 9, 3 );
		Assert.Equal( 1, fixture.View.HorizontalOffset );
		fixture.Send( CursesMouseAction.Release, 9, 3 );
		fixture.Host.Render(); fixture.Send( CursesMouseAction.WheelRight, 5, 0 );
		Assert.Equal( 2, fixture.View.HorizontalOffset );

		fixture.View.SetScroll( 0, 0 ); fixture.Host.Render();
		fixture.Send( CursesMouseAction.Press, 8, 3, CursesKeyModifiers.Shift );
		Assert.Equal( 2, fixture.View.HorizontalOffset );
		fixture.Send( CursesMouseAction.Release, 8, 3, CursesKeyModifiers.Shift );

		fixture.View.SetScroll( 0, 0 ); fixture.Host.Render();
		fixture.Send( CursesMouseAction.Press, 1, 3 ); fixture.Send( CursesMouseAction.Move, 8, 3 ); fixture.Send( CursesMouseAction.Release, 8, 3 );
		Assert.Equal( 2, fixture.View.HorizontalOffset );

		fixture.View.SetScroll( 10, 0 ); fixture.Host.Render();
		fixture.SendAt( CursesMouseAction.Press, 0, 3, TimeSpan.FromMilliseconds( 100 ) );
		Assert.Equal( 9, fixture.View.HorizontalOffset );
		fixture.Host.Update( TimeSpan.FromMilliseconds( 500 ) );
		Assert.Equal( 8, fixture.View.HorizontalOffset );
		Assert.Same( fixture.Editor, fixture.Host.FocusedWidget );
	}

	[Fact]
	public void EditorScrollbarContextAndCornerStayOwnedByEditor() {
		using HorizontalFixture fixture = new( bothAxes: true ); fixture.Host.Render(); long caret = fixture.View.Caret;

		fixture.Send( CursesMouseAction.Press, 9, 3 );
		fixture.Send( CursesMouseAction.Release, 9, 3 );
		Assert.Equal( caret, fixture.View.Caret );
		Assert.Equal( 0, fixture.View.HorizontalOffset );

		fixture.Send( CursesMouseAction.Press, 7, 3, button: CursesMouseButton.Secondary );
		fixture.Send( CursesMouseAction.Release, 7, 3, button: CursesMouseButton.Secondary );
		Assert.True( fixture.Editor.HorizontalContextMenu.IsOpen );
		Assert.Same( fixture.Editor.HorizontalContextMenu, fixture.Host.FocusedWidget );
		fixture.Host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 10 ) );
		Assert.True( fixture.View.HorizontalOffset > 0 );
		Assert.Same( fixture.Editor, fixture.Host.FocusedWidget );
	}

	[Fact]
	public void HorizontalScrollbarInteractionCancelsWithLifecycleChanges() {
		using HorizontalFixture fixture = new(); fixture.Host.Render();
		fixture.Send( CursesMouseAction.Press, 1, 3 );
		fixture.Host.Process( WidgetInput.FocusChanged( CursesFocusState.Unfocused ), TimeSpan.FromMilliseconds( 1 ) );
		long before = fixture.View.HorizontalOffset;

		fixture.Send( CursesMouseAction.Move, 8, 3 ); fixture.Host.Update( TimeSpan.FromSeconds( 1 ) );

		Assert.Equal( before, fixture.View.HorizontalOffset );
	}

	[Fact]
	public void HorizontalDragCancelsWhenExtentDiscoveryChangesGeometry() {
		using HorizontalFixture fixture = new(); fixture.Host.Render();
		fixture.Send( CursesMouseAction.Press, 1, 3 );

		fixture.Host.Update( TimeSpan.FromMilliseconds( 100 ) ); long before = fixture.View.HorizontalOffset;
		fixture.SendAt( CursesMouseAction.Move, 8, 3, TimeSpan.FromMilliseconds( 101 ) );

		Assert.True( fixture.Editor.HorizontalScrollRange.IsExtentExact );
		Assert.Equal( before, fixture.View.HorizontalOffset );
	}

	[Fact]
	public void HorizontalScrollClampsCaretToVisibleGraphemeAndEmitsOneAction() {
		using HorizontalFixture fixture = new(); fixture.View.SetSelection( 0, 0 ); fixture.Host.Render();

		WidgetDispatchResult result = fixture.Send( CursesMouseAction.WheelRight, 5, 0 );

		Assert.False( fixture.View.HasSelection );
		Assert.Equal( 1, fixture.View.Caret );
		Assert.IsType<WidgetTextSelectionChangedAction>( Assert.Single( result.Actions ) );
	}

	[Fact]
	public void HorizontalScrollPreservesSelectionWhileCaretRemainsVisible() {
		using HorizontalFixture fixture = new(); fixture.View.SetSelection( 4, 6 ); fixture.Host.Render();

		WidgetDispatchResult result = fixture.Send( CursesMouseAction.WheelRight, 5, 0 );

		Assert.Equal( ( 4L, 6L ), ( fixture.View.Anchor, fixture.View.Caret ) );
		Assert.Empty( result.Actions );
	}

	[Fact]
	public void HorizontalCaretClampReservesActionCapacityBeforeScrolling() {
		using HorizontalFixture fixture = new(); fixture.View.SetSelection( 0, 0 ); fixture.Host.Render();
		for ( int index = 0; index < WidgetLimits.MaximumPendingActions; index++ )
			fixture.Host.Enqueue( new WidgetInvokedAction( "seed", index.ToString() ) );

		Assert.Throws<InvalidOperationException>( () => fixture.Send( CursesMouseAction.WheelRight, 5, 0 ) );

		Assert.Equal( 0, fixture.View.HorizontalOffset );
		Assert.Equal( 0, fixture.View.Caret );
	}

	[Fact]
	public void ExactHorizontalShrinkClampsViewportInOneUpdate() {
		using HorizontalFixture fixture = new(); fixture.Host.Render(); fixture.Host.Update( TimeSpan.Zero );
		fixture.View.SetScroll( 60, 0 ); fixture.View.SetSelection( 0, fixture.Document.Length );
		fixture.Host.Process( WidgetInput.TextInput( new System.Text.Rune( 'x' ) ), TimeSpan.FromMilliseconds( 1 ) );

		fixture.Host.Update( TimeSpan.FromMilliseconds( 2 ) );

		Assert.True( fixture.Editor.HorizontalScrollRange.IsExtentExact );
		Assert.Equal( 0, fixture.View.HorizontalOffset );
		Assert.Equal( fixture.Editor.HorizontalScrollRange.Offset, fixture.View.HorizontalOffset );
	}

	[Theory]
	[InlineData( "\tab界e\u0301z" )]
	[InlineData( "ab\U0001F600界z" )]
	public void HorizontalCaretClampUsesDisplayColumnsAndTextElementBoundaries( string prefix ) {
		using HorizontalFixture fixture = new( text: prefix + new string( 'x', 20 ) ); fixture.View.SetSelection( 0, 0 ); fixture.Host.Render();

		WidgetDispatchResult result = fixture.Send( CursesMouseAction.WheelRight, 5, 0 );

		Assert.True( fixture.Document.IsTextElementBoundary( fixture.View.Caret ) );
		Assert.IsType<WidgetTextSelectionChangedAction>( Assert.Single( result.Actions ) );
		Assert.True( Assert.IsType<WidgetCaretIntent>( fixture.Editor.GetHostCaretIntent() ).IsVisible );
	}

	private static void StartDrag( Fixture fixture ) { fixture.Send( CursesMouseAction.Press, 0, 1 ); fixture.Send( CursesMouseAction.Move, 0, 5 ); }
	private static void AssertNoAutoscroll( Fixture fixture ) { long before = fixture.View.VerticalOffset; fixture.Host.Update( TimeSpan.FromSeconds( 1 ) ); Assert.Equal( before, fixture.View.VerticalOffset ); }
	private static void StartScrollbarDrag( Fixture fixture ) { fixture.Editor.Arrange( new CursesRectangle( 0, 0, 6, 20 ) ); fixture.Host.Render(); fixture.Send( CursesMouseAction.Press, 19, 1 ); }
	private static void AssertDragCanceled( Fixture fixture ) { long before = fixture.View.VerticalOffset; fixture.Send( CursesMouseAction.Move, 19, 4 ); Assert.Equal( before, fixture.View.VerticalOffset ); }

	private sealed class Fixture : IDisposable {
		private int tick;
		internal Fixture( WidgetEditorOptions? options = null ) {
			string text = string.Join( '\n', Enumerable.Range( 0, 200 ).Select( index => $"line {index:D3}" ) );
			this.Document = new WidgetTextDocument( [ text.AsMemory() ] ); this.View = new WidgetTextViewState( this.Document ); this.Editor = new WidgetTextEditor( "editor", this.View, options );
			this.Host = new WidgetHost( new CursesScreen( 20, 6 ), WidgetTheme.Default ); this.Host.Attach( this.Editor ); this.Editor.Arrange( new CursesRectangle( 0, 0, 2, 20 ) ); Assert.True( this.Host.Focus( this.Editor ) );
		}
		internal WidgetTextDocument Document { get; }
		internal WidgetTextViewState View { get; }
		internal WidgetTextEditor Editor { get; }
		internal WidgetHost Host { get; }
			internal WidgetDispatchResult Send( CursesMouseAction action, int column, int row, CursesKeyModifiers modifiers = CursesKeyModifiers.None ) {
			CursesMouseButton button = action is CursesMouseAction.WheelUp or CursesMouseAction.WheelDown or CursesMouseAction.WheelLeft or CursesMouseAction.WheelRight
				? CursesMouseButton.None : CursesMouseButton.Primary;
				return this.Host.Process( WidgetInput.PointerInput( new CursesMouseEvent( action, button, column, row, modifiers ) ), TimeSpan.FromMilliseconds( this.tick++ ) );
		}
		public void Dispose() { this.Host.Dispose(); this.View.Dispose(); }
	}

	private sealed class HorizontalFixture : IDisposable {
		private int tick;
		internal HorizontalFixture( bool bothAxes = false, string? text = null ) {
			string line = text ?? new string( 'x', 80 ); string documentText = bothAxes ? string.Join( '\n', Enumerable.Repeat( line, 8 ) ) : line;
			this.Document = new WidgetTextDocument( [ documentText.AsMemory() ] ); this.View = new WidgetTextViewState( this.Document );
			this.Editor = new WidgetTextEditor( "horizontal-editor", this.View, new WidgetEditorOptions {
				ShowHorizontalScrollBar = true, ShowVerticalScrollBar = bothAxes
			} );
			this.Host = new WidgetHost( new CursesScreen( 20, 6 ), WidgetTheme.Default ); this.Host.Attach( this.Editor );
			this.Editor.Arrange( new CursesRectangle( 0, 0, 4, 10 ) ); Assert.True( this.Host.Focus( this.Editor ) );
		}
		internal WidgetTextDocument Document { get; }
		internal WidgetTextViewState View { get; }
		internal WidgetTextEditor Editor { get; }
		internal WidgetHost Host { get; }
		internal WidgetDispatchResult Send( CursesMouseAction action, int column, int row,
			CursesKeyModifiers modifiers = CursesKeyModifiers.None, CursesMouseButton? button = null ) =>
			this.SendAt( action, column, row, TimeSpan.FromMilliseconds( this.tick++ ), modifiers, button );
		internal WidgetDispatchResult SendAt( CursesMouseAction action, int column, int row, TimeSpan timestamp,
			CursesKeyModifiers modifiers = CursesKeyModifiers.None, CursesMouseButton? button = null ) {
			CursesMouseButton actual = button ?? ( action is CursesMouseAction.WheelUp or CursesMouseAction.WheelDown
				or CursesMouseAction.WheelLeft or CursesMouseAction.WheelRight ? CursesMouseButton.None : CursesMouseButton.Primary );
			return this.Host.Process( WidgetInput.PointerInput( new CursesMouseEvent( action, actual, column, row, modifiers ) ), timestamp );
		}
		public void Dispose() { this.Host.Dispose(); this.View.Dispose(); }
	}
}
