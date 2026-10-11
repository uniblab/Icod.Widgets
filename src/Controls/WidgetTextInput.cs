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

/// <summary>Edits one bounded line by Unicode text element.</summary>
public sealed class WidgetTextInput : Widget, IWidgetEditTarget, IWidgetClipboardTarget {
	private sealed record HistoryState( string Value, int Anchor, int Caret );
	private static readonly UTF8Encoding StrictUtf8 = new( false, true );
	private readonly Guid documentId = Guid.NewGuid();
	private readonly Guid viewId = Guid.NewGuid();
	private readonly List<HistoryState> undo = [];
	private readonly List<HistoryState> redo = [];
	private string value;
	private int anchor;
	private int caret;
	private int maxTextElements = 4096;
	private int maximumClipboardBytes = 65_536;
	private long revision;
	private long selectionEpoch;
	private StringBuilder? pendingPaste;
	private long pendingRevision;
	private long pendingSelectionEpoch;
	private bool pasteExceeded;
	private bool isReadOnly;
	private WidgetHost? dispatchHost;

	public WidgetTextInput( string id, string value = "" ) : base( id ) {
		this.value = TakeTextElements( NormalizePaste( value ?? throw new ArgumentNullException( nameof( value ) ) ), this.maxTextElements ); this.anchor = this.caret = this.value.Length;
	}

	public override bool IsFocusable => true;
	public string Value => this.value;
	public int Anchor => this.anchor;
	public int Caret => this.caret;
	public int SelectionStart => Math.Min( this.anchor, this.caret );
	public int SelectionEnd => Math.Max( this.anchor, this.caret );
	public bool HasSelection => this.anchor != this.caret;
	public bool IsReadOnly {
		get => this.isReadOnly;
		set { if ( this.isReadOnly == value ) return; this.isReadOnly = value; this.CancelPendingPaste(); this.selectionEpoch++; }
	}
	public int MaximumClipboardBytes {
		get => this.maximumClipboardBytes;
		set { if ( value is < 0 or > WidgetEditorOptions.MaximumSupportedClipboardBytes ) throw new ArgumentOutOfRangeException( nameof( value ) ); if ( this.maximumClipboardBytes == value ) return; this.maximumClipboardBytes = value; this.selectionEpoch++; }
	}
	public int MaxTextElements {
		get => this.maxTextElements;
		set {
			if ( value is < 0 or > 100_000 ) throw new ArgumentOutOfRangeException( nameof( value ) ); if ( this.maxTextElements == value ) return; this.CancelPendingPaste(); this.maxTextElements = value; this.selectionEpoch++;
			string next = TakeTextElements( this.value, value ); int nextAnchor = Math.Min( this.anchor, next.Length ); int nextCaret = Math.Min( this.caret, next.Length );
			if ( !StringComparer.Ordinal.Equals( next, this.value ) ) { this.value = next; this.revision++; this.undo.Clear(); this.redo.Clear(); }
			if ( nextAnchor != this.anchor || nextCaret != this.caret ) { this.anchor = nextAnchor; this.caret = nextCaret; this.selectionEpoch++; }
		}
	}

	public void SetSelection( int anchor, int caret ) {
		if ( !IsBoundary( this.value, anchor ) ) throw new ArgumentOutOfRangeException( nameof( anchor ) );
		if ( !IsBoundary( this.value, caret ) ) throw new ArgumentOutOfRangeException( nameof( caret ) );
		this.SetSelectionCore( anchor, caret );
	}

	public bool CanExecuteEditCommand( WidgetEditCommand command ) => command switch {
		WidgetEditCommand.Copy => this.TryGetClipboardText( out _ ),
		WidgetEditCommand.Cut => !this.IsReadOnly && this.TryGetClipboardText( out _ ),
		WidgetEditCommand.Paste => !this.IsReadOnly,
		WidgetEditCommand.SelectAll => true,
		WidgetEditCommand.Undo => !this.IsReadOnly && 0 < this.undo.Count,
		WidgetEditCommand.Redo => !this.IsReadOnly && 0 < this.redo.Count,
		_ => false
	};

