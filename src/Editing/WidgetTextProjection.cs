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

using System.Globalization;
using Icod.DCurses;

internal readonly record struct WidgetProjectedCaret( int Row, int Column, long Offset );
internal readonly record struct WidgetProjectedSelectionSpan( int Row, int Column, int Columns, long SourceStart, long SourceEnd );

internal sealed class WidgetProjectedRow {
	private readonly long origin;
	private readonly long baseColumn;
	private readonly int layoutStartingColumn;
	internal WidgetProjectedRow( long logicalLineIndex, long origin, long baseColumn, int layoutStartingColumn, CursesTextLayout layout, int visualLineIndex, bool placeholder, bool continuation ) {
		this.LogicalLineIndex = logicalLineIndex; this.origin = origin; this.Layout = layout; this.VisualLineIndex = visualLineIndex;
		this.baseColumn = baseColumn; this.layoutStartingColumn = layoutStartingColumn; this.IsPlaceholder = placeholder; this.HasContinuation = continuation;
	}
	internal long LogicalLineIndex { get; }
	internal CursesTextLayout Layout { get; }
	internal int VisualLineIndex { get; }
	internal bool IsPlaceholder { get; }
	internal bool HasContinuation { get; }
	internal long BaseColumn => this.baseColumn;
	internal long SourceStart => this.IsPlaceholder ? this.origin : this.origin + this.Layout.Lines[ this.VisualLineIndex ].SourceStart.Offset;
	internal long SourceEnd => this.IsPlaceholder ? this.origin : this.origin + this.Layout.Lines[ this.VisualLineIndex ].SourceEnd.Offset;

	internal long GetColumn( long sourceOffset ) {
		if ( this.IsPlaceholder ) { if ( sourceOffset != this.origin ) throw new ArgumentOutOfRangeException( nameof( sourceOffset ) ); return 0; }
		long local = sourceOffset - this.origin; if ( local < 0 || local > this.Layout.Text.Length ) throw new ArgumentOutOfRangeException( nameof( sourceOffset ) );
		return this.baseColumn + this.Layout.GetVisualPosition( new CursesTextPosition( checked( (int)local ) ) ).Column - this.layoutStartingColumn;
	}

	internal long HitTest( long column ) {
		if ( this.IsPlaceholder ) return this.origin;
		long localColumn = this.layoutStartingColumn + column - this.baseColumn;
		return this.origin + this.Layout.HitTest( this.VisualLineIndex, (int)Math.Clamp( localColumn, 0, int.MaxValue ) ).Position.Offset;
	}

	internal long TranslateLayoutColumn( int column ) => this.baseColumn + column - this.layoutStartingColumn;
}

internal sealed record WidgetTextProjectionResult(
	IReadOnlyList<WidgetProjectedRow> Rows,
	WidgetProjectedCaret? Caret,
	IReadOnlyList<WidgetProjectedSelectionSpan> SelectionSpans,
	long VerticalExtent,
	bool IsVerticalExtentExact,
	bool IsComplete,
	int SourceUnitsRead,
	int CacheHits,
	int CacheMisses );

internal sealed class WidgetTextProjection {
	private sealed class CacheEntry {
		internal required Guid DocumentId { get; init; }
		internal required long Revision { get; set; }
		internal required long LogicalLine { get; init; }
		internal required long Origin { get; init; }
		internal required long BaseColumn { get; init; }
		internal required int StartingColumn { get; init; }
		internal required long AffectedEnd { get; init; }
		internal required int ViewportColumns { get; init; }
		internal required long HorizontalOffset { get; init; }
		internal required int LayoutRows { get; init; }
		internal required int TabWidth { get; init; }
		internal required CursesTextWrapMode WrapMode { get; init; }
		internal required ICursesTextWidthProvider WidthProvider { get; init; }
		internal required CursesTextLayout Layout { get; init; }
		internal required bool IsPlaceholder { get; init; }
		internal required bool HasContinuation { get; init; }
		internal long LastUse { get; set; }
		internal int RetainedUnits => this.Layout.Text.Length;
		internal int RetainedRows => this.Layout.Lines.Count;
	}

	private sealed class ScanState {
		internal required Guid DocumentId { get; init; }
		internal required long Revision { get; init; }
		internal required long LogicalLine { get; init; }
		internal required int TabWidth { get; init; }
		internal required ICursesTextWidthProvider WidthProvider { get; init; }
		internal long SourceOffset { get; set; }
		internal long Column { get; set; }
	}

