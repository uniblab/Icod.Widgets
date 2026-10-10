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
	public WidgetTextInput( string id, string value = "" ) : base( id ) => this.value = value ?? throw new ArgumentNullException( nameof( value ) );
	public override bool IsFocusable => true;
	public string Value => this.value;
	public int MaxTextElements {
		get => this.maxTextElements;
		set {
			if ( value is < 0 or > 100_000 ) throw new ArgumentOutOfRangeException( nameof( value ) );
			this.maxTextElements = value;
			this.value = TakeTextElements( this.value, value );
		}
	}

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		string? candidate = null;
		if ( WidgetInputKind.Text == input.Kind && input.Character.HasValue ) candidate = this.value + input.Character.Value;
		else if ( WidgetInputKind.Key == input.Kind && WidgetKey.Character == input.KeyCode && input.Character.HasValue ) candidate = this.value + input.Character.Value;
		else if ( WidgetInputKind.Paste == input.Kind ) candidate = this.value + NormalizePaste( input.Text ?? string.Empty );
		else if ( WidgetInputKind.Key == input.Kind && WidgetKey.Backspace == input.KeyCode ) candidate = RemoveLastTextElement( this.value );
		if ( candidate is null ) return false;
		candidate = TakeTextElements( candidate, this.MaxTextElements );
		if ( StringComparer.Ordinal.Equals( candidate, this.value ) ) return true;
		this.value = candidate;
		host.Enqueue( new WidgetValueChangedAction<string>( this.Id, this.value ) );
		return true;
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		string display = CursesText.TruncateToColumns( this.value, area.Columns, screen.TextWidthProvider );
		screen.StandardWindow.Move( area.Row, area.Column );
		screen.StandardWindow.Write( display, !this.IsEnabled ? theme.Disabled : focused ? theme.Focused : theme.Normal );
	}

	private static string RemoveLastTextElement( string text ) {
		int[] starts = StringInfo.ParseCombiningCharacters( text );
		return 0 == starts.Length ? text : text[ ..starts[ ^1 ] ];
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

