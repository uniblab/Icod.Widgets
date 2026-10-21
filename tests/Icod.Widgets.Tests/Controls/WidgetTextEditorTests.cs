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

using System.Globalization;
using System.Text;
using Icod.DCurses;
using Xunit;

public sealed class WidgetTextEditorTests {
	[Fact]
	public void ControlDownScrollsOneDisplayedRowWithoutMovingVisibleCaret() {
		using Fixture fixture = new( "a\nb\nc\nd" ); fixture.View.SetSelection( 2, 2 );

		WidgetDispatchResult result = fixture.Send( WidgetInput.Key( WidgetKey.Down, WidgetInputModifiers.Control ) );

		Assert.Equal( new WidgetTextViewportAnchor( 1, 0 ), fixture.View.ViewportAnchor );
		Assert.Equal( 2, fixture.View.Caret );
		Assert.Empty( result.Actions );
	}

	[Fact]
	public void ControlDownPullsCaretAlongTopEdge() {
		using Fixture fixture = new( "a\nb\nc\nd" ); fixture.View.SetSelection( 0, 0 );

		WidgetDispatchResult result = fixture.Send( WidgetInput.Key( WidgetKey.Down, WidgetInputModifiers.Control ) );

		Assert.Equal( new WidgetTextViewportAnchor( 1, 0 ), fixture.View.ViewportAnchor );
		Assert.Equal( 2, fixture.View.Caret );
		Assert.IsType<WidgetTextSelectionChangedAction>( Assert.Single( result.Actions ) );
	}

	[Fact]
	public void ControlUpPullsCaretAlongBottomEdge() {
		using Fixture fixture = new( "a\nb\nc\nd" ); fixture.View.SetViewport( 0, new WidgetTextViewportAnchor( 1, 0 ) ); fixture.View.SetSelection( 6, 6 );

		WidgetDispatchResult result = fixture.Send( WidgetInput.Key( WidgetKey.Up, WidgetInputModifiers.Control ) );

		Assert.Equal( new WidgetTextViewportAnchor( 0, 0 ), fixture.View.ViewportAnchor );
		Assert.Equal( 4, fixture.View.Caret );
		Assert.IsType<WidgetTextSelectionChangedAction>( Assert.Single( result.Actions ) );
	}