	public WidgetCommandResult ExecuteEditCommand( WidgetEditCommand command, TimeSpan timestamp ) {
		if ( !Enum.IsDefined( command ) ) throw new ArgumentOutOfRangeException( nameof( command ) );
		if ( !this.CanExecuteEditCommand( command ) ) return WidgetCommandResult.Disabled; this.CancelPendingPaste();
		if ( WidgetEditCommand.SelectAll == command ) { this.SetSelectionCore( 0, this.value.Length ); return WidgetCommandResult.Executed; }
		if ( command is WidgetEditCommand.Copy or WidgetEditCommand.Cut or WidgetEditCommand.Paste ) {
			this.EnsureActionCapacity(); string? text = null;
			if ( ( command is WidgetEditCommand.Copy or WidgetEditCommand.Cut ) && !this.TryGetClipboardText( out text ) ) return WidgetCommandResult.Disabled;
			WidgetClipboardOperation operation = command switch { WidgetEditCommand.Copy => WidgetClipboardOperation.Copy, WidgetEditCommand.Cut => WidgetClipboardOperation.Cut, _ => WidgetClipboardOperation.Paste };
			WidgetClipboardRequest request = new( Guid.NewGuid(), this.viewId, this.documentId, 0, 0, this.revision, this.selectionEpoch,
				this.SelectionStart, this.SelectionEnd, operation, text ); request = this.RegisterClipboardRequest( request );
			this.EmitAction( new WidgetClipboardRequestedAction( this.Id, request ) ); return WidgetCommandResult.Executed;
		}
		this.EnsureActionCapacity(); HistoryState current = this.CurrentState;
		if ( WidgetEditCommand.Undo == command ) { HistoryState target = this.TakeLast( this.undo ); this.Push( this.redo, current ); this.Restore( target ); }
		else { HistoryState target = this.TakeLast( this.redo ); this.Push( this.undo, current ); this.Restore( target ); }
		this.EmitAction( new WidgetValueChangedAction<string>( this.Id, this.value ) ); return WidgetCommandResult.Executed;
	}

	bool IWidgetClipboardTarget.IsClipboardRequestCurrent( WidgetClipboardRequest request ) => request.ViewId == this.viewId && request.DocumentId == this.documentId &&
		request.Revision == this.revision && request.SelectionEpoch == this.selectionEpoch && request.SelectionStart == this.SelectionStart && request.SelectionEnd == this.SelectionEnd &&
		( WidgetClipboardOperation.Copy == request.Operation || !this.IsReadOnly );

