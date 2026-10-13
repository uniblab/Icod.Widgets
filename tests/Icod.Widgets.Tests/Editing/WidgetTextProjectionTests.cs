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

public sealed class WidgetTextProjectionTests {
	[Fact]
	public void TwoViewsKeepIndependentState() {
		WidgetTextDocument document = Create( "alpha\nbeta" );
		using WidgetTextViewState first = new( document ); using WidgetTextViewState second = new( document );

		first.SetSelection( 1, 3 ); first.SetScroll( 4, 1 );
		second.SetSelection( 7, 7 );

		Assert.Equal( ( 1L, 3L, 4L, 1L ), ( first.Anchor, first.Caret, first.HorizontalOffset, first.VerticalOffset ) );
		Assert.Equal( ( 7L, 7L, 0L, 0L ), ( second.Anchor, second.Caret, second.HorizontalOffset, second.VerticalOffset ) );
		Assert.NotEqual( first.Id, second.Id );
	}

	[Fact]
	public void ViewChangeDoesNotDirtyDocument() {
		WidgetTextDocument document = Create( "text" ); using WidgetTextViewState view = new( document );

		view.SetSelection( 0, 4 ); view.SetScroll( 20, 30 ); view.WrapMode = CursesTextWrapMode.Word; view.TabWidth = 8;

		Assert.False( document.IsModified );
		Assert.Equal( 0, document.Revision );
	}

	[Fact]
	public void DocumentEditReconcilesAnchors() {
		WidgetTextDocument document = Create( "abcdef" ); using WidgetTextViewState shifted = new( document ); using WidgetTextViewState atInsertion = new( document );
		shifted.SetSelection( 4, 6 ); atInsertion.SetSelection( 1, 1 );

		document.Apply( [ new WidgetTextEdit( 1, 0, "XY" ) ], 0,
			new WidgetTextEditContext( Guid.NewGuid(), 1, 1, 3, 3, WidgetTextEditKind.Programmatic, TimeSpan.Zero ) );

		Assert.Equal( ( 6L, 8L ), ( shifted.Anchor, shifted.Caret ) );
		Assert.Equal( ( 1L, 1L ), ( atInsertion.Anchor, atInsertion.Caret ) );
		Assert.Equal( 2, shifted.SelectionEpoch );
	}

	[Fact]
	public void PreferredColumnSurvivesShortLine() {
		WidgetTextDocument document = Create( "abcdef\nx\nabcdef" ); using WidgetTextViewState view = new( document );
		view.SetSelection( 5, 5 ); WidgetTextProjection projection = new( new WidgetEditorOptions() );

		Assert.True( projection.MoveVertically( view, 1, UnicodeCursesTextWidthProvider.Instance, new CursesRectangle( 0, 0, 3, 10 ) ) );
		Assert.Equal( 8, view.Caret );
		Assert.Equal( 5, view.PreferredColumn );
		Assert.True( projection.MoveVertically( view, 1, UnicodeCursesTextWidthProvider.Instance, new CursesRectangle( 0, 0, 3, 10 ) ) );
		Assert.Equal( 14, view.Caret );
		Assert.Equal( 5, view.PreferredColumn );
	}

	[Fact]
	public void CacheInvalidatesOnlyAffectedRevisionAndGeometry() {
		WidgetTextDocument document = Create( "first\nsecond\nthird" ); using WidgetTextViewState view = new( document );
		WidgetTextProjection projection = new( new WidgetEditorOptions() ); CursesRectangle viewport = new( 0, 0, 1, 10 );
		WidgetTextProjectionResult cold = projection.Project( view, UnicodeCursesTextWidthProvider.Instance, viewport );
		WidgetTextProjectionResult warm = projection.Project( view, UnicodeCursesTextWidthProvider.Instance, viewport );
		document.Apply( [ new WidgetTextEdit( 13, 0, "X" ) ], 0,
			new WidgetTextEditContext( Guid.NewGuid(), 13, 13, 14, 14, WidgetTextEditKind.Programmatic, TimeSpan.Zero ) );
		WidgetTextProjectionResult unaffected = projection.Project( view, UnicodeCursesTextWidthProvider.Instance, viewport );
		WidgetTextProjectionResult geometryChanged = projection.Project( view, UnicodeCursesTextWidthProvider.Instance, new CursesRectangle( 0, 0, 1, 11 ) );

		Assert.Equal( 1, cold.CacheMisses );
		Assert.Equal( 1, warm.CacheHits );
		Assert.Equal( 1, unaffected.CacheHits );
		Assert.Equal( 1, geometryChanged.CacheMisses );
	}

