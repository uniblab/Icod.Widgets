/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using System.Globalization;
using System.Text;
using Icod.DCurses;

/// <summary>Edits one bounded line by Unicode text element.</summary>
public sealed class WidgetTextInput : Widget {
	private string value;
	private int maxTextElements = 4096;
	private int caretIndex;
	public WidgetTextInput( string id, string value = "" ) : base( id ) {
		this.value = value ?? throw new ArgumentNullException( nameof( value ) );
		this.caretIndex = StringInfo.ParseCombiningCharacters( this.value ).Length;
	}
	public override bool IsFocusable => true;
	public string Value => this.value;
	public int MaxTextElements {
		get => this.maxTextElements;
		set {
			if ( value is < 0 or > 100_000 ) throw new ArgumentOutOfRangeException( nameof( value ) );
			this.maxTextElements = value;
			this.value = TakeTextElements( this.value, value );
			this.caretIndex = Math.Min( this.caretIndex, StringInfo.ParseCombiningCharacters( this.value ).Length );
		}
	}

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		int elementCount = StringInfo.ParseCombiningCharacters( this.value ).Length;
		if ( WidgetInputKind.Key == input.Kind && input.KeyCode is WidgetKey.Left or WidgetKey.Right or WidgetKey.Home or WidgetKey.End ) {
			this.caretIndex = input.KeyCode switch { WidgetKey.Left => Math.Max( 0, this.caretIndex - 1 ), WidgetKey.Right => Math.Min( elementCount, this.caretIndex + 1 ), WidgetKey.Home => 0, _ => elementCount };
			return true;
		}
		string? insertion = null;
		if ( WidgetInputKind.Text == input.Kind && input.Character.HasValue ) insertion = input.Character.Value.ToString();
		else if ( WidgetInputKind.Key == input.Kind && WidgetKey.Character == input.KeyCode && input.Character.HasValue ) insertion = input.Character.Value.ToString();
		else if ( WidgetInputKind.Paste == input.Kind ) insertion = NormalizePaste( input.Text ?? string.Empty );
		string? candidate = insertion is null ? null : InsertAtTextElement( this.value, this.caretIndex, insertion );
		int nextCaret = insertion is null ? this.caretIndex : this.caretIndex + StringInfo.ParseCombiningCharacters( insertion ).Length;
		if ( WidgetInputKind.Key == input.Kind && WidgetKey.Backspace == input.KeyCode && 0 < this.caretIndex ) { candidate = RemoveTextElement( this.value, this.caretIndex - 1 ); nextCaret = this.caretIndex - 1; }
		else if ( WidgetInputKind.Key == input.Kind && WidgetKey.Delete == input.KeyCode && this.caretIndex < elementCount ) { candidate = RemoveTextElement( this.value, this.caretIndex ); nextCaret = this.caretIndex; }
		if ( candidate is null ) return false;
		candidate = TakeTextElements( candidate, this.MaxTextElements );
		if ( StringComparer.Ordinal.Equals( candidate, this.value ) ) return true;
		host.EnsureActionCapacity();
		this.value = candidate;
		this.caretIndex = Math.Min( nextCaret, StringInfo.ParseCombiningCharacters( this.value ).Length );
		host.Enqueue( new WidgetValueChangedAction<string>( this.Id, this.value ) );
		return true;
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		int[] starts = StringInfo.ParseCombiningCharacters( this.value ); int caretOffset = this.caretIndex >= starts.Length ? this.value.Length : starts[ this.caretIndex ]; int startIndex = 0;
		while ( startIndex < this.caretIndex && CursesText.MeasureColumns( this.value[ starts[ startIndex ]..caretOffset ], screen.TextWidthProvider ) > area.Columns ) startIndex++;
		int startOffset = startIndex < starts.Length ? starts[ startIndex ] : this.value.Length;
		string display = CursesText.TruncateToColumns( this.value[ startOffset.. ], area.Columns, screen.TextWidthProvider );
		screen.StandardWindow.Move( area.Row, area.Column );
		screen.StandardWindow.Write( display, !this.IsEffectivelyEnabled ? theme.Disabled : focused ? theme.Focused : theme.Normal );
	}

	private static string RemoveTextElement( string text, int index ) {
		int[] starts = StringInfo.ParseCombiningCharacters( text );
		if ( index < 0 || index >= starts.Length ) return text;
		int end = index + 1 < starts.Length ? starts[ index + 1 ] : text.Length;
		return text.Remove( starts[ index ], end - starts[ index ] );
	}

	private static string InsertAtTextElement( string text, int index, string insertion ) { int[] starts = StringInfo.ParseCombiningCharacters( text ); int offset = index >= starts.Length ? text.Length : starts[ index ]; return text.Insert( offset, insertion ); }

	private static string TakeTextElements( string text, int count ) {
		int[] starts = StringInfo.ParseCombiningCharacters( text );
		return starts.Length <= count ? text : text[ ..starts[ count ] ];
	}

	private static string NormalizePaste( string text ) {
		StringBuilder result = new(); bool spacing = false;
		foreach ( Rune rune in text.EnumerateRunes() ) {
			UnicodeCategory category = Rune.GetUnicodeCategory( rune );
			if ( rune.Value is '\r' or '\n' or '\t' || Rune.IsWhiteSpace( rune ) ) {
				if ( !spacing && 0 < result.Length ) result.Append( ' ' );
				spacing = true;
			} else if ( UnicodeCategory.Control != category ) {
				result.Append( rune ); spacing = false;
			}
		}
		return result.ToString().TrimEnd();
	}
}

