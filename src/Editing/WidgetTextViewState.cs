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

using Icod.DCurses;

/// <summary>Owns one independent selection, scroll position, and geometry policy for a document.</summary>
public sealed class WidgetTextViewState : IDisposable {
	private readonly WidgetTextDocument document;
	private CursesTextWrapMode wrapMode;
	private int tabWidth = 4;
	private int? preferredColumn;
	private CursesTextAffinity caretAffinity;
	private bool disposed;

	public WidgetTextViewState( WidgetTextDocument document ) {
		this.document = document ?? throw new ArgumentNullException( nameof( document ) ); this.Id = Guid.NewGuid();
		this.document.Changed += this.OnDocumentChanged;
	}

	public Guid Id { get; }
	public WidgetTextDocument Document { get { this.ThrowIfDisposed(); return this.document; } }
	public long Anchor { get; private set; }
	public long Caret { get; private set; }
	public long SelectionStart => Math.Min( this.Anchor, this.Caret );
	public long SelectionEnd => Math.Max( this.Anchor, this.Caret );
	public bool HasSelection => this.Anchor != this.Caret;
	public long SelectionEpoch { get; private set; }
	public long HorizontalOffset { get; private set; }
	public long VerticalOffset { get; private set; }
	public long ViewRevision { get; private set; }
	public int? PreferredColumn {
		get { this.ThrowIfDisposed(); return this.preferredColumn; }
		set { this.ThrowIfDisposed(); if ( value < 0 ) throw new ArgumentOutOfRangeException( nameof( value ) ); if ( this.preferredColumn == value ) return; this.preferredColumn = value; this.ViewRevision++; }
	}
	public CursesTextWrapMode WrapMode {
		get { this.ThrowIfDisposed(); return this.wrapMode; }
		set { this.ThrowIfDisposed(); if ( !Enum.IsDefined( value ) ) throw new ArgumentOutOfRangeException( nameof( value ) ); if ( this.wrapMode == value ) return; this.wrapMode = value; this.ViewRevision++; }
	}
	public int TabWidth {
		get { this.ThrowIfDisposed(); return this.tabWidth; }
		set { this.ThrowIfDisposed(); if ( value is < 1 or > 256 ) throw new ArgumentOutOfRangeException( nameof( value ) ); if ( this.tabWidth == value ) return; this.tabWidth = value; this.ViewRevision++; }
	}

	public void SetSelection( long anchor, long caret ) => this.SetSelectionCore( anchor, caret, false );

	public void SetScroll( long horizontalOffset, long verticalOffset ) {
		this.ThrowIfDisposed(); if ( 0 > horizontalOffset ) throw new ArgumentOutOfRangeException( nameof( horizontalOffset ) ); if ( 0 > verticalOffset ) throw new ArgumentOutOfRangeException( nameof( verticalOffset ) );
		if ( this.HorizontalOffset == horizontalOffset && this.VerticalOffset == verticalOffset ) return;
		this.HorizontalOffset = horizontalOffset; this.VerticalOffset = verticalOffset; this.ViewRevision++;
	}

	public void Dispose() { if ( this.disposed ) return; this.document.Changed -= this.OnDocumentChanged; this.disposed = true; }

	internal WidgetTextDocumentChangedEventArgs? LastDocumentChange { get; private set; }
	internal CursesTextAffinity CaretAffinity => this.caretAffinity;
	internal void SetSelectionFromProjection( long anchor, long caret, bool preservePreferredColumn, CursesTextAffinity? affinity = null ) =>
		this.SetSelectionCore( anchor, caret, preservePreferredColumn, affinity ?? this.caretAffinity );
	internal void InvalidatePendingOperations() { this.ThrowIfDisposed(); this.SelectionEpoch++; this.ViewRevision++; }

	private void SetSelectionCore( long anchor, long caret, bool preservePreferredColumn, CursesTextAffinity affinity = CursesTextAffinity.Leading ) {
		this.ThrowIfDisposed();
		if ( !Enum.IsDefined( affinity ) ) throw new ArgumentOutOfRangeException( nameof( affinity ) );
		if ( !this.document.IsTextElementBoundary( anchor ) ) throw new ArgumentOutOfRangeException( nameof( anchor ) );
		if ( !this.document.IsTextElementBoundary( caret ) ) throw new ArgumentOutOfRangeException( nameof( caret ) );
		bool selectionChanged = this.Anchor != anchor || this.Caret != caret; bool affinityChanged = this.caretAffinity != affinity;
		if ( !selectionChanged && !affinityChanged ) return;
		this.Anchor = anchor; this.Caret = caret; this.caretAffinity = affinity;
		if ( selectionChanged ) this.SelectionEpoch++;
		this.ViewRevision++;
		if ( !preservePreferredColumn ) this.preferredColumn = null;
	}

	private void OnDocumentChanged( object? sender, WidgetTextDocumentChangedEventArgs change ) {
		long anchor = Transform( this.Anchor, change.Changes, this.document.Length ); long caret = Transform( this.Caret, change.Changes, this.document.Length );
		this.LastDocumentChange = change;
		if ( anchor != this.Anchor || caret != this.Caret ) { this.Anchor = anchor; this.Caret = caret; this.SelectionEpoch++; }
		this.ViewRevision++; this.preferredColumn = null; this.caretAffinity = CursesTextAffinity.Leading;
	}

	private static long Transform( long position, IReadOnlyList<WidgetTextChange> changes, long maximum ) {
		long shift = 0;
		foreach ( WidgetTextChange change in changes ) {
			if ( position < change.Start ) break;
			if ( position == change.Start || position < change.Start + change.OldLength ) return Math.Clamp( change.Start + shift, 0, maximum );
			shift = checked( shift + change.NewLength - change.OldLength );
		}
		return Math.Clamp( checked( position + shift ), 0, maximum );
	}

	private void ThrowIfDisposed() { if ( this.disposed ) throw new ObjectDisposedException( nameof( WidgetTextViewState ) ); }
}
