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

/// <summary>Discovers a no-wrap document's display-column extent without unbounded work in one update.</summary>
internal sealed class WidgetHorizontalExtentCache {
	private readonly WidgetEditorOptions options;
	private Guid documentId;
	private long revision = -1;
	private int tabWidth;
	private CursesTextWrapMode wrapMode;
	private ICursesTextWidthProvider? widthProvider;
	private long nextLine;
	private long lineSourceOffset;
	private long lineColumn;
	private long maximumColumn;
	private bool exact;

	internal WidgetHorizontalExtentCache( WidgetEditorOptions options ) {
		this.options = options ?? throw new ArgumentNullException( nameof( options ) ); this.options.Validate();
	}

	internal void ObserveVisible( WidgetTextViewState view, ICursesTextWidthProvider provider, int viewportColumns,
		long horizontalOffset, long visibleRight, bool hasOverflow ) {
		this.EnsureKey( view, provider ); if ( CursesTextWrapMode.NoWrap != view.WrapMode ) return;
		this.maximumColumn = Math.Max( this.maximumColumn, Math.Max( 0, visibleRight ) );
		if ( hasOverflow ) this.maximumColumn = Math.Max( this.maximumColumn,
			SaturatingAdd( horizontalOffset, Math.Max( 0, viewportColumns ) + 1L ) );
	}

	/// <summary>Advances bounded discovery and returns the number of UTF-16 source units examined.</summary>
	internal int Advance( WidgetTextViewState view, ICursesTextWidthProvider provider ) {
		this.EnsureKey( view, provider ); if ( this.exact || CursesTextWrapMode.NoWrap != view.WrapMode ) return 0;
		WidgetTextDocument document = view.Document; int budget = this.options.MaximumProjectionSourceUnits; int unitsRead = 0; int completedLines = 0;
		while ( this.nextLine < document.LineCount && 0 < budget && completedLines < this.options.MaximumWorkPerUpdate ) {
			WidgetTextLine line = document.GetLine( this.nextLine ); long remaining = line.ContentLength - this.lineSourceOffset;
			if ( 0 == remaining ) { this.CompleteLine(); completedLines++; continue; }
			int maximumUnitWidth = Math.Max( 2, view.TabWidth );
			int layoutCapacity = Math.Max( 1, ( WidgetEditorOptions.MaximumSupportedProjectionSourceUnits - view.TabWidth ) / maximumUnitWidth );
			int requested = checked( (int)Math.Min( remaining, Math.Min( budget, layoutCapacity ) ) );
			string source = document.Read( line.Start + this.lineSourceOffset, requested );
			int consumed = source.Length;
			if ( this.lineSourceOffset + source.Length < line.ContentLength &&
				!document.IsTextElementBoundary( line.Start + this.lineSourceOffset + source.Length ) ) {
				int[] starts = StringInfo.ParseCombiningCharacters( source );
				if ( starts.Length <= 1 ) source = "�";
				else { consumed = starts[ ^1 ]; source = source[ ..consumed ]; }
			}
			if ( 0 == source.Length ) break;
			this.lineColumn = SaturatingAdd( this.lineColumn, Measure( Sanitize( source ), this.lineColumn, view.TabWidth, provider ) );
			this.lineSourceOffset += consumed; unitsRead += consumed; budget -= consumed;
			if ( this.lineSourceOffset == line.ContentLength ) { this.CompleteLine(); completedLines++; }
		}
		if ( this.nextLine >= document.LineCount ) this.exact = true;
		return unitsRead;
	}

	internal WidgetScrollRange GetRange( WidgetTextViewState view, ICursesTextWidthProvider provider, int viewportColumns ) {
		this.EnsureKey( view, provider ); int viewport = Math.Max( 0, viewportColumns );
		if ( CursesTextWrapMode.NoWrap != view.WrapMode ) return new WidgetScrollRange( viewport, viewport, 0, true );
		long extent = Math.Max( viewport, this.maximumColumn );
		if ( !this.exact ) extent = Math.Max( extent, SaturatingAdd( view.HorizontalOffset, viewport ) );
		long maximum = Math.Max( 0, extent - viewport ); long offset = Math.Min( view.HorizontalOffset, maximum );
		return new WidgetScrollRange( extent, viewport, offset, this.exact );
	}

	private void EnsureKey( WidgetTextViewState view, ICursesTextWidthProvider provider ) {
		ArgumentNullException.ThrowIfNull( view ); ArgumentNullException.ThrowIfNull( provider ); WidgetTextDocument document = view.Document;
		if ( this.documentId == document.Id && this.revision == document.Revision && this.tabWidth == view.TabWidth &&
			this.wrapMode == view.WrapMode && ReferenceEquals( this.widthProvider, provider ) ) return;
		this.documentId = document.Id; this.revision = document.Revision; this.tabWidth = view.TabWidth; this.wrapMode = view.WrapMode;
		this.widthProvider = provider; this.nextLine = 0; this.lineSourceOffset = 0; this.lineColumn = 0; this.maximumColumn = 0;
		this.exact = CursesTextWrapMode.NoWrap != view.WrapMode;
	}

	private void CompleteLine() {
		this.maximumColumn = Math.Max( this.maximumColumn, this.lineColumn ); this.nextLine++;
		this.lineSourceOffset = 0; this.lineColumn = 0;
	}

	private static long Measure( string text, long baseColumn, int tabWidth, ICursesTextWidthProvider provider ) {
		int startingColumn = checked( (int)( baseColumn % tabWidth ) );
		long maximumWidth = (long)text.Length * Math.Max( 2, tabWidth ) + startingColumn;
		int columns = checked( (int)Math.Clamp( maximumWidth, 1, WidgetEditorOptions.MaximumSupportedProjectionSourceUnits ) );
		CursesTextLayout layout = CursesTextLayout.Create( text, new CursesTextLayoutOptions( columns ) {
			MaximumRows = 1, WrapMode = CursesTextWrapMode.NoWrap, Overflow = CursesTextOverflow.Clip,
			StartingColumn = startingColumn, TabInterval = tabWidth, WidthProvider = provider
		} );
		return 0 == layout.Lines.Count ? 0 : layout.Lines[ 0 ].Columns;
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
		try { return checked( left + right ); } catch ( OverflowException ) { return long.MaxValue; }
	}
}