	[Theory]
	[InlineData( "", 0, "x" )]
	[InlineData( "x", 1, "xy" )]
	public void InsertionAtCachedLineEndReprojectsVisibleText( string initial, long offset, string expected ) {
		WidgetTextDocument document = Create( initial ); using WidgetTextViewState view = new( document );
		WidgetTextProjection projection = new( new WidgetEditorOptions() ); CursesRectangle viewport = new( 0, 0, 1, 20 );
		Assert.Equal( initial, Assert.Single( projection.Project( view, UnicodeCursesTextWidthProvider.Instance, viewport ).Rows ).Layout.Text );

		document.Apply( [ new WidgetTextEdit( offset, 0, initial.Length == 0 ? "x" : "y" ) ], 0,
			new WidgetTextEditContext( Guid.NewGuid(), offset, offset, offset + 1, offset + 1, WidgetTextEditKind.Programmatic, TimeSpan.Zero ) );
		WidgetTextProjectionResult refreshed = projection.Project( view, UnicodeCursesTextWidthProvider.Instance, viewport );

		Assert.Equal( expected, Assert.Single( refreshed.Rows ).Layout.Text );
		Assert.Equal( 1, refreshed.CacheMisses );
	}

	[Fact]
	public void LongLineHasBoundedSlices() {
		WidgetTextDocument document = Create( new string( 'x', 1_000_000 ) ); using WidgetTextViewState view = new( document );
		WidgetTextProjection projection = new( new WidgetEditorOptions { MaximumProjectionSourceUnits = 1_024 } );

		WidgetTextProjectionResult result = projection.Project( view, UnicodeCursesTextWidthProvider.Instance, new CursesRectangle( 0, 0, 1, 80 ) );

		Assert.InRange( result.SourceUnitsRead, 1, 1_024 );
		Assert.True( Assert.Single( result.Rows ).HasContinuation );
		Assert.False( result.IsComplete );
	}

	[Fact]
	public void HugeClusterHasSafeBoundedFallback() {
		WidgetTextDocument document = Create( "a" + new string( '\u0301', 10_000 ) + "b" ); using WidgetTextViewState view = new( document );
		WidgetTextProjection projection = new( new WidgetEditorOptions { MaximumProjectionSourceUnits = 1_024, MaximumClusterScan = 256 } );

		WidgetTextProjectionResult result = projection.Project( view, UnicodeCursesTextWidthProvider.Instance, new CursesRectangle( 0, 0, 1, 20 ) );

		WidgetProjectedRow row = Assert.Single( result.Rows );
		Assert.True( row.IsPlaceholder );
		Assert.True( row.HasContinuation );
		Assert.InRange( result.SourceUnitsRead, 1, 1_024 );
		Assert.Equal( "�", row.Layout.Text );
	}

	[Fact]
	public void TabsUseTrueSourceColumn() {
		WidgetTextDocument document = Create( "\tX" ); using WidgetTextViewState view = new( document ); view.TabWidth = 4;
		WidgetTextProjection projection = new( new WidgetEditorOptions() );

		WidgetProjectedRow row = Assert.Single( projection.Project( view, UnicodeCursesTextWidthProvider.Instance,
			new CursesRectangle( 0, 0, 1, 10 ) ).Rows );

		Assert.Equal( 4, row.GetColumn( 1 ) );
		Assert.Equal( 5, row.GetColumn( 2 ) );
		Assert.Equal( 1, row.HitTest( 4 ) );
	}

	[Fact]
	public void TinyViewportsRemainBounded() {
		WidgetTextDocument document = Create( "界e\u0301" ); using WidgetTextViewState view = new( document ); WidgetTextProjection projection = new( new WidgetEditorOptions() );

		Assert.Empty( projection.Project( view, UnicodeCursesTextWidthProvider.Instance, new CursesRectangle( 0, 0, 0, 0 ) ).Rows );
		WidgetTextProjectionResult one = projection.Project( view, UnicodeCursesTextWidthProvider.Instance, new CursesRectangle( 0, 0, 1, 1 ) );

		Assert.Single( one.Rows );
		Assert.InRange( one.SourceUnitsRead, 0, 65_536 );
	}