	bool IWidgetClipboardTarget.ApplyClipboardResult( WidgetClipboardRequest request, WidgetClipboardResult result, TimeSpan timestamp ) {
		if ( WidgetClipboardStatus.Success != result.Status || WidgetClipboardOperation.Copy == request.Operation ) return true;
		if ( WidgetClipboardOperation.Cut == request.Operation ) return this.ReplaceSelection( "" );
		if ( result.Text is null || !TryGetUtf8ByteCount( result.Text, out int bytes ) || bytes > this.MaximumClipboardBytes ) return false;
		string normalized = NormalizePaste( result.Text ); if ( !this.CanReplaceWithoutTruncation( normalized ) ) return false; return this.ReplaceSelection( normalized );
	}

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		WidgetHost? previous = this.dispatchHost; this.dispatchHost = host;
		try { return this.OnInputCore( input, host ); }
		finally { this.dispatchHost = previous; }
	}

	private bool OnInputCore( WidgetInput input, WidgetHost host ) {
		if ( this.pendingPaste is not null && WidgetInputKind.Paste != input.Kind ) this.CancelPendingPaste();
		if ( WidgetInputKind.Paste == input.Kind ) return this.HandlePaste( input );
		if ( WidgetInputKind.Key == input.Kind && CursesKeyEventPhase.Release == input.KeyPhase ) return false;
		if ( WidgetInputKind.Key == input.Kind ) {
			bool shift = 0 != ( input.Modifiers & WidgetInputModifiers.Shift ); bool control = 0 != ( input.Modifiers & WidgetInputModifiers.Control );
			if ( input.KeyCode is WidgetKey.Left or WidgetKey.Right or WidgetKey.Home or WidgetKey.End ) {
				int target;
				if ( !shift && this.HasSelection && input.KeyCode is WidgetKey.Left or WidgetKey.Right ) target = WidgetKey.Left == input.KeyCode ? this.SelectionStart : this.SelectionEnd;
				else target = input.KeyCode switch {
					WidgetKey.Left => control ? this.FindWordPrevious( this.caret ) : this.PreviousBoundary( this.caret ),
					WidgetKey.Right => control ? this.FindWordNext( this.caret ) : this.NextBoundary( this.caret ),
					WidgetKey.Home => 0,
					_ => this.value.Length
				};
				this.SetSelectionCore( shift ? this.anchor : target, target ); return true;
			}
			if ( input.KeyCode is WidgetKey.Backspace or WidgetKey.Delete ) {
				if ( this.IsReadOnly ) return true; int start = this.SelectionStart; int end = this.SelectionEnd;
				if ( start == end ) { if ( WidgetKey.Backspace == input.KeyCode ) start = control ? this.FindWordPrevious( start ) : this.PreviousBoundary( start ); else end = control ? this.FindWordNext( end ) : this.NextBoundary( end ); }
				return start == end || this.ReplaceRange( start, end, "" );
			}
		}
		if ( this.IsReadOnly || 0 != ( input.Modifiers & ( WidgetInputModifiers.Control | WidgetInputModifiers.Alt ) ) ) return false;
		string? inserted = input.Kind switch {
			WidgetInputKind.Text => input.Text ?? input.Character?.ToString(),
			WidgetInputKind.Key when WidgetKey.Space == input.KeyCode => " ",
			WidgetInputKind.Key when WidgetKey.Character == input.KeyCode => input.Text ?? input.Character?.ToString(),
			_ => null
		};
		return inserted is not null && this.ReplaceSelection( inserted );
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return; CursesStyle style = !this.IsEffectivelyEnabled ? theme.Disabled : focused ? theme.Focused : theme.Normal;
		screen.StandardWindow.FillRectangle( area.Row, area.Column, area.Rows, area.Columns, CursesCell.Blank( style ) ); int start = this.GetVisibleStart( area.Columns, screen.TextWidthProvider );
		string display = CursesText.TruncateToColumns( this.value[ start.. ], area.Columns, screen.TextWidthProvider ); int end = start + display.Length;
		int selectionStart = Math.Clamp( this.SelectionStart, start, end ); int selectionEnd = Math.Clamp( this.SelectionEnd, start, end );
		screen.StandardWindow.Move( area.Row, area.Column );
		if ( selectionStart > start ) screen.StandardWindow.Write( this.value[ start..selectionStart ], style );
		if ( selectionEnd > selectionStart ) screen.StandardWindow.Write( this.value[ selectionStart..selectionEnd ], theme.Selected );
		if ( end > selectionEnd ) screen.StandardWindow.Write( this.value[ selectionEnd..end ], style );
	}

	protected override WidgetCaretIntent? GetCaretIntent() {
		WidgetHost? host = this.Host; if ( host is null ) return null; CursesRectangle area = this.Bounds.Intersect( host.Screen.Bounds ); if ( area.IsEmpty ) return null;
		int start = this.GetVisibleStart( area.Columns, host.Screen.TextWidthProvider ); int column = CursesText.MeasureColumns( this.value[ start..this.caret ], host.Screen.TextWidthProvider );
		return column < area.Columns ? new WidgetCaretIntent( true, area.Row, area.Column + column, CursesCursorStyle.SteadyBar, 0 ) : null;
	}

	private bool HandlePaste( WidgetInput input ) {
		if ( input.PastePhase is null ) { this.CancelPendingPaste(); return this.IsReadOnly || this.ReplaceSelection( NormalizePaste( input.Text ?? "" ) ); }
		switch ( input.PastePhase.Value ) {
			case CursesPastePhase.Begin:
				this.pendingPaste = new StringBuilder(); this.pendingRevision = this.revision; this.pendingSelectionEpoch = this.selectionEpoch; this.pasteExceeded = false; return true;
			case CursesPastePhase.Data:
				if ( this.pendingPaste is null || this.pasteExceeded ) return true; string chunk = input.Text ?? "";
				if ( chunk.Length > WidgetEditorOptions.MaximumSupportedPendingPasteUnits - this.pendingPaste.Length ) { this.pendingPaste.Clear(); this.pasteExceeded = true; } else this.pendingPaste.Append( chunk ); return true;
			case CursesPastePhase.End:
				if ( this.pendingPaste is null ) return true; string? text = !this.IsReadOnly && !this.pasteExceeded && this.pendingRevision == this.revision && this.pendingSelectionEpoch == this.selectionEpoch ? this.pendingPaste.ToString() : null;
				this.CancelPendingPaste(); return text is null || this.ReplaceSelection( NormalizePaste( text ) );
			default: return true;
		}
	}

	private bool ReplaceSelection( string inserted ) => this.ReplaceRange( this.SelectionStart, this.SelectionEnd, inserted );
	private bool CanReplaceWithoutTruncation( string inserted ) => CountTextElements( this.value[ ..this.SelectionStart ] + inserted + this.value[ this.SelectionEnd.. ] ) <= this.MaxTextElements;

	private bool ReplaceRange( int start, int end, string inserted ) {
		if ( this.IsReadOnly ) return true; string before = this.value; HistoryState old = this.CurrentState;
		string prefix = before[ ..start ]; string suffix = before[ end.. ]; string candidate = prefix + inserted + suffix;
		while ( CountTextElements( candidate ) > this.MaxTextElements && 0 < inserted.Length ) {
			int[] elements = StringInfo.ParseCombiningCharacters( inserted ); inserted = inserted[ ..elements[ ^1 ] ]; candidate = prefix + inserted + suffix;
		}
		if ( CountTextElements( candidate ) > this.MaxTextElements || StringComparer.Ordinal.Equals( candidate, before ) ) return true;
		this.EnsureValueActionCapacity(); this.Push( this.undo, old ); this.redo.Clear(); this.value = candidate; this.anchor = this.caret = start + inserted.Length; this.revision++; this.selectionEpoch++;
		this.EmitValueAction( candidate ); return true;
	}

	private void Restore( HistoryState state ) { this.value = state.Value; this.anchor = state.Anchor; this.caret = state.Caret; this.revision++; this.selectionEpoch++; }
	private HistoryState CurrentState => new( this.value, this.anchor, this.caret );
	private void SetSelectionCore( int anchor, int caret ) { if ( this.anchor == anchor && this.caret == caret ) return; this.anchor = anchor; this.caret = caret; this.selectionEpoch++; }
	private void CancelPendingPaste() { this.pendingPaste?.Clear(); this.pendingPaste = null; this.pasteExceeded = false; }
	private void EnsureValueActionCapacity() { if ( this.Host is not null ) this.EnsureActionCapacity(); else ( this.dispatchHost ?? throw new InvalidOperationException( "The field is not attached or embedded in an active widget dispatch." ) ).EnsureActionCapacity(); }
	private void EmitValueAction( string value ) { WidgetValueChangedAction<string> action = new( this.Id, value ); if ( this.Host is not null ) this.EmitAction( action ); else ( this.dispatchHost ?? throw new InvalidOperationException( "The field is not attached or embedded in an active widget dispatch." ) ).Enqueue( action ); }
	private void Push( List<HistoryState> stack, HistoryState state ) { if ( 4_096 == stack.Count ) stack.RemoveAt( 0 ); stack.Add( state ); }
	private HistoryState TakeLast( List<HistoryState> stack ) { HistoryState result = stack[ ^1 ]; stack.RemoveAt( stack.Count - 1 ); return result; }

	private int GetVisibleStart( int columns, ICursesTextWidthProvider widthProvider ) {
		int start = 0; int[] elements = StringInfo.ParseCombiningCharacters( this.value );
		while ( start < this.caret && CursesText.MeasureColumns( this.value[ start..this.caret ], widthProvider ) > columns ) {
			int index = Array.FindIndex( elements, element => element > start ); start = 0 <= index ? elements[ index ] : this.caret;
		}
		return start;
	}

	private int PreviousBoundary( int position ) { int[] starts = StringInfo.ParseCombiningCharacters( this.value ); int index = Array.BinarySearch( starts, position ); int insertion = 0 <= index ? index : ~index; return 0 < insertion ? starts[ insertion - 1 ] : 0; }
	private int NextBoundary( int position ) { int[] starts = StringInfo.ParseCombiningCharacters( this.value ); int index = Array.BinarySearch( starts, position ); return index >= 0 && index + 1 < starts.Length ? starts[ index + 1 ] : this.value.Length; }
	private int FindWordPrevious( int position ) { if ( 0 == position ) return 0; int current = this.PreviousBoundary( position ); int category = Category( this.value, current ); while ( 0 < current ) { int previous = this.PreviousBoundary( current ); if ( Category( this.value, previous ) != category ) break; current = previous; } return current; }
	private int FindWordNext( int position ) { if ( position == this.value.Length ) return position; int category = Category( this.value, position ); int current = position; while ( current < this.value.Length ) { int next = this.NextBoundary( current ); if ( next == this.value.Length || Category( this.value, next ) != category ) return next; current = next; } return current; }
	private static int Category( string text, int offset ) { Rune rune = text[ offset.. ].EnumerateRunes().First(); return Rune.IsWhiteSpace( rune ) ? 0 : Rune.IsLetterOrDigit( rune ) || Rune.GetUnicodeCategory( rune ) is UnicodeCategory.ConnectorPunctuation or UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark ? 1 : 2; }

	private bool TryGetClipboardText( out string? text ) { text = null; int length = this.SelectionEnd - this.SelectionStart; if ( 0 == length || length > this.MaximumClipboardBytes ) return false; string candidate = this.value.Substring( this.SelectionStart, length ); if ( !TryGetUtf8ByteCount( candidate, out int bytes ) || bytes > this.MaximumClipboardBytes ) return false; text = candidate; return true; }
	private static bool TryGetUtf8ByteCount( string text, out int bytes ) { try { bytes = StrictUtf8.GetByteCount( text ); return true; } catch ( EncoderFallbackException ) { bytes = 0; return false; } }
	private static bool IsBoundary( string text, int offset ) => offset >= 0 && offset <= text.Length && ( offset == text.Length || 0 <= Array.BinarySearch( StringInfo.ParseCombiningCharacters( text ), offset ) );
	private static int CountTextElements( string text ) => StringInfo.ParseCombiningCharacters( text ).Length;
	private static string TakeTextElements( string text, int count ) { int[] starts = StringInfo.ParseCombiningCharacters( text ); return starts.Length <= count ? text : text[ ..starts[ count ] ]; }

	private static string NormalizePaste( string text ) {
		StringBuilder result = new(); bool spacing = false;
		foreach ( Rune rune in text.EnumerateRunes() ) {
			UnicodeCategory category = Rune.GetUnicodeCategory( rune );
			if ( rune.Value is '\r' or '\n' or '\t' || Rune.IsWhiteSpace( rune ) ) { if ( !spacing && 0 < result.Length ) result.Append( ' ' ); spacing = true; }
			else if ( UnicodeCategory.Control != category ) { result.Append( rune ); spacing = false; }
		}
		return result.ToString().TrimEnd();
	}
}