	private readonly WidgetEditorOptions options;
	private readonly List<CacheEntry> cache = [];
	private readonly List<ScanState> scans = [];
	private long useClock;

	internal WidgetTextProjection( WidgetEditorOptions options ) { this.options = options ?? throw new ArgumentNullException( nameof( options ) ); this.options.Validate(); }

	internal WidgetTextProjectionResult Project( WidgetTextViewState view, ICursesTextWidthProvider widthProvider, CursesRectangle viewport ) {
		ArgumentNullException.ThrowIfNull( view ); ArgumentNullException.ThrowIfNull( widthProvider );
		WidgetTextDocument document = view.Document; this.ReconcileCache( view );
		long extent = document.LineCount; bool exactExtent = CursesTextWrapMode.NoWrap == view.WrapMode;
		if ( 0 == viewport.Rows || 0 == viewport.Columns ) return new WidgetTextProjectionResult( [], null, [], extent, exactExtent, true, 0, 0, 0 );

		long firstLine = Math.Min( view.VerticalOffset, Math.Max( 0, document.LineCount - 1 ) );
		int remainingUnits = this.options.MaximumProjectionSourceUnits; int sourceUnitsRead = 0; int hits = 0; int misses = 0; int work = 0; bool complete = true;
		List<WidgetProjectedRow> rows = [];
		for ( long lineIndex = firstLine; lineIndex < document.LineCount && rows.Count < viewport.Rows; lineIndex++ ) {
			if ( work >= this.options.MaximumWorkPerUpdate ) { complete = false; break; }
			int layoutRows = Math.Max( 1, Math.Min( viewport.Rows - rows.Count, this.options.MaximumWorkPerUpdate - work ) ); work++;
			CacheEntry? entry = this.Find( document, lineIndex, viewport.Columns, layoutRows, view, widthProvider );
			if ( entry is null ) {
				misses++; if ( 0 == remainingUnits ) { complete = false; break; }
				entry = this.BuildEntry( document, lineIndex, viewport.Columns, layoutRows, view, widthProvider, remainingUnits, out int read, out bool cacheable );
				sourceUnitsRead = checked( sourceUnitsRead + read ); remainingUnits -= read; if ( cacheable ) this.Add( entry );
			} else { hits++; entry.LastUse = ++this.useClock; }
			complete &= !entry.HasContinuation;
			for ( int visual = 0; visual < entry.Layout.Lines.Count && rows.Count < viewport.Rows; visual++ )
				rows.Add( new WidgetProjectedRow( lineIndex, entry.Origin, entry.BaseColumn, entry.StartingColumn, entry.Layout, visual, entry.IsPlaceholder, entry.HasContinuation ) );
		}

		WidgetProjectedCaret? caret = FindCaret( view, rows, viewport.Columns );
		IReadOnlyList<WidgetProjectedSelectionSpan> selection = FindSelection( view, rows, viewport.Columns );
		return new WidgetTextProjectionResult( rows.AsReadOnly(), caret, selection, extent, exactExtent, complete, sourceUnitsRead, hits, misses );
	}

	internal bool MoveVertically( WidgetTextViewState view, int lineDelta, ICursesTextWidthProvider widthProvider, CursesRectangle viewport ) {
		ArgumentNullException.ThrowIfNull( view ); ArgumentNullException.ThrowIfNull( widthProvider );
		if ( 0 == lineDelta ) return false; WidgetTextDocument document = view.Document; long currentLine = document.GetLineIndex( view.Caret );
		long targetLine = Math.Clamp( currentLine + lineDelta, 0, document.LineCount - 1 ); if ( targetLine == currentLine ) return false;
		int columns = Math.Max( 1, viewport.Columns );
		CacheEntry current = this.BuildEntry( document, currentLine, columns, 1, view, widthProvider, this.options.MaximumProjectionSourceUnits, out _, out _ );
		WidgetProjectedRow currentRow = new( currentLine, current.Origin, current.BaseColumn, current.StartingColumn, current.Layout, Math.Max( 0, current.Layout.Lines.Count - 1 ), current.IsPlaceholder, current.HasContinuation );
		int preferred = view.PreferredColumn ?? checked( (int)currentRow.GetColumn( view.Caret ) );
		CacheEntry target = this.BuildEntry( document, targetLine, columns, 1, view, widthProvider, this.options.MaximumProjectionSourceUnits, out _, out _ );
		WidgetProjectedRow targetRow = new( targetLine, target.Origin, target.BaseColumn, target.StartingColumn, target.Layout, 0, target.IsPlaceholder, target.HasContinuation );
		long offset = targetRow.HitTest( preferred ); view.PreferredColumn = preferred; view.SetSelectionFromProjection( offset, offset, true ); return true;
	}

