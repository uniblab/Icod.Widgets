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
public sealed class WidgetTextInput : Widget {
	private string value;
	private int caret;
	private int maxTextElements = 4096;
	public WidgetTextInput( string id, string value = "" ) : base( id ) { this.value = TakeTextElements( NormalizePaste( value ?? throw new ArgumentNullException( nameof( value ) ) ), this.maxTextElements ); this.caret = StringInfo.ParseCombiningCharacters( this.value ).Length; }
	public override bool IsFocusable => true;
	public string Value => this.value;
	public int MaxTextElements {
		get => this.maxTextElements;
		set {
			if ( value is < 0 or > 100_000 ) throw new ArgumentOutOfRangeException( nameof( value ) );
			this.maxTextElements = value;
			this.value = TakeTextElements( this.value, value ); this.caret = Math.Min( this.caret, StringInfo.ParseCombiningCharacters( this.value ).Length );
		}
	}

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		int[] starts = StringInfo.ParseCombiningCharacters( this.value );
		int position = this.caret < starts.Length ? starts[ this.caret ] : this.value.Length;
		if ( input.Kind == WidgetInputKind.Key && input.KeyCode is WidgetKey.Left or WidgetKey.Right or WidgetKey.Home or WidgetKey.End ) {
			this.caret = input.KeyCode switch { WidgetKey.Left => Math.Max( 0, this.caret - 1 ), WidgetKey.Right => Math.Min( starts.Length, this.caret + 1 ), WidgetKey.Home => 0, _ => starts.Length }; return true;
		}
		string? candidate = null; int nextCaret = this.caret;
		string? inserted = input.Kind == WidgetInputKind.Paste ? NormalizePaste( input.Text ?? "" ) :
			input.Character.HasValue && ( input.Modifiers & ( WidgetInputModifiers.Control | WidgetInputModifiers.Alt ) ) == 0 ? input.Character.Value.ToString() : input.KeyCode == WidgetKey.Space ? " " : null;
		if ( inserted is not null ) {
			inserted = TakeTextElements( inserted, Math.Max( 0, this.MaxTextElements - starts.Length ) + 1 );
			candidate = this.value.Insert( position, inserted );
			while ( StringInfo.ParseCombiningCharacters( candidate ).Length > this.MaxTextElements && inserted.Length > 0 ) { int[] elements = StringInfo.ParseCombiningCharacters( inserted ); inserted = inserted[ ..elements[ ^1 ] ]; candidate = this.value.Insert( position, inserted ); }
			nextCaret = StringInfo.ParseCombiningCharacters( candidate[ ..( position + inserted.Length ) ] ).Length;
		} else if ( input.KeyCode == WidgetKey.Backspace && this.caret > 0 ) { int previous = starts[ this.caret - 1 ]; candidate = this.value.Remove( previous, position - previous ); nextCaret--; }
		else if ( input.KeyCode == WidgetKey.Delete && this.caret < starts.Length ) { int end = this.caret + 1 < starts.Length ? starts[ this.caret + 1 ] : this.value.Length; candidate = this.value.Remove( position, end - position ); }
		if ( candidate is null ) return false;
		if ( StringComparer.Ordinal.Equals( candidate, this.value ) ) return true;
		host.Enqueue( new WidgetValueChangedAction<string>( this.Id, candidate ) );
		this.value = candidate; this.caret = Math.Min( nextCaret, StringInfo.ParseCombiningCharacters( candidate ).Length ); return true;
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		int[] starts = StringInfo.ParseCombiningCharacters( this.value ); int position = this.caret < starts.Length ? starts[ this.caret ] : this.value.Length;
		int start = 0;
		while ( start < position && CursesText.MeasureColumns( this.value[ start..position ], screen.TextWidthProvider ) > area.Columns ) { int next = Array.FindIndex( starts, element => element > start ); start = next >= 0 ? starts[ next ] : position; }
		string display = CursesText.TruncateToColumns( this.value[ start.. ], area.Columns, screen.TextWidthProvider );
		screen.StandardWindow.Move( area.Row, area.Column );
		screen.StandardWindow.Write( new string( ' ', area.Columns ), !this.IsEffectivelyEnabled ? theme.Disabled : focused ? theme.Focused : theme.Normal );
		screen.StandardWindow.Move( area.Row, area.Column );
		screen.StandardWindow.Write( display, !this.IsEffectivelyEnabled ? theme.Disabled : focused ? theme.Focused : theme.Normal );
	}

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

