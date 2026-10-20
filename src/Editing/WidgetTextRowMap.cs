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

internal sealed class WidgetTextRowMap {
	private sealed class Entry {
		internal required Guid DocumentId { get; init; }
		internal required long Revision { get; init; }
		internal required long LogicalLine { get; init; }
		internal required int Columns { get; init; }
		internal required int TabWidth { get; init; }
		internal required CursesTextWrapMode WrapMode { get; init; }
		internal required ICursesTextWidthProvider WidthProvider { get; init; }
		internal required int KnownRows { get; init; }
		internal required bool IsExact { get; init; }
		internal required int SourceUnitsRead { get; init; }
		internal long LastUse { get; set; }
	}

	private readonly WidgetEditorOptions options;
	private readonly List<Entry> entries = [];
	private long useClock;

	internal WidgetTextRowMap( WidgetEditorOptions options ) { this.options = options ?? throw new ArgumentNullException( nameof( options ) ); this.options.Validate(); }

	internal int CachedEntryCount => this.entries.Count;
	internal int LastSourceUnitsRead { get; private set; }
	internal long MeasurementCount { get; private set; }

	internal WidgetTextViewportAnchor Move( WidgetTextViewState view, WidgetTextViewportAnchor origin, long displayedRows,
		ICursesTextWidthProvider widthProvider, int columns ) {
		ArgumentNullException.ThrowIfNull( view ); ArgumentNullException.ThrowIfNull( widthProvider );
		WidgetTextDocument document = view.Document; long line = Math.Min( origin.LogicalLineIndex, Math.Max( 0, document.LineCount - 1 ) );
		if ( CursesTextWrapMode.NoWrap == view.WrapMode ) {
			long target = SaturatingAdd( line, displayedRows ); return new WidgetTextViewportAnchor( Math.Clamp( target, 0, document.LineCount - 1 ), 0 );
		}
		int visual = origin.LogicalLineIndex == line ? origin.VisualRowIndex : 0; long remaining = displayedRows;
		int work = 0;
		while ( 0 != remaining && work < this.options.MaximumWorkPerUpdate ) {
			if ( 0 < remaining ) {
				Entry current = this.Get( view, line, widthProvider, columns );
				if ( visual + 1 < current.KnownRows ) { visual++; remaining--; work++; continue; }
				if ( !current.IsExact || line + 1 >= document.LineCount ) break;
				line++; visual = 0; remaining--; work++; continue;
			}
			if ( 0 < visual ) { visual--; remaining++; work++; continue; }
			if ( 0 == line ) break;
			Entry previous = this.Get( view, line - 1, widthProvider, columns );
			line--; visual = Math.Max( 0, previous.KnownRows - 1 ); remaining++; work++;
		}
		return new WidgetTextViewportAnchor( line, visual );
	}

	internal WidgetScrollRange GetRange( WidgetTextViewState view, WidgetTextViewportAnchor origin, ICursesTextWidthProvider widthProvider,
		int columns, int viewportRows ) {
		ArgumentNullException.ThrowIfNull( view ); ArgumentNullException.ThrowIfNull( widthProvider );
		if ( 0 > viewportRows ) throw new ArgumentOutOfRangeException( nameof( viewportRows ) );
		WidgetTextDocument document = view.Document; columns = Math.Max( 1, columns );
		if ( CursesTextWrapMode.NoWrap == view.WrapMode ) {
			long offset = Math.Min( origin.LogicalLineIndex, Math.Max( 0, document.LineCount - 1 ) );
			return new WidgetScrollRange( document.LineCount, viewportRows, offset, true );
		}
		long first = Math.Min( origin.LogicalLineIndex, Math.Max( 0, document.LineCount - 1 ) );
		long measurementEnd = Math.Min( document.LineCount, SaturatingAdd( first, this.options.MaximumWorkPerUpdate ) );
		if ( document.LineCount <= this.options.MaximumWorkPerUpdate ) { first = 0; measurementEnd = document.LineCount; }
		for ( long line = first; line < measurementEnd; line++ ) _ = this.Get( view, line, widthProvider, columns );

		Entry[] matching = this.entries.Where( entry => entry.DocumentId == document.Id && entry.Revision == document.Revision &&
			entry.Columns == columns && entry.TabWidth == view.TabWidth && entry.WrapMode == view.WrapMode && ReferenceEquals( entry.WidthProvider, widthProvider ) ).ToArray();
		long extent = document.LineCount; long offsetValue = Math.Min( origin.LogicalLineIndex, Math.Max( 0, document.LineCount - 1 ) );
		foreach ( Entry entry in matching ) {
			long extra = Math.Max( 0, entry.KnownRows - 1 ); extent = SaturatingAdd( extent, extra );
			if ( entry.LogicalLine < origin.LogicalLineIndex ) offsetValue = SaturatingAdd( offsetValue, extra );
		}
		offsetValue = SaturatingAdd( offsetValue, origin.VisualRowIndex );
		bool exact = matching.LongCount( static entry => entry.IsExact ) == document.LineCount;
		return new WidgetScrollRange( extent, viewportRows, Math.Min( offsetValue, Math.Max( 0, extent - 1 ) ), exact );
	}

