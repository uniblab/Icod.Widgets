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
using System.Text;
using Icod.DCurses;

/// <summary>Provides bounded, Unicode-aware multiline editing over a caller-owned text view.</summary>
public sealed class WidgetTextEditor : Widget, IWidgetEditTarget, IWidgetClipboardTarget {
	private enum WordClass { Word, WhiteSpace, Other }
	private static readonly UTF8Encoding StrictUtf8 = new( false, true );

	private readonly WidgetEditorOptions options;
	private string newLine;
	private string indentation;
	private readonly WidgetTextProjection projection;
	private StringBuilder? pendingPaste;
	private long pendingPasteRevision;
	private long pendingPasteSelectionEpoch;
	private bool pasteExceeded;
	private bool isReadOnly;
	private bool pointerSelecting;
	private bool gutterPointerSequence;
	private int pointerColumn;
	private int pointerRow;
	private int horizontalAutoScroll;
	private int verticalAutoScroll;
	private TimeSpan? lastAutoScroll;
	private ICursesTextWidthProvider? pointerWidthProvider;

	public WidgetTextEditor( string id, WidgetTextViewState view, WidgetEditorOptions? options = null ) : base( id ) {
		this.View = view ?? throw new ArgumentNullException( nameof( view ) ); this.options = options ?? new WidgetEditorOptions(); this.options.Validate(); this.newLine = this.options.NewLine; this.indentation = this.options.Indentation;
		this.View.WrapMode = this.options.WrapMode; this.View.TabWidth = this.options.TabWidth; this.projection = new WidgetTextProjection( this.options );
		this.RefreshScrollRanges();
	}

	public override bool IsFocusable => true;
	public override bool AcceptsTab => true;
	public WidgetTextViewState View { get; }
	/// <summary>Sets the terminator inserted by future Enter keys without changing existing text.</summary>
	public string NewLine {
		get => this.newLine;
		set {
			if ( value is not "\n" and not "\r" and not "\r\n" )
				throw new ArgumentException( "The inserted newline must be LF, CR, or CRLF.", nameof( value ) );
			this.newLine = value;
		}
	}
	/// <summary>Sets the spaces or tabs inserted by future indentation commands.</summary>
	public string Indentation {
		get => this.indentation;
		set {
			if ( value is null || value.Any( static character => character is not ' ' and not '\t' ) )
				throw new ArgumentException( "Indentation may contain only spaces and tabs.", nameof( value ) );
			this.indentation = value;
		}
	}
	public bool IsReadOnly {
		get => this.isReadOnly;
		set { if ( this.isReadOnly == value ) return; this.isReadOnly = value; this.CancelPendingPaste(); this.View.InvalidatePendingOperations(); }
	}
	public bool IsOverwriteMode { get; set; }
	public WidgetScrollRange HorizontalScrollRange { get; private set; }
	public WidgetScrollRange VerticalScrollRange { get; private set; }

	public bool CanExecuteEditCommand( WidgetEditCommand command ) => command switch {
		WidgetEditCommand.Copy => this.TryGetClipboardText( out _, out _ ),
		WidgetEditCommand.Cut => !this.IsReadOnly && this.TryGetClipboardText( out _, out _ ),
		WidgetEditCommand.Paste => !this.IsReadOnly,
		WidgetEditCommand.SelectAll => true,
		WidgetEditCommand.Undo => !this.IsReadOnly && this.View.Document.CanUndo,
		WidgetEditCommand.Redo => !this.IsReadOnly && this.View.Document.CanRedo,
		_ => false
	};

	public WidgetCommandResult ExecuteEditCommand( WidgetEditCommand command, TimeSpan timestamp ) {
		if ( !Enum.IsDefined( command ) ) throw new ArgumentOutOfRangeException( nameof( command ) );
		if ( !this.CanExecuteEditCommand( command ) ) return WidgetCommandResult.Disabled;
		this.CancelPendingPaste();
		if ( command is WidgetEditCommand.Copy or WidgetEditCommand.Cut or WidgetEditCommand.Paste ) {
			this.EnsureActionCapacity(); string? text = null; bool isLinewise = false;
			if ( ( command is WidgetEditCommand.Copy or WidgetEditCommand.Cut ) && !this.TryGetClipboardText( out text, out isLinewise ) ) return WidgetCommandResult.Disabled;
			WidgetClipboardOperation operation = command switch { WidgetEditCommand.Copy => WidgetClipboardOperation.Copy, WidgetEditCommand.Cut => WidgetClipboardOperation.Cut, _ => WidgetClipboardOperation.Paste };
			WidgetClipboardRequest request = new( Guid.NewGuid(), this.View.Id, this.View.Document.Id, 0, 0, this.View.Document.Revision,
				this.View.SelectionEpoch, this.View.SelectionStart, this.View.SelectionEnd, operation, text ) { IsLinewise = isLinewise };
			request = this.RegisterClipboardRequest( request ); this.EmitAction( new WidgetClipboardRequestedAction( this.Id, request ) ); return WidgetCommandResult.Executed;
		}
		if ( WidgetEditCommand.SelectAll == command ) {
			if ( 0 == this.View.Anchor && this.View.Caret == this.View.Document.Length ) return WidgetCommandResult.Executed;
			this.EnsureActionCapacity(); this.View.SetSelection( 0, this.View.Document.Length ); this.EmitSelection(); return WidgetCommandResult.Executed;
		}
		this.EnsureActionCapacity( 2 );
		WidgetTextEditResult result = WidgetEditCommand.Undo == command ? this.View.Document.Undo() : this.View.Document.Redo();
		if ( WidgetTextEditStatus.Applied != result.Status ) return WidgetCommandResult.Disabled;
		if ( result.ViewId == this.View.Id && result.Selection is { } selection ) this.View.SetSelection( selection.Anchor, selection.Caret );
		this.EmitEdit(); this.EmitSelection(); this.RevealCaret(); return WidgetCommandResult.Executed;
	}

