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

	private static void StartDrag( Fixture fixture ) { fixture.Send( CursesMouseAction.Press, 0, 1 ); fixture.Send( CursesMouseAction.Move, 0, 5 ); }
	private static void AssertNoAutoscroll( Fixture fixture ) { long before = fixture.View.VerticalOffset; fixture.Host.Update( TimeSpan.FromSeconds( 1 ) ); Assert.Equal( before, fixture.View.VerticalOffset ); }

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
		internal void Send( CursesMouseAction action, int column, int row, CursesKeyModifiers modifiers = CursesKeyModifiers.None ) {
			CursesMouseButton button = action is CursesMouseAction.WheelUp or CursesMouseAction.WheelDown or CursesMouseAction.WheelLeft or CursesMouseAction.WheelRight
				? CursesMouseButton.None : CursesMouseButton.Primary;
			this.Host.Process( WidgetInput.PointerInput( new CursesMouseEvent( action, button, column, row, modifiers ) ), TimeSpan.FromMilliseconds( this.tick++ ) );
		}
		public void Dispose() { this.Host.Dispose(); this.View.Dispose(); }
	}
}