	private Entry Get( WidgetTextViewState view, long lineIndex, ICursesTextWidthProvider widthProvider, int columns ) {
		WidgetTextDocument document = view.Document; columns = Math.Max( 1, columns );
		Entry? cached = this.entries.FirstOrDefault( entry => entry.DocumentId == document.Id && entry.Revision == document.Revision &&
			entry.LogicalLine == lineIndex && entry.Columns == columns && entry.TabWidth == view.TabWidth && entry.WrapMode == view.WrapMode &&
			ReferenceEquals( entry.WidthProvider, widthProvider ) );
		if ( cached is not null ) { cached.LastUse = ++this.useClock; this.LastSourceUnitsRead = 0; return cached; }
		WidgetTextLine line = document.GetLine( lineIndex ); int count = checked( (int)Math.Min( line.ContentLength, this.options.MaximumProjectionSourceUnits ) );
		string source = document.Read( line.Start, count );
		if ( count < line.ContentLength && !document.IsTextElementBoundary( line.Start + count ) ) {
			int[] starts = StringInfo.ParseCombiningCharacters( source ); source = starts.Length <= 1 ? "�" : source[ ..starts[ ^1 ] ];
		}
		string display = Sanitize( source ); int maximumRows = Math.Min( int.MaxValue, this.options.MaximumWorkPerUpdate + 1 );
		CursesTextLayout layout = CursesTextLayout.Create( display, new CursesTextLayoutOptions( columns ) {
			MaximumRows = Math.Max( 1, maximumRows ), WrapMode = view.WrapMode, Overflow = CursesTextOverflow.Clip,
			TabInterval = view.TabWidth, WidthProvider = widthProvider
		} );
		bool exact = count == line.ContentLength && !layout.IsTruncated;
		int rows = Math.Max( 1, layout.Lines.Count ); if ( layout.IsTruncated ) rows = checked( rows + 1 );
		Entry result = new() { DocumentId = document.Id, Revision = document.Revision, LogicalLine = lineIndex, Columns = columns,
			TabWidth = view.TabWidth, WrapMode = view.WrapMode, WidthProvider = widthProvider, KnownRows = rows, IsExact = exact,
			SourceUnitsRead = count, LastUse = ++this.useClock };
		this.MeasurementCount++; this.LastSourceUnitsRead = count; this.entries.Add( result );
		while ( this.entries.Count > this.options.MaximumCachedRows ) this.entries.Remove( this.entries.MinBy( static entry => entry.LastUse )! );
		return result;
	}

	private static string Sanitize( string value ) {
		char[]? replacement = null;
		for ( int index = 0; index < value.Length; index++ ) {
			char current = value[ index ]; if ( '\t' == current || current > 0x1F && ( current < 0x7F || current > 0x9F ) ) continue;
			replacement ??= value.ToCharArray(); replacement[ index ] = '�';
		}
		return replacement is null ? value : new string( replacement );
	}

	private static long SaturatingAdd( long left, long right ) {
		if ( 0 < right && left > long.MaxValue - right ) return long.MaxValue;
		if ( long.MinValue == right || 0 > right && left < -right ) return 0;
		return left + right;
	}
}
