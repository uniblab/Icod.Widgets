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

namespace Icod.Widgets.Tests.Controls;

using Icod.DCurses;
using Xunit;

public sealed class WidgetScrollBarTests {
	[Fact]
	public void VerticalScrollbarRendersArrowTrackAndThumbGlyphs() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Vertical ); bar.SetRange( 100, 20, 0 );
		using WidgetHost host = Host( bar, new CursesRectangle( 0, 0, 10, 1 ) );

		host.Render();

		Assert.Equal( "▲", host.Screen.StandardWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( "┃", host.Screen.StandardWindow.GetCell( 1, 0 ).Content );
		Assert.Equal( "│", host.Screen.StandardWindow.GetCell( 2, 0 ).Content );
		Assert.Equal( "▼", host.Screen.StandardWindow.GetCell( 9, 0 ).Content );
	}

	[Fact]
	public void GeometryIsAxisNeutralForShortAndNormalTracks() {
		WidgetScrollBarGeometry zero = WidgetScrollBarGeometry.Create( 0, 100, 20, 0 );
		WidgetScrollBarGeometry one = WidgetScrollBarGeometry.Create( 1, 100, 20, 0 );
		WidgetScrollBarGeometry two = WidgetScrollBarGeometry.Create( 2, 100, 20, 0 );
		WidgetScrollBarGeometry normal = WidgetScrollBarGeometry.Create( 10, 100, 25, 0 );

		Assert.Equal( WidgetScrollBarPointerTarget.None, zero.HitTest( 0 ) );
		Assert.Equal( WidgetScrollBarPointerTarget.None, one.HitTest( 0 ) );
		Assert.Equal( WidgetScrollBarPointerTarget.DecrementButton, two.HitTest( 0 ) );
		Assert.Equal( WidgetScrollBarPointerTarget.IncrementButton, two.HitTest( 1 ) );
		Assert.Equal( 0, two.TrackLength );
		Assert.Equal( 1, normal.TrackStart );
		Assert.Equal( 8, normal.TrackLength );
		Assert.Equal( 2, normal.ThumbLength );
	}

	[Fact]
	public void ScrollHereCentersThumbAndClampsEndpoints() {
		WidgetScrollBarGeometry geometry = WidgetScrollBarGeometry.Create( 12, 100, 20, 0 );

		Assert.Equal( 0, geometry.OffsetForCoordinate( 0 ) );
		Assert.Equal( 40, geometry.OffsetForCoordinate( 5 ) );
		Assert.Equal( 80, geometry.OffsetForCoordinate( 11 ) );
	}

	[Fact]
	public void HorizontalScrollbarRendersButtonsTrackAndThumb() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Horizontal ); bar.SetRange( 100, 20, 0 );
		using WidgetHost host = Host( bar, new CursesRectangle( 0, 0, 1, 10 ) );

		host.Render();

		Assert.Equal( "◄", host.Screen.StandardWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( "━", host.Screen.StandardWindow.GetCell( 0, 1 ).Content );
		Assert.Equal( "─", host.Screen.StandardWindow.GetCell( 0, 2 ).Content );
		Assert.Equal( "►", host.Screen.StandardWindow.GetCell( 0, 9 ).Content );
	}

	[Fact]
	public void BothOrientationsMapLongMaxWithoutOverflow() {
		foreach ( WidgetScrollOrientation orientation in Enum.GetValues<WidgetScrollOrientation>() ) {
			WidgetScrollBar bar = new( $"scroll-{orientation}", orientation ); bar.SetRange( long.MaxValue, 1, 0 );
			CursesRectangle bounds = WidgetScrollOrientation.Horizontal == orientation
				? new CursesRectangle( 0, 0, 1, 10 ) : new CursesRectangle( 0, 0, 10, 1 );
			using WidgetHost host = Host( bar, bounds );

			host.Process( PointerFor( orientation, CursesMouseAction.Press, 1 ), TimeSpan.Zero );
			host.Process( PointerFor( orientation, CursesMouseAction.Move, 8 ), TimeSpan.FromMilliseconds( 1 ) );
			host.Process( PointerFor( orientation, CursesMouseAction.Release, 8 ), TimeSpan.FromMilliseconds( 2 ) );

			Assert.Equal( long.MaxValue - 1, bar.Offset );
		}
	}

	[Fact]
	public void ArrowButtonsStepOneUnit() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Vertical ); bar.SetRange( 100, 10, 10 );
		using WidgetHost host = Host( bar, new CursesRectangle( 0, 0, 10, 1 ) );

		host.Process( Pointer( CursesMouseAction.Press, 0, 0 ), TimeSpan.Zero );
		Assert.Equal( 9, bar.Offset );
		host.Process( Pointer( CursesMouseAction.Press, 0, 9 ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Equal( 10, bar.Offset );
	}

	[Fact]
	public void TrackClicksPage() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Vertical ); bar.SetRange( 100, 10, 0 );
		using WidgetHost host = Host( bar, new CursesRectangle( 0, 0, 10, 1 ) );

		host.Process( Pointer( CursesMouseAction.Press, 0, 8 ), TimeSpan.Zero );

		Assert.Equal( 10, bar.Offset );
	}

	[Fact]
	public void TwoRowVerticalScrollbarRendersBothButtonsWithoutTrack() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Vertical ); bar.SetRange( 100, 10, 0 );
		using WidgetHost host = Host( bar, new CursesRectangle( 0, 0, 2, 1 ) );

		host.Render();

		Assert.Equal( "▲", host.Screen.StandardWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( "▼", host.Screen.StandardWindow.GetCell( 1, 0 ).Content );
	}

	[Fact]
	public void ThumbMapsLargeRangeWithoutOverflow() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Horizontal ); bar.SetRange( long.MaxValue, 1, 0 );
		using WidgetHost host = Host( bar, new CursesRectangle( 0, 0, 1, 10 ) );

		host.Render(); host.Process( Pointer( CursesMouseAction.Press, 1, 0 ), TimeSpan.Zero );
		host.Process( Pointer( CursesMouseAction.Move, 8, 0 ), TimeSpan.FromMilliseconds( 1 ) );
		WidgetDispatchResult result = host.Process( Pointer( CursesMouseAction.Release, 8, 0 ), TimeSpan.FromMilliseconds( 2 ) );

		Assert.Equal( long.MaxValue - 1, bar.Offset );
		Assert.All( result.Actions, action => Assert.IsType<WidgetScrollChangedAction>( action ) );
	}

	[Fact]
	public void ZeroAndShortTracksRemainUsable() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Vertical ); bar.SetRange( 0, 0, long.MaxValue );
		using WidgetHost host = Host( bar, new CursesRectangle( 0, 0, 1, 1 ) );

		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.Zero ); host.Render();

		Assert.Equal( 0, bar.Offset );
		Assert.Empty( result.Actions );
	}

	[Fact]
	public void TrackPagesAndKeyboardScrolls() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Vertical ); bar.SetRange( 100, 10, 0 );
		using WidgetHost host = Host( bar, new CursesRectangle( 0, 0, 10, 1 ) );

		host.Process( Pointer( CursesMouseAction.Press, 0, 8 ), TimeSpan.Zero );
		Assert.Equal( 10, bar.Offset );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Equal( 11, bar.Offset );
		host.Process( WidgetInput.Key( WidgetKey.End ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( 90, bar.Offset );
	}

	[Fact]
	public void ScrollbarDoesNotMoveCaret() {
		WidgetTextDocument document = new( [ "one\ntwo\nthree".AsMemory() ] ); using WidgetTextViewState view = new( document ); view.SetSelection( 5, 5 );
		WidgetTextEditor editor = new( "editor", view ); WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Vertical ); bar.SetRange( 100, 10, 0 );
		WidgetStack root = new( "root", WidgetOrientation.Horizontal ); root.Add( editor ); root.Add( bar );
		using WidgetHost host = new( new CursesScreen( 20, 5 ), WidgetTheme.Default ); host.Attach( root ); host.Arrange( host.Screen.Bounds ); Assert.True( host.Focus( bar ) );

		host.Process( WidgetInput.Key( WidgetKey.PageDown ), TimeSpan.Zero );

		Assert.Equal( 5, view.Caret );
		Assert.Equal( 10, bar.Offset );
	}

	[Fact]
	public void RapidWheelAndDragInputStaysInRange() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Horizontal ); bar.SetRange( long.MaxValue, 10, long.MaxValue / 2 );
		using WidgetHost host = Host( bar, new CursesRectangle( 0, 0, 1, 10 ) ); TimeSpan timestamp = TimeSpan.Zero;

		for ( int index = 0; index < 100; index++ ) {
			host.Process( WidgetInput.PointerInput( new CursesMouseEvent( 0 == index % 2 ? CursesMouseAction.WheelLeft : CursesMouseAction.WheelRight,
				CursesMouseButton.None, 5, 0 ) ), timestamp ); timestamp += TimeSpan.FromMilliseconds( 1 );
			host.Process( Pointer( CursesMouseAction.Press, 5, 0 ), timestamp ); timestamp += TimeSpan.FromMilliseconds( 1 );
			host.Process( Pointer( CursesMouseAction.Move, 0 == index % 2 ? 0 : 9, 0 ), timestamp ); timestamp += TimeSpan.FromMilliseconds( 1 );
			host.Process( Pointer( CursesMouseAction.Release, 0 == index % 2 ? 0 : 9, 0 ), timestamp ); timestamp += TimeSpan.FromMilliseconds( 1 );
		}

		Assert.InRange( bar.Offset, 0, long.MaxValue - 10 );
	}

	private static WidgetHost Host( Widget widget, CursesRectangle bounds ) {
		WidgetHost host = new( new CursesScreen( Math.Max( 10, bounds.Columns ), Math.Max( 10, bounds.Rows ) ), WidgetTheme.Default );
		host.Attach( widget ); widget.Arrange( bounds ); Assert.True( host.Focus( widget ) ); return host;
	}

	private static WidgetInput Pointer( CursesMouseAction action, int column, int row ) =>
		WidgetInput.PointerInput( new CursesMouseEvent( action, CursesMouseButton.Primary, column, row ) );

	private static WidgetInput PointerFor( WidgetScrollOrientation orientation, CursesMouseAction action, int coordinate ) =>
		WidgetScrollOrientation.Horizontal == orientation ? Pointer( action, coordinate, 0 ) : Pointer( action, 0, coordinate );
}