	bool IWidgetClipboardTarget.IsClipboardRequestCurrent( WidgetClipboardRequest request ) =>
		request.ViewId == this.View.Id && request.DocumentId == this.View.Document.Id && request.Revision == this.View.Document.Revision &&
		request.SelectionEpoch == this.View.SelectionEpoch && request.SelectionStart == this.View.SelectionStart && request.SelectionEnd == this.View.SelectionEnd &&
		( WidgetClipboardOperation.Copy == request.Operation || !this.IsReadOnly );

	bool IWidgetClipboardTarget.ApplyClipboardResult( WidgetClipboardRequest request, WidgetClipboardResult result, TimeSpan timestamp ) {
		if ( WidgetClipboardStatus.Success != result.Status ) return true;
		if ( WidgetClipboardOperation.Copy == request.Operation ) return true;
		if ( WidgetClipboardOperation.Cut == request.Operation ) {
			long start = request.SelectionStart;
			long length = request.SelectionEnd - start;
			if ( request.IsLinewise ) {
				WidgetTextLine line = this.View.Document.GetLine( this.View.Document.GetLineIndex( start ) );
				start = line.Start; length = line.Length;
			}
			return this.Apply( [ new WidgetTextEdit( start, length, "" ) ],
				WidgetTextEditKind.Cut, timestamp, start, start, false );
		}
		if ( result.Text is null || result.Text.Length > this.options.MaximumPendingPasteUnits || !TryGetUtf8ByteCount( result.Text, out int bytes ) || bytes > this.options.MaximumClipboardBytes ) return false;
		if ( result.IsLinewise && !this.View.HasSelection ) {
			WidgetTextLine line = this.View.Document.GetLine( this.View.Document.GetLineIndex( request.SelectionStart ) );
			string insertion = result.Text;
			if ( line.ContentLength > 0 && ( insertion.Length == 0 || insertion[ ^1 ] is not ( '\r' or '\n' ) ) ) {
				if ( insertion.Length > this.options.MaximumPendingPasteUnits - this.NewLine.Length ) return false;
				insertion += this.NewLine;
			}
			long after = checked( line.Start + insertion.Length );
			return this.Apply( [ new WidgetTextEdit( line.Start, 0, insertion ) ],
				WidgetTextEditKind.Paste, timestamp, after, after, false );
		}
		return this.ReplaceSelection( result.Text, WidgetTextEditKind.Paste, timestamp, false );
	}

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( this.pendingPaste is not null && WidgetInputKind.Paste != input.Kind ) this.CancelPendingPaste();
		if ( WidgetInputKind.Paste == input.Kind ) return this.HandlePaste( input, host.CurrentTime );
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is { } pointer ) return this.HandlePointer( pointer, host );
		if ( WidgetInputKind.Focus == input.Kind && CursesFocusState.Unfocused == input.FocusState ) { this.CancelPointerSelection(); this.CancelPendingPaste(); return false; }
		if ( WidgetInputKind.Key == input.Kind && CursesKeyEventPhase.Release == input.KeyPhase ) return false;

		if ( WidgetInputKind.Key == input.Kind ) {
			bool shift = 0 != ( input.Modifiers & WidgetInputModifiers.Shift ); bool control = 0 != ( input.Modifiers & WidgetInputModifiers.Control );
			switch ( input.KeyCode ) {
				case WidgetKey.Left: return this.MoveHorizontal( false, control, shift );
				case WidgetKey.Right: return this.MoveHorizontal( true, control, shift );
				case WidgetKey.Up: return this.MoveVertical( -1, shift, host.Screen.TextWidthProvider );
				case WidgetKey.Down: return this.MoveVertical( 1, shift, host.Screen.TextWidthProvider );
				case WidgetKey.PageUp: return this.MoveVertical( -Math.Max( 1, this.Bounds.Rows - 1 ), shift, host.Screen.TextWidthProvider );
				case WidgetKey.PageDown: return this.MoveVertical( Math.Max( 1, this.Bounds.Rows - 1 ), shift, host.Screen.TextWidthProvider );
				case WidgetKey.Home: return this.MoveHomeEnd( false, control, shift );
				case WidgetKey.End: return this.MoveHomeEnd( true, control, shift );
				case WidgetKey.Insert: this.IsOverwriteMode = !this.IsOverwriteMode; return true;
				case WidgetKey.Backspace: return this.Delete( false, control, host.CurrentTime );
				case WidgetKey.Delete: return this.Delete( true, control, host.CurrentTime );
				case WidgetKey.Tab: return shift ? this.Outdent( host.CurrentTime ) : this.Indent( host.CurrentTime );
				case WidgetKey.Enter: return this.InsertNewLine( host.CurrentTime );
				case WidgetKey.Escape: return false;
			}
		}

		if ( this.IsReadOnly || 0 != ( input.Modifiers & ( WidgetInputModifiers.Control | WidgetInputModifiers.Alt ) ) ) return false;
		string? text = input.Kind switch {
			WidgetInputKind.Text => input.Text ?? input.Character?.ToString(),
			WidgetInputKind.Key when WidgetKey.Space == input.KeyCode => " ",
			WidgetInputKind.Key when WidgetKey.Character == input.KeyCode => input.Text ?? input.Character?.ToString(),
			_ => null
		};
		return text is not null && this.ReplaceSelection( text, this.IsOverwriteMode ? WidgetTextEditKind.Overwrite : WidgetTextEditKind.Typing,
			host.CurrentTime, this.options.AllowTypingCoalescence && !this.IsOverwriteMode && !this.View.HasSelection );
	}

	protected override void OnCaptureCanceled() => this.CancelPointerSelection();
	internal override void OnDetached() { this.CancelPointerSelection(); this.CancelPendingPaste(); this.View.InvalidatePendingOperations(); }

	protected override void OnUpdate( TimeSpan timestamp ) {
		if ( !this.pointerSelecting || ( 0 == this.horizontalAutoScroll && 0 == this.verticalAutoScroll ) || this.pointerWidthProvider is null ) return;
		TimeSpan previous = this.lastAutoScroll ?? timestamp; long elapsedTicks = Math.Max( 0, ( timestamp - previous ).Ticks );
		int steps = Math.Min( this.options.MaximumWorkPerUpdate, checked( (int)Math.Min( int.MaxValue, elapsedTicks / TimeSpan.FromMilliseconds( 50 ).Ticks ) ) );
		if ( 0 == steps ) return; this.EnsureActionCapacity(); this.lastAutoScroll = previous + TimeSpan.FromTicks( checked( steps * TimeSpan.FromMilliseconds( 50 ).Ticks ) );
		long vertical = this.View.VerticalOffset; long horizontal = this.View.HorizontalOffset;
		long maximumVertical = Math.Max( 0, this.View.Document.LineCount - Math.Max( 1, this.Bounds.Rows ) );
		vertical = Math.Clamp( SaturatingAdd( vertical, (long)this.verticalAutoScroll * steps ), 0, maximumVertical );
		horizontal = Math.Max( 0, SaturatingAdd( horizontal, (long)this.horizontalAutoScroll * steps ) );
		this.View.SetScroll( horizontal, vertical ); long offset = this.HitTestPointer( this.pointerColumn, this.pointerRow, this.pointerWidthProvider );
		this.View.SetSelection( this.View.Anchor, offset ); this.EmitSelection(); this.RefreshScrollRanges();
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		WidgetEditorViewport viewport = WidgetEditorViewport.Create( area, this.View.Document.LineCount, this.options.ShowLineNumbers );
		CursesStyle surface = this.Style( theme, !this.IsEffectivelyEnabled ? WidgetStyleRole.Disabled : WidgetStyleRole.EditorSurface );
		CursesStyle baseStyle = this.Style( theme, !this.IsEffectivelyEnabled ? WidgetStyleRole.Disabled : focused && !this.HasPartStyle( theme, WidgetStyleRole.EditorText ) ? WidgetStyleRole.Focused : WidgetStyleRole.EditorText );
		screen.StandardWindow.FillRectangle( area.Row, area.Column, area.Rows, area.Columns, CursesCell.Blank( surface ) );
		WidgetTextProjectionResult result = this.projection.Project( this.View, screen.TextWidthProvider, new CursesRectangle( 0, 0, viewport.Text.Rows, viewport.Text.Columns ) );
		this.UpdateScrollRanges( result, viewport.Text.Rows, viewport.Text.Columns );
		long currentLine = this.options.HighlightCurrentLine && this.IsEffectivelyEnabled ? this.View.Document.GetLineIndex( this.View.Caret ) : -1;
		long gutterCurrentLine = this.IsEffectivelyEnabled ? this.View.Document.GetLineIndex( this.View.Caret ) : -1;
		CursesStyle currentLineStyle = this.Style( theme, WidgetStyleRole.EditorCurrentLine );
		CursesStyle gutterStyle = this.Style( theme, this.IsEffectivelyEnabled ? WidgetStyleRole.EditorGutter : WidgetStyleRole.Disabled );
		CursesStyle gutterCurrentLineStyle = this.Style( theme, this.IsEffectivelyEnabled ? WidgetStyleRole.EditorGutterCurrentLine : WidgetStyleRole.Disabled );
		for ( int rowIndex = 0; rowIndex < viewport.Gutter.Rows; rowIndex++ ) {
			WidgetProjectedRow? projected = rowIndex < result.Rows.Count ? result.Rows[ rowIndex ] : null;
			CursesStyle rowStyle = projected is not null && projected.LogicalLineIndex == gutterCurrentLine ? gutterCurrentLineStyle : gutterStyle;
			screen.StandardWindow.FillRectangle( viewport.Gutter.Row + rowIndex, viewport.Gutter.Column, 1, viewport.Gutter.Columns, CursesCell.Blank( rowStyle ) );
			if ( projected is null || 0 != projected.VisualLineIndex ) continue;
			string label = checked( projected.LogicalLineIndex + 1 ).ToString( CultureInfo.InvariantCulture );
			int labelColumn = viewport.Gutter.RightExclusive - 1 - label.Length;
			screen.StandardWindow.Move( viewport.Gutter.Row + rowIndex, labelColumn ); screen.StandardWindow.Write( label, rowStyle );
		}
		for ( int index = 0; index < result.Rows.Count; index++ ) {
			WidgetProjectedRow row = result.Rows[ index ]; long destination = (long)viewport.Text.Column + row.BaseColumn - this.View.HorizontalOffset;
			if ( destination < 0 || destination > int.MaxValue ) continue;
			screen.StandardWindow.PresentTextLayout( row.Layout, row.VisualLineIndex, 1, viewport.Text.Row + index, (int)destination );
		}
		for ( int row = 0; row < viewport.Text.Rows; row++ ) {
			bool isCurrentLine = row < result.Rows.Count && result.Rows[ row ].LogicalLineIndex == currentLine;
			CursesStyle textStyle = isCurrentLine ? currentLineStyle : baseStyle;
			for ( int column = 0; column < viewport.Text.Columns; column++ ) {
				CursesCell cell = screen.StandardWindow.GetCell( viewport.Text.Row + row, viewport.Text.Column + column );
				if ( isCurrentLine || !cell.IsBlank ) RestyleCell( screen.StandardWindow, viewport.Text.Row + row, viewport.Text.Column + column, cell, textStyle );
			}
		}
		foreach ( WidgetProjectedSelectionSpan span in result.SelectionSpans ) for ( int column = span.Column; column < span.Column + span.Columns && column < viewport.Text.Columns; column++ ) {
			CursesCell cell = screen.StandardWindow.GetCell( viewport.Text.Row + span.Row, viewport.Text.Column + column );
			RestyleCell( screen.StandardWindow, viewport.Text.Row + span.Row, viewport.Text.Column + column, cell, this.Style( theme, !this.IsEffectivelyEnabled ? WidgetStyleRole.Disabled : focused ? WidgetStyleRole.TextSelection : WidgetStyleRole.TextSelectionInactive ) );
		}
	}

	protected override void OnArrange( CursesRectangle bounds ) { this.CancelPointerSelection(); this.RefreshScrollRanges(); }

	protected override WidgetCaretIntent? GetCaretIntent() {
		WidgetHost? host = this.Host; if ( host is null ) return null; CursesRectangle area = this.Bounds.Intersect( host.Screen.Bounds ); if ( area.IsEmpty ) return null;
		WidgetEditorViewport viewport = WidgetEditorViewport.Create( area, this.View.Document.LineCount, this.options.ShowLineNumbers );
		WidgetTextProjectionResult result = this.projection.Project( this.View, host.Screen.TextWidthProvider, new CursesRectangle( 0, 0, viewport.Text.Rows, viewport.Text.Columns ) );
		if ( result.Caret is not { } caret ) return null; int row = viewport.Text.Row + caret.Row; int column = viewport.Text.Column + caret.Column;
		if ( !viewport.Text.Contains( row, column ) ) return null;
		return new WidgetCaretIntent( true, row, column, this.IsOverwriteMode ? CursesCursorStyle.SteadyBlock : CursesCursorStyle.SteadyBar, 0 );
	}

	private bool HandlePaste( WidgetInput input, TimeSpan timestamp ) {
		if ( input.PastePhase is null ) {
			this.CancelPendingPaste(); if ( this.IsReadOnly ) return true;
			string text = input.Text ?? ""; if ( text.Length > this.options.MaximumPendingPasteUnits ) return true;
			return this.ReplaceSelection( text, WidgetTextEditKind.Paste, timestamp, false );
		}
		switch ( input.PastePhase.Value ) {
			case CursesPastePhase.Begin:
				this.pendingPaste = new StringBuilder( Math.Min( this.options.MaximumPendingPasteUnits, 4_096 ) ); this.pasteExceeded = false;
				this.pendingPasteRevision = this.View.Document.Revision; this.pendingPasteSelectionEpoch = this.View.SelectionEpoch; return true;
			case CursesPastePhase.Data:
				if ( this.pendingPaste is null || this.pasteExceeded ) return true;
				string chunk = input.Text ?? "";
				if ( chunk.Length > this.options.MaximumPendingPasteUnits - this.pendingPaste.Length ) { this.pendingPaste.Clear(); this.pasteExceeded = true; }
				else this.pendingPaste.Append( chunk );
				return true;
			case CursesPastePhase.End:
				if ( this.pendingPaste is null ) return true;
				if ( input.Text is { Length: > 0 } tail && !this.pasteExceeded ) {
					if ( tail.Length > this.options.MaximumPendingPasteUnits - this.pendingPaste.Length ) { this.pendingPaste.Clear(); this.pasteExceeded = true; }
					else this.pendingPaste.Append( tail );
				}
				bool valid = !this.IsReadOnly && !this.pasteExceeded && this.pendingPasteRevision == this.View.Document.Revision && this.pendingPasteSelectionEpoch == this.View.SelectionEpoch;
				string completed = valid ? this.pendingPaste.ToString() : ""; this.CancelPendingPaste();
				return !valid || this.ReplaceSelection( completed, WidgetTextEditKind.Paste, timestamp, false );
			default: return true;
		}
	}

	private bool HandlePointer( CursesMouseEvent pointer, WidgetHost host ) {
		this.pointerWidthProvider = host.Screen.TextWidthProvider;
		if ( pointer.Action is CursesMouseAction.WheelUp or CursesMouseAction.WheelDown ) {
			long maximum = Math.Max( 0, this.View.Document.LineCount - Math.Max( 1, this.Bounds.Rows ) );
			long target = Math.Clamp( SaturatingAdd( this.View.VerticalOffset, CursesMouseAction.WheelUp == pointer.Action ? -3 : 3 ), 0, maximum );
			if ( target != this.View.VerticalOffset ) this.View.SetScroll( this.View.HorizontalOffset, target ); this.RefreshScrollRanges(); return true;
		}
		if ( pointer.Action is CursesMouseAction.WheelLeft or CursesMouseAction.WheelRight ) {
			long target = Math.Max( 0, SaturatingAdd( this.View.HorizontalOffset, CursesMouseAction.WheelLeft == pointer.Action ? -4 : 4 ) );
			if ( target != this.View.HorizontalOffset ) this.View.SetScroll( target, this.View.VerticalOffset ); this.RefreshScrollRanges(); return true;
		}
		this.pointerColumn = pointer.Column; this.pointerRow = pointer.Row;
		if ( this.gutterPointerSequence ) {
			if ( CursesMouseAction.Release == pointer.Action ) this.CancelPointerSelection();
			return true;
		}
		if ( CursesMouseAction.Move == pointer.Action && this.pointerSelecting ) {
			this.UpdateAutoScroll( pointer, host.CurrentTime ); long offset = this.HitTestPointer( pointer.Column, pointer.Row, host.Screen.TextWidthProvider );
			return this.SetPointerSelection( this.View.Anchor, offset );
		}
		if ( CursesMouseAction.Release == pointer.Action && this.pointerSelecting ) {
			long offset = this.HitTestPointer( pointer.Column, pointer.Row, host.Screen.TextWidthProvider ); bool changed = this.SetPointerSelection( this.View.Anchor, offset );
			this.CancelPointerSelection(); return changed;
		}
		if ( CursesMouseButton.Primary != pointer.Button ) return false;
		if ( CursesMouseAction.Press == pointer.Action ) {
			WidgetEditorViewport viewport = WidgetEditorViewport.Create( this.Bounds, this.View.Document.LineCount, this.options.ShowLineNumbers );
			if ( viewport.Gutter.Contains( pointer.Row, pointer.Column ) ) { this.CancelPointerSelection(); this.gutterPointerSequence = true; return true; }
			long offset = this.HitTestPointer( pointer.Column, pointer.Row, host.Screen.TextWidthProvider ); bool extend = 0 != ( pointer.Modifiers & CursesKeyModifiers.Shift );
			this.pointerSelecting = true; this.UpdateAutoScroll( pointer, host.CurrentTime );
			return this.SetPointerSelection( extend ? this.View.Anchor : offset, offset );
		}
		return false;
	}

	private long HitTestPointer( int screenColumn, int screenRow, ICursesTextWidthProvider widthProvider ) {
		CursesRectangle text = WidgetEditorViewport.Create( this.Bounds, this.View.Document.LineCount, this.options.ShowLineNumbers ).Text;
		int rows = Math.Max( 1, text.Rows ); int columns = Math.Max( 1, text.Columns );
		WidgetTextProjectionResult result = this.projection.Project( this.View, widthProvider, new CursesRectangle( 0, 0, rows, columns ) );
		if ( 0 == result.Rows.Count ) return 0; int row = Math.Clamp( screenRow - text.Row, 0, result.Rows.Count - 1 );
		long localColumn = Math.Clamp( (long)screenColumn - text.Column, 0, columns ); long trueColumn = SaturatingAdd( this.View.HorizontalOffset, localColumn );
		return result.Rows[ row ].HitTest( trueColumn );
	}

	private bool SetPointerSelection( long anchor, long caret ) {
		if ( anchor == this.View.Anchor && caret == this.View.Caret ) return true;
		this.EnsureActionCapacity(); this.View.SetSelection( anchor, caret ); this.EmitSelection(); return true;
	}

	private void UpdateAutoScroll( CursesMouseEvent pointer, TimeSpan timestamp ) {
		CursesRectangle text = WidgetEditorViewport.Create( this.Bounds, this.View.Document.LineCount, this.options.ShowLineNumbers ).Text;
		this.horizontalAutoScroll = pointer.Column < text.Column ? -1 : pointer.Column >= text.RightExclusive ? 1 : 0;
		this.verticalAutoScroll = pointer.Row < this.Bounds.Row ? -1 : pointer.Row >= this.Bounds.BottomExclusive ? 1 : 0;
		if ( 0 != this.horizontalAutoScroll || 0 != this.verticalAutoScroll ) this.lastAutoScroll ??= timestamp; else this.lastAutoScroll = null;
	}

	private void CancelPointerSelection() {
		this.pointerSelecting = false; this.gutterPointerSequence = false; this.horizontalAutoScroll = 0; this.verticalAutoScroll = 0; this.lastAutoScroll = null; this.pointerWidthProvider = null;
	}

	private bool MoveHorizontal( bool forward, bool word, bool extend ) {
		long target;
		if ( !extend && this.View.HasSelection ) target = forward ? this.View.SelectionEnd : this.View.SelectionStart;
		else target = word ? forward ? this.FindWordNext( this.View.Caret ) : this.FindWordPrevious( this.View.Caret )
			: forward ? this.NextTextElement( this.View.Caret ) : this.PreviousTextElement( this.View.Caret );
		return this.SetCaret( target, extend );
	}

	private bool MoveVertical( int delta, bool extend, ICursesTextWidthProvider widthProvider ) {
		long currentLine = this.View.Document.GetLineIndex( this.View.Caret ); long targetLine = Math.Clamp( currentLine + delta, 0, this.View.Document.LineCount - 1 );
		if ( targetLine == currentLine ) return true;
		this.EnsureActionCapacity(); long anchor = this.View.Anchor;
		CursesRectangle text = WidgetEditorViewport.Create( this.Bounds, this.View.Document.LineCount, this.options.ShowLineNumbers ).Text;
		if ( !this.projection.MoveVertically( this.View, checked( (int)( targetLine - currentLine ) ), widthProvider,
			new CursesRectangle( 0, 0, Math.Max( 1, text.Rows ), Math.Max( 1, text.Columns ) ) ) ) return true;
		if ( extend ) this.View.SetSelectionFromProjection( anchor, this.View.Caret, true );
		this.EmitSelection(); this.RevealCaret(); return true;
	}

	private bool MoveHomeEnd( bool end, bool document, bool extend ) {
		long target;
		if ( document ) target = end ? this.View.Document.Length : 0;
		else { WidgetTextLine line = this.View.Document.GetLine( this.View.Document.GetLineIndex( this.View.Caret ) ); target = end ? line.Start + line.ContentLength : line.Start; }
		return this.SetCaret( target, extend );
	}

	private bool SetCaret( long target, bool extend ) {
		long anchor = extend ? this.View.Anchor : target; if ( anchor == this.View.Anchor && target == this.View.Caret ) return true;
		this.EnsureActionCapacity(); this.View.SetSelection( anchor, target ); this.EmitSelection(); this.RevealCaret(); return true;
	}

	private bool Delete( bool forward, bool word, TimeSpan timestamp ) {
		if ( this.IsReadOnly ) return true;
		long start = this.View.SelectionStart; long end = this.View.SelectionEnd;
		if ( start == end ) {
			if ( forward ) end = word ? this.FindWordNext( end ) : this.NextTextElement( end );
			else start = word ? this.FindWordPrevious( start ) : this.PreviousTextElement( start );
		}
		if ( start == end ) return true;
		WidgetTextEditKind kind = forward ? WidgetTextEditKind.Delete : WidgetTextEditKind.Backspace;
		return this.Apply( [ new WidgetTextEdit( start, end - start, "" ) ], kind, timestamp, start, start,
			this.options.AllowTypingCoalescence && !this.View.HasSelection );
	}

	private bool ReplaceSelection( string text, WidgetTextEditKind kind, TimeSpan timestamp, bool coalesce ) {
		long start = this.View.SelectionStart; long deleteLength = this.View.SelectionEnd - start;
		if ( WidgetTextEditKind.Overwrite == kind && 0 == deleteLength && 0 < text.Length ) {
			WidgetTextLine line = this.View.Document.GetLine( this.View.Document.GetLineIndex( start ) );
			if ( start < line.Start + line.ContentLength ) deleteLength = this.NextTextElement( start ) - start;
		}
		return this.Apply( [ new WidgetTextEdit( start, deleteLength, text ) ], kind, timestamp, start + text.Length, start + text.Length, coalesce );
	}

	private bool InsertNewLine( TimeSpan timestamp ) {
		if ( this.IsReadOnly ) return true;
		string insertion = this.NewLine;
		if ( this.options.AutoIndent ) {
			WidgetTextLine line = this.View.Document.GetLine( this.View.Document.GetLineIndex( this.View.SelectionStart ) );
			int count = checked( (int)Math.Min( line.ContentLength, this.options.MaximumPendingPasteUnits ) ); string content = this.View.Document.Read( line.Start, count );
			int prefix = 0; while ( prefix < content.Length && content[ prefix ] is ' ' or '\t' ) prefix++;
			insertion += content[ ..prefix ];
		}
		return this.ReplaceSelection( insertion, WidgetTextEditKind.NewLine, timestamp, false );
	}

	private bool Indent( TimeSpan timestamp ) {
		if ( this.IsReadOnly ) return true;
		if ( !this.View.HasSelection ) return this.ReplaceSelection( this.Indentation, WidgetTextEditKind.Indent, timestamp, false );
		if ( 0 == this.Indentation.Length ) return true;
		List<long> starts = this.GetSelectedLineStarts(); List<WidgetTextEdit> edits = starts.Select( start => new WidgetTextEdit( start, 0, this.Indentation ) ).ToList();
		return this.ApplySelectionBatch( edits, WidgetTextEditKind.Indent, timestamp );
	}

	private bool Outdent( TimeSpan timestamp ) {
		if ( this.IsReadOnly ) return true;
		List<long> starts = this.View.HasSelection ? this.GetSelectedLineStarts() : [ this.View.Document.GetLine( this.View.Document.GetLineIndex( this.View.Caret ) ).Start ];
		List<WidgetTextEdit> edits = [];
		foreach ( long start in starts ) {
			WidgetTextLine line = this.View.Document.GetLine( this.View.Document.GetLineIndex( start ) ); int count = checked( (int)Math.Min( line.ContentLength, Math.Max( this.Indentation.Length, this.View.TabWidth ) ) );
			string prefix = this.View.Document.Read( start, count ); int remove = 0;
			if ( 0 < this.Indentation.Length && prefix.StartsWith( this.Indentation, StringComparison.Ordinal ) ) remove = this.Indentation.Length;
			else if ( prefix.StartsWith( '\t' ) ) remove = 1;
			else while ( remove < prefix.Length && remove < this.View.TabWidth && ' ' == prefix[ remove ] ) remove++;
			if ( 0 < remove ) edits.Add( new WidgetTextEdit( start, remove, "" ) );
		}
		return 0 == edits.Count || this.ApplySelectionBatch( edits, WidgetTextEditKind.Outdent, timestamp );
	}

	private bool ApplySelectionBatch( IReadOnlyList<WidgetTextEdit> edits, WidgetTextEditKind kind, TimeSpan timestamp ) {
		long start = this.View.SelectionStart; long end = this.View.SelectionEnd;
		long nextStart = Transform( start, edits, false ); long nextEnd = Transform( end, edits, true );
		long anchor = this.View.Anchor <= this.View.Caret ? nextStart : nextEnd; long caret = this.View.Anchor <= this.View.Caret ? nextEnd : nextStart;
		return this.Apply( edits, kind, timestamp, anchor, caret, false );
	}

	private bool Apply( IReadOnlyList<WidgetTextEdit> edits, WidgetTextEditKind kind, TimeSpan timestamp, long afterAnchor, long afterCaret, bool coalesce ) {
		this.EnsureActionCapacity( 2 ); WidgetTextDocument document = this.View.Document;
		WidgetTextEditContext context = new( this.View.Id, this.View.Anchor, this.View.Caret, afterAnchor, afterCaret, kind, timestamp, coalesce );
		WidgetTextEditResult result = document.Apply( edits, document.Revision, context );
		if ( WidgetTextEditStatus.Applied != result.Status ) return true;
		this.View.SetSelection( afterAnchor, afterCaret ); this.EmitEdit(); this.EmitSelection(); this.RevealCaret(); return true;
	}

	private List<long> GetSelectedLineStarts() {
		long first = this.View.Document.GetLineIndex( this.View.SelectionStart ); long endProbe = this.View.SelectionEnd;
		if ( endProbe > this.View.SelectionStart ) endProbe--; long last = this.View.Document.GetLineIndex( endProbe ); List<long> result = [];
		for ( long line = first; line <= last; line++ ) result.Add( this.View.Document.GetLine( line ).Start ); return result;
	}

	private long PreviousTextElement( long position ) {
		if ( 0 == position ) return 0; int count = checked( (int)Math.Min( position, this.options.MaximumClusterScan ) ); long origin = position - count;
		string source = this.View.Document.Read( origin, count ); int[] starts = StringInfo.ParseCombiningCharacters( source );
		for ( int index = starts.Length - 1; index >= 0; index-- ) { long candidate = origin + starts[ index ]; if ( candidate < position && this.View.Document.IsTextElementBoundary( candidate ) ) return candidate; }
		return position;
	}

	private long NextTextElement( long position ) {
		if ( position == this.View.Document.Length ) return position; int count = checked( (int)Math.Min( this.View.Document.Length - position, this.options.MaximumClusterScan ) );
		string source = this.View.Document.Read( position, count ); int[] starts = StringInfo.ParseCombiningCharacters( source );
		for ( int index = 1; index < starts.Length; index++ ) { long candidate = position + starts[ index ]; if ( this.View.Document.IsTextElementBoundary( candidate ) ) return candidate; }
		long end = position + source.Length; return end == this.View.Document.Length && this.View.Document.IsTextElementBoundary( end ) ? end : position;
	}

	private long FindWordNext( long position ) {
		if ( position == this.View.Document.Length ) return position; WordClass category = this.GetWordClass( position ); long current = position;
		for ( int work = 0; work < this.options.MaximumWorkPerUpdate && current < this.View.Document.Length; work++ ) {
			long next = this.NextTextElement( current ); if ( next == current ) break; current = next;
			if ( current == this.View.Document.Length || this.GetWordClass( current ) != category ) break;
		}
		return current;
	}

	private long FindWordPrevious( long position ) {
		if ( 0 == position ) return 0; long current = this.PreviousTextElement( position ); if ( current == position ) return position; WordClass category = this.GetWordClass( current );
		for ( int work = 1; work < this.options.MaximumWorkPerUpdate && 0 < current; work++ ) {
			long previous = this.PreviousTextElement( current ); if ( previous == current || this.GetWordClass( previous ) != category ) break; current = previous;
		}
		return current;
	}

	private WordClass GetWordClass( long position ) {
		long next = this.NextTextElement( position ); if ( next == position ) return WordClass.Other;
		string element = this.View.Document.Read( position, checked( (int)( next - position ) ) ); Rune rune = element.EnumerateRunes().First();
		if ( Rune.IsWhiteSpace( rune ) ) return WordClass.WhiteSpace;
		UnicodeCategory category = Rune.GetUnicodeCategory( rune );
		return category is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or
			UnicodeCategory.OtherLetter or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.ConnectorPunctuation or UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark
			? WordClass.Word : WordClass.Other;
	}

	private void RevealCaret() {
		if ( 0 == this.Bounds.Rows ) return; long line = this.View.Document.GetLineIndex( this.View.Caret ); long vertical = this.View.VerticalOffset;
		if ( line < vertical ) vertical = line; else if ( line >= vertical + this.Bounds.Rows ) vertical = line - this.Bounds.Rows + 1;
		if ( vertical != this.View.VerticalOffset ) this.View.SetScroll( this.View.HorizontalOffset, vertical ); this.RefreshScrollRanges();
	}

	private void RefreshScrollRanges() {
		int textColumns = WidgetEditorViewport.Create( this.Bounds, this.View.Document.LineCount, this.options.ShowLineNumbers ).Text.Columns;
		long verticalExtent = Math.Max( this.View.Document.LineCount, SaturatingAdd( this.View.VerticalOffset, this.Bounds.Rows ) );
		this.VerticalScrollRange = new WidgetScrollRange( verticalExtent, this.Bounds.Rows, this.View.VerticalOffset, CursesTextWrapMode.NoWrap == this.View.WrapMode );
		long horizontalExtent = Math.Max( textColumns, SaturatingAdd( this.View.HorizontalOffset, textColumns ) );
		this.HorizontalScrollRange = new WidgetScrollRange( horizontalExtent, textColumns, this.View.HorizontalOffset, CursesTextWrapMode.NoWrap != this.View.WrapMode );
	}

	private void UpdateScrollRanges( WidgetTextProjectionResult result, int rows, int columns ) {
		long verticalExtent = Math.Max( result.VerticalExtent, SaturatingAdd( this.View.VerticalOffset, rows ) );
		this.VerticalScrollRange = new WidgetScrollRange( verticalExtent, rows, this.View.VerticalOffset, result.IsVerticalExtentExact );
		long horizontalExtent = SaturatingAdd( this.View.HorizontalOffset, columns );
		foreach ( WidgetProjectedRow row in result.Rows ) horizontalExtent = Math.Max( horizontalExtent, row.GetColumn( row.SourceEnd ) );
		bool horizontalExact = CursesTextWrapMode.NoWrap != this.View.WrapMode;
		this.HorizontalScrollRange = new WidgetScrollRange( horizontalExtent, columns, this.View.HorizontalOffset, horizontalExact );
	}

	private void EmitEdit() => this.EmitAction( new WidgetTextEditedAction( this.Id, this.View.Document.Id, this.View.Document.Revision, this.View.Document.StateId ) );
	private void EmitSelection() => this.EmitAction( new WidgetTextSelectionChangedAction( this.Id, this.View.Id, this.View.Anchor, this.View.Caret ) );
	private void CancelPendingPaste() { this.pendingPaste?.Clear(); this.pendingPaste = null; this.pasteExceeded = false; }
	private static void RestyleCell( CursesWindow window, int row, int column, CursesCell cell, CursesStyle style ) {
		if ( cell.IsContinuation ) return;
		if ( cell.IsBlank ) { window.FillRectangle( row, column, 1, 1, CursesCell.Blank( style ) ); return; }
		window.Move( row, column ); window.Write( cell.Content, style );
	}

	private static long Transform( long position, IReadOnlyList<WidgetTextEdit> edits, bool moveAtInsertion ) {
		long delta = 0;
		foreach ( WidgetTextEdit edit in edits.OrderBy( static item => item.Start ) ) {
			if ( position < edit.Start ) break;
			if ( position == edit.Start && 0 == edit.DeleteLength ) return checked( edit.Start + delta + ( moveAtInsertion ? edit.InsertText.Length : 0 ) );
			if ( position < edit.Start + edit.DeleteLength ) return checked( edit.Start + delta );
			delta = checked( delta + edit.InsertText.Length - edit.DeleteLength );
		}
		return checked( position + delta );
	}

	private bool TryGetClipboardText( out string? text, out bool isLinewise ) {
		text = null; isLinewise = !this.View.HasSelection;
		long start = this.View.SelectionStart;
		long length = this.View.SelectionEnd - start;
		if ( isLinewise ) {
			WidgetTextLine line = this.View.Document.GetLine( this.View.Document.GetLineIndex( this.View.Caret ) );
			start = line.Start; length = line.Length;
		}
		if ( 0 == length || length > this.options.MaximumClipboardBytes || length > int.MaxValue ) return false;
		string candidate = this.View.Document.Read( start, checked( (int)length ) );
		if ( !TryGetUtf8ByteCount( candidate, out int bytes ) || bytes > this.options.MaximumClipboardBytes ) return false;
		text = candidate; return true;
	}

	private static bool TryGetUtf8ByteCount( string text, out int bytes ) {
		try { bytes = StrictUtf8.GetByteCount( text ); return true; }
		catch ( EncoderFallbackException ) { bytes = 0; return false; }
	}

	private static long SaturatingAdd( long left, long right ) {
		if ( 0 < right && left > long.MaxValue - right ) return long.MaxValue;
		if ( long.MinValue == right || 0 > right && left < -right ) return 0;
		return left + right;
	}
}