	private CacheEntry BuildEntry( WidgetTextDocument document, long lineIndex, int viewportColumns, int layoutRows, WidgetTextViewState view,
		ICursesTextWidthProvider widthProvider, int budget, out int sourceUnitsRead, out bool cacheable ) {
		WidgetTextLine line = document.GetLine( lineIndex ); sourceUnitsRead = 0; long relativeStart = 0; long baseColumn = 0; bool reachedHorizontal = true;
		if ( CursesTextWrapMode.NoWrap == view.WrapMode && 0 < view.HorizontalOffset && 0 < line.ContentLength ) {
			ScanState scan = this.GetScan( document, lineIndex, view.TabWidth, widthProvider );
			if ( view.HorizontalOffset < scan.Column ) { scan.SourceOffset = 0; scan.Column = 0; }
			this.AdvanceScan( document, line, scan, view.HorizontalOffset, ref budget, ref sourceUnitsRead );
			relativeStart = scan.SourceOffset; baseColumn = scan.Column; reachedHorizontal = scan.Column >= view.HorizontalOffset || scan.SourceOffset >= line.ContentLength;
		}
		long available = Math.Max( 0, line.ContentLength - relativeStart ); int count = checked( (int)Math.Min( available, Math.Min( budget, this.options.MaximumProjectionSourceUnits ) ) );
		int probeCount = Math.Min( count, this.options.MaximumClusterScan ); string source = document.Read( line.Start + relativeStart, probeCount ); sourceUnitsRead += probeCount;
		bool continuation = relativeStart + count < line.ContentLength || !reachedHorizontal; bool placeholder = false;
		if ( 0 < probeCount && relativeStart + probeCount < line.ContentLength && !document.IsTextElementBoundary( line.Start + relativeStart + probeCount ) ) {
			int[] starts = StringInfo.ParseCombiningCharacters( source );
			if ( starts.Length <= 1 ) { placeholder = true; continuation = true; source = "�"; }
			else { source = source[ ..starts[ ^1 ] ]; continuation = true; }
		} else if ( probeCount < count ) {
			source += document.Read( line.Start + relativeStart + probeCount, count - probeCount ); sourceUnitsRead += count - probeCount;
			if ( relativeStart + count < line.ContentLength && !document.IsTextElementBoundary( line.Start + relativeStart + count ) ) {
				int[] starts = StringInfo.ParseCombiningCharacters( source );
				if ( starts.Length <= 1 ) { placeholder = true; source = "�"; }
				else source = source[ ..starts[ ^1 ] ];
				continuation = true;
			}
		}
		string display = placeholder ? source : Sanitize( source ); int startingColumn = checked( (int)( baseColumn % view.TabWidth ) );
		long skipped = Math.Max( 0, view.HorizontalOffset - baseColumn ); long requestedColumns = !reachedHorizontal ? 1 : CursesTextWrapMode.NoWrap == view.WrapMode
			? skipped >= 1_048_576 ? 1_048_576 : startingColumn + skipped + viewportColumns
			: viewportColumns;
		int columnExtent = checked( (int)Math.Clamp( requestedColumns, 1, 1_048_576 ) );
		CursesTextLayout layout = CursesTextLayout.Create( display, new CursesTextLayoutOptions( columnExtent ) {
			MaximumRows = Math.Max( 1, layoutRows ), WrapMode = view.WrapMode, Overflow = CursesTextOverflow.Clip,
			StartingColumn = startingColumn, TabInterval = view.TabWidth, WidthProvider = widthProvider
		} );
		cacheable = reachedHorizontal && ( 0 < display.Length || 0 == available );
		return new CacheEntry {
			DocumentId = document.Id, Revision = document.Revision, LogicalLine = lineIndex, Origin = line.Start + relativeStart,
			BaseColumn = baseColumn, StartingColumn = startingColumn, AffectedEnd = line.End, ViewportColumns = viewportColumns,
			HorizontalOffset = view.HorizontalOffset, LayoutRows = layoutRows, TabWidth = view.TabWidth, WrapMode = view.WrapMode,
			WidthProvider = widthProvider, Layout = layout, IsPlaceholder = placeholder,
			HasContinuation = continuation || CursesTextWrapMode.NoWrap != view.WrapMode && layout.IsTruncated, LastUse = ++this.useClock
		};
	}

