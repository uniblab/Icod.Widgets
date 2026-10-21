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
	public void ShiftTrackClickScrollsHereWithoutPaging() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Vertical ); bar.SetRange( 100, 20, 0 );
		using WidgetHost host = Host( bar, new CursesRectangle( 0, 0, 10, 1 ) );

		WidgetDispatchResult result = host.Process( Pointer( CursesMouseAction.Press, 0, 8, CursesKeyModifiers.Shift ), TimeSpan.Zero );

		Assert.Equal( 80, bar.Offset );
		Assert.Single( result.Actions );
		host.Process( Pointer( CursesMouseAction.Release, 0, 8, CursesKeyModifiers.Shift ), TimeSpan.FromMilliseconds( 1 ) );
		bar.SetRange( 100, 20, 10 );
		host.Process( Pointer( CursesMouseAction.Press, 0, 0, CursesKeyModifiers.Shift ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( 9, bar.Offset );
	}

	[Fact]
	public void DirectionalKeyPairsMatchWin32Vocabulary() {
		foreach ( WidgetScrollOrientation orientation in Enum.GetValues<WidgetScrollOrientation>() ) {
			WidgetScrollBar bar = new( $"scroll-{orientation}", orientation ); bar.SetRange( 100, 20, 10 );
			CursesRectangle bounds = WidgetScrollOrientation.Horizontal == orientation
				? new CursesRectangle( 0, 0, 1, 10 ) : new CursesRectangle( 0, 0, 10, 1 );
			using WidgetHost host = Host( bar, bounds );

			host.Process( WidgetInput.Key( WidgetKey.Up ), TimeSpan.Zero );
			host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.FromMilliseconds( 1 ) );
			Assert.Equal( 8, bar.Offset );
			host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.FromMilliseconds( 2 ) );
			host.Process( WidgetInput.Key( WidgetKey.Right ), TimeSpan.FromMilliseconds( 3 ) );
			Assert.Equal( 10, bar.Offset );
		}
	}

	[Fact]
	public void TrackAndButtonsUseSameSemanticCommandsOnBothAxes() {
		foreach ( WidgetScrollOrientation orientation in Enum.GetValues<WidgetScrollOrientation>() ) {
			WidgetScrollBar bar = new( $"scroll-{orientation}", orientation ); bar.SetRange( 100, 20, 20 );
			CursesRectangle bounds = WidgetScrollOrientation.Horizontal == orientation
				? new CursesRectangle( 0, 0, 1, 10 ) : new CursesRectangle( 0, 0, 10, 1 );
			using WidgetHost host = Host( bar, bounds );

			host.Process( PointerFor( orientation, CursesMouseAction.Press, 0 ), TimeSpan.Zero );
			host.Process( PointerFor( orientation, CursesMouseAction.Release, 0 ), TimeSpan.FromMilliseconds( 1 ) );
			Assert.Equal( 19, bar.Offset );
			host.Process( PointerFor( orientation, CursesMouseAction.Press, 8 ), TimeSpan.FromMilliseconds( 2 ) );
			host.Process( PointerFor( orientation, CursesMouseAction.Release, 8 ), TimeSpan.FromMilliseconds( 3 ) );
			Assert.Equal( 39, bar.Offset );
		}
	}

	[Fact]
	public void BoundaryCommandsAreHandledWithoutActions() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Horizontal ); bar.SetRange( 100, 20, 0 );
		using WidgetHost host = Host( bar, new CursesRectangle( 0, 0, 1, 10 ) );

		WidgetDispatchResult start = host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.Zero );
		WidgetDispatchResult home = host.Process( WidgetInput.Key( WidgetKey.Home ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.True( start.Handled ); Assert.Empty( start.Actions );
		Assert.True( home.Handled ); Assert.Empty( home.Actions );
		bar.SetRange( 100, 20, 80 );
		WidgetDispatchResult end = host.Process( WidgetInput.Key( WidgetKey.End ), TimeSpan.FromMilliseconds( 2 ) );
		WidgetDispatchResult increment = host.Process( WidgetInput.Key( WidgetKey.Right ), TimeSpan.FromMilliseconds( 3 ) );
		Assert.True( end.Handled ); Assert.Empty( end.Actions );
		Assert.True( increment.Handled ); Assert.Empty( increment.Actions );
	}

	[Fact]
	public void ButtonHoldRepeatsAfterDelayAtBoundedCadence() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Vertical ); bar.SetRange( 1_000, 10, 500 );
		using WidgetHost host = Host( bar, new CursesRectangle( 0, 0, 10, 1 ) );

		host.Process( Pointer( CursesMouseAction.Press, 0, 0 ), TimeSpan.Zero );
		Assert.Equal( 499, bar.Offset );
		Assert.Empty( host.Update( TimeSpan.FromMilliseconds( 399 ) ).Actions );
		Assert.Equal( 499, bar.Offset );
		Assert.Single( host.Update( TimeSpan.FromMilliseconds( 400 ) ).Actions );
		Assert.Equal( 498, bar.Offset );
		Assert.Equal( 3, host.Update( TimeSpan.FromMilliseconds( 550 ) ).Actions.Count );
		Assert.Equal( 495, bar.Offset );
		Assert.InRange( host.Update( TimeSpan.FromHours( 1 ) ).Actions.Count, 1, WidgetScrollBarInteraction.MaximumWorkPerUpdate );
	}

	[Fact]
	public void TrackHoldStopsWhenThumbReachesPointer() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Vertical ); bar.SetRange( 100, 20, 0 );
		using WidgetHost host = Host( bar, new CursesRectangle( 0, 0, 10, 1 ) );

		host.Process( Pointer( CursesMouseAction.Press, 0, 4 ), TimeSpan.Zero );
		Assert.Equal( 20, bar.Offset );
		host.Update( TimeSpan.FromMilliseconds( 400 ) );
		Assert.Equal( 40, bar.Offset );
		Assert.Empty( host.Update( TimeSpan.FromSeconds( 1 ) ).Actions );
		Assert.Equal( 40, bar.Offset );
	}

	[Fact]
	public void RepeatCancelsAcrossReleaseFocusArrangeModalDisableAndDetach() {
		foreach ( string reason in new[] { "release", "focus", "arrange", "modal", "disable", "detach" } ) {
			WidgetScrollBar bar = new( $"scroll-{reason}", WidgetScrollOrientation.Vertical ); bar.SetRange( 100, 10, 50 );
			using WidgetHost host = new( new CursesScreen( 10, 10 ), WidgetTheme.Default );
			host.Attach( bar ); bar.Arrange( new CursesRectangle( 0, 0, 10, 1 ) ); Assert.True( host.Focus( bar ) );
			host.Process( Pointer( CursesMouseAction.Press, 0, 0 ), TimeSpan.Zero );
			Assert.Equal( 49, bar.Offset );

			switch ( reason ) {
				case "release": host.Process( Pointer( CursesMouseAction.Release, 0, 0 ), TimeSpan.FromMilliseconds( 1 ) ); break;
				case "focus":
					WidgetButton button = new( $"button-{reason}", "Other", "other" ); host.Attach( button );
					button.Arrange( new CursesRectangle( 0, 2, 1, 8 ) ); Assert.True( host.Focus( button ) ); break;
				case "arrange": bar.Arrange( new CursesRectangle( 0, 0, 10, 1 ) ); break;
				case "modal": _ = host.ShowModal( WidgetMessageDialog.Ok( $"dialog-{reason}", "Dialog", "Text" ) ); break;
				case "disable": bar.IsEnabled = false; break;
				case "detach": Assert.True( host.Detach( bar ) ); break;
			}

			Assert.Empty( host.Update( TimeSpan.FromSeconds( 1 ) ).Actions );
			Assert.Equal( 49, bar.Offset );
		}
	}

	[Theory]
	[InlineData( WidgetScrollOrientation.Vertical )]
	[InlineData( WidgetScrollOrientation.Horizontal )]
	public void SecondaryReleaseOpensOrientationSpecificCommands( WidgetScrollOrientation orientation ) {
		WidgetScrollBar bar = new( "scroll", orientation ); bar.SetRange( 100, 20, 20 );
		using WidgetHost host = ContextHost( bar, orientation );

		host.Process( SecondaryFor( orientation, CursesMouseAction.Press, 5 ), TimeSpan.Zero );
		host.Process( SecondaryFor( orientation, CursesMouseAction.Release, 5 ), TimeSpan.FromMilliseconds( 1 ) );

		Assert.True( bar.ContextMenu.IsOpen );
		string[] expected = WidgetScrollOrientation.Vertical == orientation
			? [ "Scroll Here", "-", "Top", "Bottom", "-", "Page Up", "Page Down", "-", "Scroll Up", "Scroll Down" ]
			: [ "Scroll Here", "-", "Left Edge", "Right Edge", "-", "Page Left", "Page Right", "-", "Scroll Left", "Scroll Right" ];
		Assert.Equal( expected, bar.ContextMenu.RootItems.Select( item => WidgetMenuItemKind.Separator == item.Kind ? "-" : item.Text ) );
	}

	[Fact]
	public void ContextCommandsShareDirectScrollBehavior() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Vertical ); bar.SetRange( 100, 20, 0 );
		using WidgetHost host = ContextHost( bar, WidgetScrollOrientation.Vertical );

		OpenContext( host, WidgetScrollOrientation.Vertical, 8 );
		WidgetDispatchResult scrollHere = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( 80, bar.Offset );
		Assert.Single( scrollHere.Actions, action => action is WidgetScrollChangedAction );
		Assert.DoesNotContain( scrollHere.Actions, action => action is WidgetInvokedAction );
		Assert.Same( bar, host.FocusedWidget );

		OpenContext( host, WidgetScrollOrientation.Vertical, 8, 3 );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.FromMilliseconds( 5 ) );
		WidgetDispatchResult top = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 6 ) );
		Assert.Equal( 0, bar.Offset );
		Assert.Single( top.Actions, action => action is WidgetScrollChangedAction );
	}

	[Fact]
	public void ContextItemsDisableAtBoundaries() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Vertical ); bar.SetRange( 100, 20, 0 );
		using WidgetHost host = ContextHost( bar, WidgetScrollOrientation.Vertical );

		OpenContext( host, WidgetScrollOrientation.Vertical, 8 );
		Assert.True( Item( bar, "scroll-here" ).IsEnabled );
		Assert.False( Item( bar, "start" ).IsEnabled );
		Assert.False( Item( bar, "page-decrement" ).IsEnabled );
		Assert.False( Item( bar, "unit-decrement" ).IsEnabled );
		Assert.True( Item( bar, "end" ).IsEnabled );
		bar.ContextMenu.Close(); bar.SetRange( 100, 20, 80 );

		OpenContext( host, WidgetScrollOrientation.Vertical, 1, 3 );
		Assert.True( Item( bar, "start" ).IsEnabled );
		Assert.False( Item( bar, "end" ).IsEnabled );
		Assert.False( Item( bar, "page-increment" ).IsEnabled );
		Assert.False( Item( bar, "unit-increment" ).IsEnabled );
	}

	[Fact]
	public void ShiftF10AnchorsStandaloneMenuAtThumb() {
		WidgetScrollBar bar = new( "scroll", WidgetScrollOrientation.Vertical ); bar.SetRange( 100, 20, 40 );
		using WidgetHost host = new( new CursesScreen( 30, 30 ), WidgetTheme.Default ); host.Attach( bar );
		bar.Arrange( new CursesRectangle( 2, 5, 10, 1 ) ); Assert.True( host.Focus( bar ) );

		host.Process( WidgetInput.FunctionKey( 10, WidgetInputModifiers.Shift ), TimeSpan.Zero ); host.Render();

		Assert.True( bar.ContextMenu.IsOpen );
		Assert.Equal( 6, bar.ContextMenu.RenderBounds.Row );
		Assert.Equal( 5, bar.ContextMenu.RenderBounds.Column );
		Assert.False( Item( bar, "scroll-here" ).IsEnabled );
	}

	[Fact]
	public void ContextSequenceCancelsAcrossLifecycleChanges() {
		foreach ( string reason in new[] { "range", "arrange", "modal", "disable", "detach" } ) {
			WidgetScrollBar bar = new( $"scroll-{reason}", WidgetScrollOrientation.Vertical ); bar.SetRange( 100, 20, 20 );
			using WidgetHost host = ContextHost( bar, WidgetScrollOrientation.Vertical );
			host.Process( SecondaryFor( WidgetScrollOrientation.Vertical, CursesMouseAction.Press, 5 ), TimeSpan.Zero );

			switch ( reason ) {
				case "range": bar.SetRange( 200, 20, 20 ); break;
				case "arrange": bar.Arrange( new CursesRectangle( 1, 1, 10, 1 ) ); break;
				case "modal": _ = host.ShowModal( WidgetMessageDialog.Ok( $"dialog-{reason}", "Dialog", "Text" ) ); break;
				case "disable": bar.IsEnabled = false; break;
				case "detach": Assert.True( host.Detach( bar ) ); break;
			}

			host.Process( SecondaryFor( WidgetScrollOrientation.Vertical, CursesMouseAction.Release, 5 ), TimeSpan.FromMilliseconds( 1 ) );
			Assert.False( bar.ContextMenu.IsOpen );
		}

		WidgetScrollBar repeating = new( "scroll-repeating", WidgetScrollOrientation.Vertical ); repeating.SetRange( 100, 20, 20 );
		using WidgetHost repeatHost = ContextHost( repeating, WidgetScrollOrientation.Vertical );
		repeatHost.Process( Pointer( CursesMouseAction.Press, 2, 11 ), TimeSpan.Zero );
		Assert.Equal( 21, repeating.Offset );
		OpenContext( repeatHost, WidgetScrollOrientation.Vertical, 5, 1 );
		Assert.True( repeating.ContextMenu.IsOpen );
		Assert.Empty( repeatHost.Update( TimeSpan.FromSeconds( 1 ) ).Actions );
		Assert.Equal( 21, repeating.Offset );
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

	private static WidgetInput Pointer( CursesMouseAction action, int column, int row, CursesKeyModifiers modifiers = CursesKeyModifiers.None ) =>
		WidgetInput.PointerInput( new CursesMouseEvent( action, CursesMouseButton.Primary, column, row, modifiers ) );

	private static WidgetInput PointerFor( WidgetScrollOrientation orientation, CursesMouseAction action, int coordinate ) =>
		WidgetScrollOrientation.Horizontal == orientation ? Pointer( action, coordinate, 0 ) : Pointer( action, 0, coordinate );

	private static WidgetHost ContextHost( WidgetScrollBar bar, WidgetScrollOrientation orientation ) {
		WidgetHost host = new( new CursesScreen( 40, 30 ), WidgetTheme.Default ); host.Attach( bar );
		bar.Arrange( WidgetScrollOrientation.Horizontal == orientation
			? new CursesRectangle( 2, 2, 1, 10 ) : new CursesRectangle( 2, 2, 10, 1 ) );
		Assert.True( host.Focus( bar ) ); return host;
	}

	private static WidgetInput SecondaryFor( WidgetScrollOrientation orientation, CursesMouseAction action, int coordinate ) {
		int column = 2 + ( WidgetScrollOrientation.Horizontal == orientation ? coordinate : 0 );
		int row = 2 + ( WidgetScrollOrientation.Vertical == orientation ? coordinate : 0 );
		return WidgetInput.PointerInput( new CursesMouseEvent( action, CursesMouseButton.Secondary, column, row ) );
	}

	private static void OpenContext( WidgetHost host, WidgetScrollOrientation orientation, int coordinate, int milliseconds = 0 ) {
		host.Process( SecondaryFor( orientation, CursesMouseAction.Press, coordinate ), TimeSpan.FromMilliseconds( milliseconds ) );
		host.Process( SecondaryFor( orientation, CursesMouseAction.Release, coordinate ), TimeSpan.FromMilliseconds( milliseconds + 1 ) );
	}

	private static WidgetMenuItem Item( WidgetScrollBar bar, string id ) => Assert.Single( bar.ContextMenu.RootItems, item => item.Id == id );
}