	[Fact]
	public void RetainedGeometryRoundTripsUnicodeTabsAndControls() {
		WidgetTextDocument document = Create( "e\u0301界👩‍💻\u0001\tZ" ); using WidgetTextViewState view = new( document ); view.TabWidth = 4;
		WidgetTextProjection projection = new( new WidgetEditorOptions() ); WidgetProjectedRow row = Assert.Single(
			projection.Project( view, UnicodeCursesTextWidthProvider.Instance, new CursesRectangle( 0, 0, 1, 40 ) ).Rows );

		foreach ( long offset in new long[] { 0, 2, 3, 8, 9, 10, 11 } )
			Assert.Equal( offset, row.HitTest( row.GetColumn( offset ) ) );
	}

	[Fact]
	public void WorkBudgetDefersRemainingRows() {
		WidgetTextDocument document = Create( string.Join( '\n', Enumerable.Range( 0, 20 ) ) ); using WidgetTextViewState view = new( document );
		WidgetTextProjection projection = new( new WidgetEditorOptions { MaximumWorkPerUpdate = 2 } );

		WidgetTextProjectionResult result = projection.Project( view, UnicodeCursesTextWidthProvider.Instance, new CursesRectangle( 0, 0, 10, 10 ) );

		Assert.Equal( 2, result.Rows.Count );
		Assert.False( result.IsComplete );
	}

	[Fact]
	public void FarHorizontalOffsetDoesNotOverflowOrReadWholeLine() {
		WidgetTextDocument document = Create( new string( 'x', 1_000_000 ) ); using WidgetTextViewState view = new( document ); view.SetScroll( long.MaxValue, 0 );
		WidgetTextProjection projection = new( new WidgetEditorOptions { MaximumProjectionSourceUnits = 512 } );

		WidgetTextProjectionResult result = projection.Project( view, UnicodeCursesTextWidthProvider.Instance, new CursesRectangle( 0, 0, 1, int.MaxValue ) );

		Assert.InRange( result.SourceUnitsRead, 1, 512 );
		Assert.Null( result.Caret );
	}

	[Fact]
	public void FarHorizontalSeekMakesBoundedForwardProgress() {
		WidgetTextDocument document = Create( new string( 'x', 5_000 ) ); using WidgetTextViewState view = new( document );
		view.SetSelection( 1_000, 1_000 ); view.SetScroll( 1_000, 0 );
		WidgetTextProjection projection = new( new WidgetEditorOptions { MaximumProjectionSourceUnits = 256, MaximumClusterScan = 64 } );
		WidgetTextProjectionResult? result = null;

		for ( int attempt = 0; attempt < 8 && result?.Caret is null; attempt++ ) {
			result = projection.Project( view, UnicodeCursesTextWidthProvider.Instance, new CursesRectangle( 0, 0, 1, 20 ) );
			Assert.InRange( result.SourceUnitsRead, 0, 256 );
		}

		Assert.NotNull( result?.Caret );
		Assert.Equal( 0, result!.Caret!.Value.Column );
	}

	[Fact]
	public void FarHorizontalSelectionUsesDocumentColumns() {
		WidgetTextDocument document = Create( new string( 'x', 5_000 ) ); using WidgetTextViewState view = new( document );
		view.SetSelection( 1_000, 1_003 ); view.SetScroll( 1_000, 0 );
		WidgetTextProjection projection = new( new WidgetEditorOptions { MaximumProjectionSourceUnits = 256, MaximumClusterScan = 64 } );
		WidgetTextProjectionResult? result = null;

		for ( int attempt = 0; attempt < 8 && ( result is null || 0 == result.SelectionSpans.Count ); attempt++ )
			result = projection.Project( view, UnicodeCursesTextWidthProvider.Instance, new CursesRectangle( 0, 0, 1, 20 ) );

		WidgetProjectedSelectionSpan selection = Assert.Single( result!.SelectionSpans );
		Assert.Equal( ( 0, 3 ), ( selection.Column, selection.Columns ) );
		Assert.Equal( ( 1_000L, 1_003L ), ( selection.SourceStart, selection.SourceEnd ) );
	}

	[Fact]
	public void WrappedExtentIsExplicitlyEstimated() {
		WidgetTextDocument document = Create( "one two three four" ); using WidgetTextViewState view = new( document ); view.WrapMode = CursesTextWrapMode.Word;
		WidgetTextProjection projection = new( new WidgetEditorOptions() );

		WidgetTextProjectionResult result = projection.Project( view, UnicodeCursesTextWidthProvider.Instance, new CursesRectangle( 0, 0, 2, 4 ) );

		Assert.False( result.IsVerticalExtentExact );
		Assert.True( result.VerticalExtent >= document.LineCount );
	}

	private static WidgetTextDocument Create( string text ) => new( [ text.AsMemory() ] );
}