	[Fact]
	public void ControlScrollPreservesPreferredVisualColumnAcrossWrappedRows() {
		using Fixture fixture = new( "abcdefghi", new WidgetEditorOptions { WrapMode = CursesTextWrapMode.TextElement } );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 1, 3 ) ); fixture.View.SetSelection( 2, 2 );

		fixture.Send( WidgetInput.Key( WidgetKey.Down, WidgetInputModifiers.Control ) );
		Assert.Equal( ( 5L, 2 ), ( fixture.View.Caret, fixture.View.PreferredColumn ) );
		fixture.Send( WidgetInput.Key( WidgetKey.Down, WidgetInputModifiers.Control ) );

		Assert.Equal( ( 8L, 2 ), ( fixture.View.Caret, fixture.View.PreferredColumn ) );
	}

	[Fact]
	public void ControlShiftDoesNotExtendSelection() {
		using Fixture fixture = new( "aa\nbb\ncc\ndd" ); fixture.View.SetSelection( 3, 4 );

		WidgetDispatchResult result = fixture.Send( WidgetInput.Key( WidgetKey.Down, WidgetInputModifiers.Control | WidgetInputModifiers.Shift ) );

		Assert.Equal( ( 3L, 4L ), ( fixture.View.Anchor, fixture.View.Caret ) );
		Assert.Empty( result.Actions );
	}

	[Fact]
	public void OrdinaryVerticalMovementRemainsCaretNavigation() {
		using Fixture fixture = new( "a\nb\nc\nd" ); fixture.View.SetSelection( 0, 0 );

		fixture.Send( WidgetInput.Key( WidgetKey.Down ) );

		Assert.Equal( 2, fixture.View.Caret );
		Assert.Equal( new WidgetTextViewportAnchor( 0, 0 ), fixture.View.ViewportAnchor );
	}

	[Fact]
	public void MovementAndDeletionRespectTextElements() {
		using Fixture fixture = new( "a\u0301界👩‍💻\n" ); fixture.View.SetSelection( 9, 9 );

		fixture.Send( WidgetInput.Key( WidgetKey.Left ) );
		fixture.Send( WidgetInput.Key( WidgetKey.Backspace ) );

		Assert.Equal( "a\u0301界\n", fixture.Text );
		Assert.Equal( 3, fixture.View.Caret );
	}

	[Fact]
	public void WordAndPageNavigation() {
		using Fixture fixture = new( "one two\nshort\nthird line\nfourth line" );

		fixture.Send( WidgetInput.Key( WidgetKey.Right, WidgetInputModifiers.Control ) );
		Assert.Equal( 3, fixture.View.Caret );
		fixture.Send( WidgetInput.Key( WidgetKey.PageDown ) );
		Assert.Equal( 17, fixture.View.Caret );
		fixture.Send( WidgetInput.Key( WidgetKey.End, WidgetInputModifiers.Control | WidgetInputModifiers.Shift ) );
		Assert.Equal( 17, fixture.View.Anchor );
		Assert.Equal( fixture.Document.Length, fixture.View.Caret );
	}

	[Fact]
	public void PageNavigationCountsWrappedVisualRows() {
		using Fixture fixture = new( "abcdefghijklmnop", new WidgetEditorOptions { WrapMode = CursesTextWrapMode.TextElement } );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 3, 4 ) ); fixture.View.SetSelection( 1, 1 );

		fixture.Send( WidgetInput.Key( WidgetKey.PageDown ) );

		Assert.Equal( 9, fixture.View.Caret );
		Assert.Equal( 1, fixture.View.PreferredColumn );
	}

	[Fact]
	public void ShiftDownExtendsSelectionAcrossWrappedVisualRow() {
		using Fixture fixture = new( "abcdefgh", new WidgetEditorOptions { WrapMode = CursesTextWrapMode.TextElement } );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 2, 4 ) ); fixture.View.SetSelection( 1, 1 );

		fixture.Send( WidgetInput.Key( WidgetKey.Down, WidgetInputModifiers.Shift ) );

		Assert.Equal( ( 1L, 5L ), ( fixture.View.Anchor, fixture.View.Caret ) );
	}

	[Fact]
	public void SelectionReplacementIsOneTransaction() {
		using Fixture fixture = new( "alpha beta" ); fixture.View.SetSelection( 0, 5 );

		WidgetDispatchResult edited = fixture.Send( WidgetInput.TextInput( new Rune( 'X' ) ) );
		WidgetTextEditedAction action = Assert.IsType<WidgetTextEditedAction>( edited.Actions[ 0 ] );
		Assert.Equal( fixture.Document.Id, action.DocumentId );
		Assert.Equal( "X beta", fixture.Text );

		Assert.Equal( WidgetCommandResult.Executed, fixture.Host.ExecuteEditCommand( WidgetEditCommand.Undo, fixture.NextTime ) );
		Assert.Equal( "alpha beta", fixture.Text );
		Assert.Equal( ( 0L, 5L ), ( fixture.View.Anchor, fixture.View.Caret ) );
	}

	[Fact]
	public void IndentAndOutdentAreUndoable() {
		using Fixture fixture = new( "one\ntwo\nthree" ); fixture.View.SetSelection( 0, 7 );

		fixture.Send( WidgetInput.Key( WidgetKey.Tab ) );
		Assert.Equal( "\tone\n\ttwo\nthree", fixture.Text );
		fixture.Host.ExecuteEditCommand( WidgetEditCommand.Undo, fixture.NextTime );
		Assert.Equal( "one\ntwo\nthree", fixture.Text );

		fixture.View.SetSelection( 0, 7 ); fixture.Send( WidgetInput.Key( WidgetKey.Tab, WidgetInputModifiers.Shift ) );
		Assert.Equal( "one\ntwo\nthree", fixture.Text );
		fixture.View.SetSelection( 0, 9 ); fixture.Send( WidgetInput.Key( WidgetKey.Tab ) );
		fixture.Send( WidgetInput.Key( WidgetKey.Tab, WidgetInputModifiers.Shift ) );
		Assert.Equal( "one\ntwo\nthree", fixture.Text );
		fixture.Host.ExecuteEditCommand( WidgetEditCommand.Undo, fixture.NextTime );
		Assert.Equal( "\tone\n\ttwo\n\tthree", fixture.Text );
	}

	[Fact]
	public void OverwriteDoesNotSplitClusterOrCrossLineEnd() {
		using Fixture fixture = new( "a\u0301界\nZ" ); fixture.View.SetSelection( 0, 0 );
		fixture.Send( WidgetInput.Key( WidgetKey.Insert ) );

		fixture.Send( WidgetInput.TextInput( new Rune( 'X' ) ) );
		Assert.Equal( "X界\nZ", fixture.Text );
		fixture.View.SetSelection( 2, 2 ); fixture.Send( WidgetInput.TextInput( new Rune( 'Y' ) ) );

		Assert.Equal( "X界Y\nZ", fixture.Text );
	}

	[Fact]
	public void ReadOnlyCannotMutate() {
		using Fixture fixture = new( "locked" ); fixture.Editor.IsReadOnly = true; fixture.View.SetSelection( 0, 6 );

		fixture.Send( WidgetInput.TextInput( new Rune( 'X' ) ) );
		fixture.Send( WidgetInput.Key( WidgetKey.Backspace ) );
		fixture.Send( WidgetInput.Paste( "paste" ) );

		Assert.Equal( "locked", fixture.Text );
		Assert.Equal( WidgetCommandResult.Disabled, fixture.Host.ExecuteEditCommand( WidgetEditCommand.Undo, fixture.NextTime ) );
	}

	[Fact]
	public void AssociatedTextInsertedOnce() {
		using Fixture fixture = new( "" );
		WidgetInput input = WidgetInput.Key( WidgetKey.Character, character: new Rune( 'x' ) ) with { Text = "xy" };

		fixture.Send( input );

		Assert.Equal( "xy", fixture.Text );
	}

	[Fact]
	public void BracketedPasteCommitsOnlyAtEnd() {
		using Fixture fixture = new( "ab" ); fixture.View.SetSelection( 1, 1 );

		fixture.Send( WidgetInput.PasteChunk( CursesPastePhase.Begin ) );
		fixture.Send( WidgetInput.PasteChunk( CursesPastePhase.Data, "X\r\n" ) );
		fixture.Send( WidgetInput.PasteChunk( CursesPastePhase.Data, "Y" ) );
		Assert.Equal( "ab", fixture.Text );
		fixture.Send( WidgetInput.PasteChunk( CursesPastePhase.End ) );

		Assert.Equal( "aX\r\nYb", fixture.Text );
		fixture.Host.ExecuteEditCommand( WidgetEditCommand.Undo, fixture.NextTime );
		Assert.Equal( "ab", fixture.Text );
	}

	[Fact]
	public void InterruptedOrOversizedPasteDoesNotMutate() {
		WidgetEditorOptions options = new() { MaximumPendingPasteUnits = 4 };
		using Fixture fixture = new( "safe", options );

		fixture.Send( WidgetInput.PasteChunk( CursesPastePhase.Begin ) );
		fixture.Send( WidgetInput.PasteChunk( CursesPastePhase.Data, "12345" ) );
		fixture.Send( WidgetInput.PasteChunk( CursesPastePhase.End ) );
		Assert.Equal( "safe", fixture.Text );

		fixture.Send( WidgetInput.PasteChunk( CursesPastePhase.Begin ) );
		fixture.Send( WidgetInput.PasteChunk( CursesPastePhase.Data, "OK" ) );
		fixture.Send( WidgetInput.Key( WidgetKey.Left ) );
		fixture.Send( WidgetInput.PasteChunk( CursesPastePhase.End ) );
		Assert.Equal( "safe", fixture.Text );
	}

	[Fact]
	public void FullActionQueueLeavesEditUnchanged() {
		using Fixture fixture = new( "before" ); fixture.View.SetSelection( 0, 6 );
		for ( int index = 0; index < WidgetLimits.MaximumPendingActions - 1; index++ )
			fixture.Host.Enqueue( new WidgetInvokedAction( "seed", index.ToString() ) );

		Assert.Throws<InvalidOperationException>( () => fixture.Host.Process( WidgetInput.TextInput( new Rune( 'X' ) ), fixture.NextTime ) );

		Assert.Equal( "before", fixture.Text );
		Assert.Equal( ( 0L, 6L ), ( fixture.View.Anchor, fixture.View.Caret ) );
	}

	[Fact]
	public void RenderProjectsTabsWideTextAndSelection() {
		using Fixture fixture = new( "\t界e\u0301\nnext" ); fixture.View.SetSelection( 0, 2 );

		fixture.Host.Render();

		Assert.Equal( "界", fixture.Host.Screen.StandardWindow.GetCell( 0, 4 ).Content );
		Assert.True( fixture.Host.Screen.StandardWindow.GetCell( 0, 5 ).IsContinuation );
		Assert.Equal( "n", fixture.Host.Screen.StandardWindow.GetCell( 1, 0 ).Content );
	}

	[Fact]
	public void LineNumberGutterDisabledPreservesExistingTextCoordinates() {
		using Fixture fixture = new( "alpha", new WidgetEditorOptions { ShowLineNumbers = false } );

		fixture.Host.Render();

		Assert.Equal( "a", fixture.Host.Screen.StandardWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( "l", fixture.Host.Screen.StandardWindow.GetCell( 0, 1 ).Content );
	}

	[Fact]
	public void LineNumberGutterUsesDocumentDigitWidthAndStaysFixedDuringHorizontalScroll() {
		string text = string.Join( '\n', Enumerable.Range( 1, 10 ).Select( index => 1 == index ? "abcdef" : $"line {index}" ) );
		using Fixture fixture = new( text, new WidgetEditorOptions { ShowLineNumbers = true } );
		fixture.View.SetScroll( 2, 0 );

		fixture.Host.Render();

		Assert.True( fixture.Host.Screen.StandardWindow.GetCell( 0, 0 ).IsBlank );
		Assert.Equal( "1", fixture.Host.Screen.StandardWindow.GetCell( 0, 1 ).Content );
		Assert.True( fixture.Host.Screen.StandardWindow.GetCell( 0, 2 ).IsBlank );
		Assert.Equal( "c", fixture.Host.Screen.StandardWindow.GetCell( 0, 3 ).Content );
	}

	[Fact]
	public void LineNumberGutterClipsPartiallyScrolledTabToTextViewport() {
		using Fixture fixture = new( "\tvalue", new WidgetEditorOptions { ShowLineNumbers = true } );
		fixture.View.SetScroll( 2, 0 );

		fixture.Host.Render();

		Assert.Equal( "1", fixture.Host.Screen.StandardWindow.GetCell( 0, 0 ).Content );
		Assert.True( fixture.Host.Screen.StandardWindow.GetCell( 0, 1 ).IsBlank );
	}

	[Fact]
	public void DigitThresholdReflowsTextWithoutStaleProjection() {
		using Fixture fixture = new( string.Join( '\n', Enumerable.Range( 1, 9 ).Select( index => $"line {index}" ) ), new WidgetEditorOptions { ShowLineNumbers = true } );
		fixture.Host.Render();
		Assert.Equal( "l", fixture.Host.Screen.StandardWindow.GetCell( 0, 2 ).Content );
		long end = fixture.Document.Length;
		WidgetTextEditResult result = fixture.Document.Apply( [ new WidgetTextEdit( end, 0, "\nline 10" ) ], fixture.Document.Revision,
			new WidgetTextEditContext( fixture.View.Id, 0, 0, end + 8, end + 8, WidgetTextEditKind.Programmatic, TimeSpan.Zero ) );
		Assert.Equal( WidgetTextEditStatus.Applied, result.Status ); fixture.View.SetSelection( 0, 0 ); fixture.View.SetScroll( 0, 0 );

		fixture.Host.Render();

		Assert.Equal( "1", fixture.Host.Screen.StandardWindow.GetCell( 0, 1 ).Content );
		Assert.True( fixture.Host.Screen.StandardWindow.GetCell( 0, 2 ).IsBlank );
		Assert.Equal( "l", fixture.Host.Screen.StandardWindow.GetCell( 0, 3 ).Content );
	}

	[Fact]
	public void WrappedLineNumberAppearsOnlyOnFirstVisualRow() {
		using Fixture fixture = new( "abcdef\nz", new WidgetEditorOptions { ShowLineNumbers = true, WrapMode = CursesTextWrapMode.TextElement } );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 3, 6 ) );

		fixture.Host.Render();

		Assert.Equal( "1", fixture.Host.Screen.StandardWindow.GetCell( 0, 0 ).Content );
		Assert.True( fixture.Host.Screen.StandardWindow.GetCell( 1, 0 ).IsBlank );
		Assert.Equal( "2", fixture.Host.Screen.StandardWindow.GetCell( 2, 0 ).Content );
	}

	[Fact]
	public void LineNumberGutterUsesAsciiDigitsUnderNonLatinCulture() {
		CultureInfo originalCulture = CultureInfo.CurrentCulture;
		try {
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo( "ar-SA" );
			using Fixture fixture = new( string.Join( '\n', Enumerable.Range( 1, 12 ).Select( index => $"line {index}" ) ), new WidgetEditorOptions { ShowLineNumbers = true } );
			fixture.View.SetScroll( 0, 11 ); fixture.Host.Render();

			Assert.Equal( "1", fixture.Host.Screen.StandardWindow.GetCell( 0, 0 ).Content );
			Assert.Equal( "2", fixture.Host.Screen.StandardWindow.GetCell( 0, 1 ).Content );
		} finally { CultureInfo.CurrentCulture = originalCulture; }
	}

	[Fact]
	public void LineNumberGutterSuppressesAndRestoresWhenTextNeedsTheFinalColumn() {
		using Fixture fixture = new( "abc", new WidgetEditorOptions { ShowLineNumbers = true } );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 1, 1 ) ); fixture.Host.Render();
		Assert.Equal( "a", fixture.Host.Screen.StandardWindow.GetCell( 0, 0 ).Content );

		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 1, 4 ) ); fixture.Host.Render();

		Assert.Equal( "1", fixture.Host.Screen.StandardWindow.GetCell( 0, 0 ).Content );
		Assert.True( fixture.Host.Screen.StandardWindow.GetCell( 0, 1 ).IsBlank );
		Assert.Equal( "a", fixture.Host.Screen.StandardWindow.GetCell( 0, 2 ).Content );
	}

	[Fact]
	public void HorizontalScrollRangeUsesTextViewportWidth() {
		using Fixture fixture = new( string.Join( '\n', Enumerable.Range( 1, 10 ).Select( index => $"line {index}" ) ), new WidgetEditorOptions { ShowLineNumbers = true } );

		fixture.Host.Render();

		Assert.Equal( 17, fixture.Editor.HorizontalScrollRange.Viewport );
	}

	[Fact]
	public void FittingDocumentDoesNotRenderOrReserveScrollbar() {
		using Fixture fixture = new( "abcd", new WidgetEditorOptions { ShowVerticalScrollBar = true } );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 3, 4 ) );

		fixture.Host.Render();

		Assert.Equal( "d", fixture.Host.Screen.StandardWindow.GetCell( 0, 3 ).Content );
		Assert.DoesNotContain( new[] { "▲", "▼", "│", "┃" }, glyph => glyph == fixture.Host.Screen.StandardWindow.GetCell( 0, 3 ).Content );
	}

	[Fact]
	public void OverflowingDocumentUsesRightmostColumn() {
		using Fixture fixture = new( "one\ntwo\nthree\nfour", new WidgetEditorOptions { ShowVerticalScrollBar = true } );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 3, 5 ) );

		fixture.Host.Render();

		Assert.Equal( "▲", fixture.Host.Screen.StandardWindow.GetCell( 0, 4 ).Content );
		Assert.Equal( "┃", fixture.Host.Screen.StandardWindow.GetCell( 1, 4 ).Content );
		Assert.Equal( "▼", fixture.Host.Screen.StandardWindow.GetCell( 2, 4 ).Content );
		Assert.Equal( "o", fixture.Host.Screen.StandardWindow.GetCell( 0, 0 ).Content );
	}

	[Fact]
	public void FullWidthFitProbeDoesNotOscillateWhenReservedColumnWouldWrap() {
		using Fixture fixture = new( "abcdefgh", new WidgetEditorOptions { ShowVerticalScrollBar = true, WrapMode = CursesTextWrapMode.TextElement } );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 2, 4 ) );

		fixture.Host.Render(); fixture.Host.Render();

		Assert.Equal( "d", fixture.Host.Screen.StandardWindow.GetCell( 0, 3 ).Content );
		Assert.Equal( "h", fixture.Host.Screen.StandardWindow.GetCell( 1, 3 ).Content );
	}

	[Fact]
	public void EditAndResizeHideAndRestoreScrollbar() {
		using Fixture fixture = new( "one", new WidgetEditorOptions { ShowVerticalScrollBar = true } );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 2, 6 ) ); fixture.Host.Render();
		Assert.NotEqual( "▲", fixture.Host.Screen.StandardWindow.GetCell( 0, 5 ).Content );
		long end = fixture.Document.Length;
		Assert.Equal( WidgetTextEditStatus.Applied, fixture.Document.Apply( [ new WidgetTextEdit( end, 0, "\ntwo\nthree" ) ], fixture.Document.Revision,
			new WidgetTextEditContext( fixture.View.Id, 0, 0, end + 10, end + 10, WidgetTextEditKind.Programmatic, TimeSpan.Zero ) ).Status );

		fixture.Host.Render(); Assert.Equal( "▲", fixture.Host.Screen.StandardWindow.GetCell( 0, 5 ).Content );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 3, 6 ) ); fixture.Host.Render();

		Assert.NotEqual( "▲", fixture.Host.Screen.StandardWindow.GetCell( 0, 5 ).Content );
	}

	[Fact]
	public void ScrollbarComposesWithLeftLineNumberGutter() {
		using Fixture fixture = new( string.Join( '\n', Enumerable.Range( 1, 10 ).Select( index => $"line {index}" ) ),
			new WidgetEditorOptions { ShowLineNumbers = true, ShowVerticalScrollBar = true } );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 3, 8 ) );

		fixture.Host.Render();

		Assert.Equal( "1", fixture.Host.Screen.StandardWindow.GetCell( 0, 1 ).Content );
		Assert.Equal( "l", fixture.Host.Screen.StandardWindow.GetCell( 0, 3 ).Content );
		Assert.Equal( "▲", fixture.Host.Screen.StandardWindow.GetCell( 0, 7 ).Content );
	}

	[Fact]
	public void TwoRowEditorShowsOnlyArrowButtons() {
		using Fixture fixture = new( "one\ntwo\nthree", new WidgetEditorOptions { ShowVerticalScrollBar = true } );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 2, 5 ) );

		fixture.Host.Render();

		Assert.Equal( "▲", fixture.Host.Screen.StandardWindow.GetCell( 0, 4 ).Content );
		Assert.Equal( "▼", fixture.Host.Screen.StandardWindow.GetCell( 1, 4 ).Content );
	}

	[Fact]
	public void OneRowEditorSuppressesScrollbar() {
		using Fixture fixture = new( "abcde\nsecond", new WidgetEditorOptions { ShowVerticalScrollBar = true } );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 1, 5 ) );

		fixture.Host.Render();

		Assert.Equal( "e", fixture.Host.Screen.StandardWindow.GetCell( 0, 4 ).Content );
	}

	[Fact]
	public void HorizontalOptionDefaultsFalse() {
		Assert.False( new WidgetEditorOptions().ShowHorizontalScrollBar );
	}

	[Fact]
	public void HorizontalBarAutoHidesAndUsesApprovedGlyphs() {
		using Fixture fitting = new( "abcd", new WidgetEditorOptions { ShowHorizontalScrollBar = true } );
		fitting.Editor.Arrange( new CursesRectangle( 0, 0, 3, 10 ) ); fitting.Host.Render();
		Assert.NotEqual( "◄", fitting.Host.Screen.StandardWindow.GetCell( 2, 0 ).Content );

		using Fixture overflowing = new( "abcdefghijklmnop", new WidgetEditorOptions { ShowHorizontalScrollBar = true } );
		overflowing.Editor.Arrange( new CursesRectangle( 0, 0, 3, 10 ) ); overflowing.Host.Render();
		Assert.Equal( "◄", overflowing.Host.Screen.StandardWindow.GetCell( 2, 0 ).Content );
		Assert.Equal( "━", overflowing.Host.Screen.StandardWindow.GetCell( 2, 1 ).Content );
		Assert.Equal( "─", overflowing.Host.Screen.StandardWindow.GetCell( 2, 8 ).Content );
		Assert.Equal( "►", overflowing.Host.Screen.StandardWindow.GetCell( 2, 9 ).Content );
	}

	[Fact]
	public void WrappingSuppressesHorizontalBarAndNormalizesOffset() {
		using Fixture fixture = new( "abcdefghijklmnop", new WidgetEditorOptions {
			ShowHorizontalScrollBar = true, WrapMode = CursesTextWrapMode.TextElement
		} );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 3, 5 ) ); fixture.View.SetScroll( 3, 0 );

		fixture.Host.Render();

		Assert.Equal( 0, fixture.View.HorizontalOffset );
		Assert.DoesNotContain( new[] { "◄", "►", "─", "━" }, glyph => glyph == fixture.Host.Screen.StandardWindow.GetCell( 2, 0 ).Content );
	}

	[Fact]
	public void BothBarsReserveOneCornerCell() {
		using Fixture fixture = new( "abcdefghijk\nsecond\nthird\nfourth", new WidgetEditorOptions {
			ShowVerticalScrollBar = true, ShowHorizontalScrollBar = true
		} );
		fixture.Editor.Arrange( new CursesRectangle( 0, 0, 3, 8 ) ); fixture.Host.Render();

		Assert.Equal( "▲", fixture.Host.Screen.StandardWindow.GetCell( 0, 7 ).Content );
		Assert.Equal( "▼", fixture.Host.Screen.StandardWindow.GetCell( 1, 7 ).Content );
		Assert.Equal( "◄", fixture.Host.Screen.StandardWindow.GetCell( 2, 0 ).Content );
		Assert.Equal( "►", fixture.Host.Screen.StandardWindow.GetCell( 2, 6 ).Content );
		Assert.True( fixture.Host.Screen.StandardWindow.GetCell( 2, 7 ).IsBlank );
	}

	[Fact]
	public void OneBarCanCauseOverflowOnTheOtherAxisWithoutOscillation() {
		WidgetEditorOptions options = new() { ShowVerticalScrollBar = true, ShowHorizontalScrollBar = true };
		using Fixture verticalFirst = new( "12345\n12345\n12345\n12345", options );
		verticalFirst.Editor.Arrange( new CursesRectangle( 0, 0, 3, 5 ) ); verticalFirst.Host.Render(); verticalFirst.Host.Render();
		Assert.Equal( "▲", verticalFirst.Host.Screen.StandardWindow.GetCell( 0, 4 ).Content );
		Assert.Equal( "◄", verticalFirst.Host.Screen.StandardWindow.GetCell( 2, 0 ).Content );

		using Fixture horizontalFirst = new( "123456\n12345\n12345", options );
		horizontalFirst.Editor.Arrange( new CursesRectangle( 0, 0, 3, 5 ) ); horizontalFirst.Host.Render(); horizontalFirst.Host.Render();
		Assert.Equal( "▲", horizontalFirst.Host.Screen.StandardWindow.GetCell( 0, 4 ).Content );
		Assert.Equal( "◄", horizontalFirst.Host.Screen.StandardWindow.GetCell( 2, 0 ).Content );
	}

	[Fact]
	public void NewLinePolicyCanChangeForFutureEnterWithoutRewritingExistingText() {
		using Fixture fixture = new( "one\ntwo", new WidgetEditorOptions { NewLine = "\n" } );
		fixture.View.SetSelection( fixture.Document.Length, fixture.Document.Length );
		fixture.Editor.NewLine = "\r\n";
		fixture.Send( WidgetInput.Key( WidgetKey.Enter ) );
		Assert.Equal( "one\ntwo\r\n", fixture.Text );
		Assert.Throws<ArgumentException>( () => fixture.Editor.NewLine = "invalid" );
		Assert.Equal( "\r\n", fixture.Editor.NewLine );
		fixture.Host.ExecuteEditCommand( WidgetEditCommand.Undo, fixture.NextTime );
		Assert.Equal( "one\ntwo", fixture.Text );
	}

	[Fact]
	public void IndentationPolicyCanChangeWithoutLosingUndoHistory() {
		using Fixture fixture = new( "one", new WidgetEditorOptions { Indentation = "    " } );
		fixture.View.SetSelection( 0, 0 );
		fixture.Editor.Indentation = "  ";
		fixture.Send( WidgetInput.Key( WidgetKey.Tab ) );
		Assert.Equal( "  one", fixture.Text );
		Assert.Throws<ArgumentException>( () => fixture.Editor.Indentation = "x" );
		Assert.Equal( "  ", fixture.Editor.Indentation );
		fixture.Host.ExecuteEditCommand( WidgetEditCommand.Undo, fixture.NextTime );
		Assert.Equal( "one", fixture.Text );
	}

	private sealed class Fixture : IDisposable {
		private int tick;
		internal Fixture( string text, WidgetEditorOptions? options = null ) {
			this.Document = new WidgetTextDocument( [ text.AsMemory() ] ); this.View = new WidgetTextViewState( this.Document );
			this.Editor = new WidgetTextEditor( "editor", this.View, options );
			this.Host = new WidgetHost( new CursesScreen( 20, 3 ), WidgetTheme.Default ); this.Host.Attach( this.Editor );
			this.Editor.Arrange( new CursesRectangle( 0, 0, 3, 20 ) ); Assert.True( this.Host.Focus( this.Editor ) );
		}
		internal WidgetTextDocument Document { get; }
		internal WidgetTextViewState View { get; }
		internal WidgetTextEditor Editor { get; }
		internal WidgetHost Host { get; }
		internal TimeSpan NextTime => TimeSpan.FromMilliseconds( this.tick++ );
		internal string Text => this.Document.Read( 0, checked( (int)this.Document.Length ) );
		internal WidgetDispatchResult Send( WidgetInput input ) => this.Host.Process( input, this.NextTime );
		public void Dispose() { this.Host.Dispose(); this.View.Dispose(); }
	}
}