	private CacheEntry? Find( WidgetTextDocument document, long line, int viewportColumns, int layoutRows, WidgetTextViewState view, ICursesTextWidthProvider widthProvider ) =>
		this.cache.FirstOrDefault( entry => entry.DocumentId == document.Id && entry.Revision == document.Revision && entry.LogicalLine == line &&
			entry.ViewportColumns == viewportColumns && entry.HorizontalOffset == view.HorizontalOffset && entry.LayoutRows == layoutRows &&
			entry.TabWidth == view.TabWidth && entry.WrapMode == view.WrapMode && ReferenceEquals( entry.WidthProvider, widthProvider ) );

	private void Add( CacheEntry entry ) {
		if ( entry.RetainedRows > this.options.MaximumCachedRows || entry.RetainedUnits > this.options.MaximumCachedSourceUnits ) return;
		this.cache.Add( entry );
		while ( this.cache.Sum( static item => item.RetainedRows ) > this.options.MaximumCachedRows || this.cache.Sum( static item => item.RetainedUnits ) > this.options.MaximumCachedSourceUnits )
			this.cache.Remove( this.cache.MinBy( static item => item.LastUse )! );
	}

	private void ReconcileCache( WidgetTextViewState view ) {
		WidgetTextDocument document = view.Document; WidgetTextDocumentChangedEventArgs? change = view.LastDocumentChange;
		long firstChanged = change is null || 0 == change.Changes.Count ? -1 : change.Changes.Min( static item => item.Start );
		for ( int index = this.cache.Count - 1; index >= 0; index-- ) {
			CacheEntry entry = this.cache[ index ]; if ( entry.DocumentId != document.Id || entry.Revision == document.Revision ) continue;
			if ( change is not null && change.Revision == document.Revision && entry.Revision + 1 == document.Revision && 0 <= firstChanged && entry.AffectedEnd <= firstChanged ) entry.Revision = document.Revision;
			else this.cache.RemoveAt( index );
		}
		this.scans.RemoveAll( state => state.DocumentId == document.Id && state.Revision != document.Revision );
	}

	private ScanState GetScan( WidgetTextDocument document, long line, int tabWidth, ICursesTextWidthProvider widthProvider ) {
		ScanState? result = this.scans.FirstOrDefault( state => state.DocumentId == document.Id && state.Revision == document.Revision &&
			state.LogicalLine == line && state.TabWidth == tabWidth && ReferenceEquals( state.WidthProvider, widthProvider ) );
		if ( result is not null ) return result;
		result = new ScanState { DocumentId = document.Id, Revision = document.Revision, LogicalLine = line, TabWidth = tabWidth, WidthProvider = widthProvider };
		this.scans.Add( result ); while ( this.scans.Count > this.options.MaximumCachedRows ) this.scans.RemoveAt( 0 ); return result;
	}

	private void AdvanceScan( WidgetTextDocument document, WidgetTextLine line, ScanState state, long targetColumn, ref int budget, ref int unitsRead ) {
		while ( state.Column < targetColumn && state.SourceOffset < line.ContentLength && 0 < budget ) {
			int count = checked( (int)Math.Min( line.ContentLength - state.SourceOffset, Math.Min( budget, this.options.MaximumProjectionSourceUnits ) ) );
			string source = document.Read( line.Start + state.SourceOffset, count ); unitsRead += count; budget -= count;
			if ( state.SourceOffset + count < line.ContentLength && !document.IsTextElementBoundary( line.Start + state.SourceOffset + count ) ) {
				int[] starts = StringInfo.ParseCombiningCharacters( source ); if ( starts.Length <= 1 ) return; source = source[ ..starts[ ^1 ] ];
			}
			if ( 0 == source.Length ) return;
			string display = Sanitize( source ); int startingColumn = checked( (int)( state.Column % state.TabWidth ) );
			int extent = checked( Math.Min( 1_048_576, startingColumn + display.Length * 2 + state.TabWidth ) );
			CursesTextLayout layout = CursesTextLayout.Create( display, new CursesTextLayoutOptions( Math.Max( 1, extent ) ) {
				MaximumRows = 1, WrapMode = CursesTextWrapMode.NoWrap, StartingColumn = startingColumn,
				TabInterval = state.TabWidth, WidthProvider = state.WidthProvider
			} );
			long width = layout.Lines.Count == 0 ? 0 : layout.Lines[ 0 ].Columns;
			if ( state.Column + width <= targetColumn ) { state.SourceOffset += source.Length; state.Column += width; continue; }
			long requested = startingColumn + targetColumn - state.Column; CursesTextHitTestResult hit = layout.HitTest( 0, (int)Math.Clamp( requested, 0, int.MaxValue ) );
			CursesTextVisualPosition visual = layout.GetVisualPosition( hit.Position, hit.Affinity );
			if ( state.Column + visual.Column - startingColumn > targetColumn ) { CursesTextPosition previous = layout.GetPreviousPosition( hit.Position ); visual = layout.GetVisualPosition( previous ); hit = new CursesTextHitTestResult( previous, CursesTextAffinity.Leading, false ); }
			state.SourceOffset += hit.Position.Offset; state.Column += visual.Column - startingColumn; return;
		}
	}

	private static WidgetProjectedCaret? FindCaret( WidgetTextViewState view, IReadOnlyList<WidgetProjectedRow> rows, int viewportColumns ) {
		for ( int rowIndex = 0; rowIndex < rows.Count; rowIndex++ ) {
			WidgetProjectedRow row = rows[ rowIndex ]; if ( view.Caret < row.SourceStart || view.Caret > row.SourceEnd ) continue;
			try { long shifted = row.GetColumn( view.Caret ) - view.HorizontalOffset; if ( shifted >= 0 && shifted < viewportColumns ) return new WidgetProjectedCaret( rowIndex, (int)shifted, view.Caret ); }
			catch ( ArgumentOutOfRangeException ) { }
		}
		return null;
	}

	private static IReadOnlyList<WidgetProjectedSelectionSpan> FindSelection( WidgetTextViewState view, IReadOnlyList<WidgetProjectedRow> rows, int viewportColumns ) {
		if ( !view.HasSelection ) return [];
		List<WidgetProjectedSelectionSpan> result = [];
		for ( int rowIndex = 0; rowIndex < rows.Count; rowIndex++ ) {
			WidgetProjectedRow row = rows[ rowIndex ]; if ( row.IsPlaceholder ) continue;
			long start = Math.Max( view.SelectionStart, row.SourceStart ); long end = Math.Min( view.SelectionEnd, row.SourceEnd ); if ( end <= start ) continue;
			int localStart = checked( (int)( start - row.SourceStart + row.Layout.Lines[ row.VisualLineIndex ].SourceStart.Offset ) );
			int localEnd = checked( (int)( end - row.SourceStart + row.Layout.Lines[ row.VisualLineIndex ].SourceStart.Offset ) );
			CursesRectangle[] rectangles = row.Layout.GetSelectionRectangles( new CursesTextSelection( new CursesTextPosition( localStart ), new CursesTextPosition( localEnd ) ), row.VisualLineIndex, 1 );
			foreach ( CursesRectangle rectangle in rectangles ) {
				long trueColumn = row.TranslateLayoutColumn( rectangle.Column ); long shifted = trueColumn - view.HorizontalOffset;
				int left = (int)Math.Clamp( shifted, 0, viewportColumns ); int right = (int)Math.Clamp( shifted + rectangle.Columns, 0, viewportColumns );
				if ( left < right ) result.Add( new WidgetProjectedSelectionSpan( rowIndex, left, right - left, start, end ) );
			}
		}
		return result.AsReadOnly();
	}

	private static string Sanitize( string value ) {
		char[]? replacement = null;
		for ( int index = 0; index < value.Length; index++ ) {
			char current = value[ index ]; if ( '\t' == current || current > 0x1F && ( current < 0x7F || current > 0x9F ) ) continue;
			replacement ??= value.ToCharArray(); replacement[ index ] = '�';
		}
		return replacement is null ? value : new string( replacement );
	}
}
