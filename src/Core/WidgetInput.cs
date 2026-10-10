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

using System.Text;
using Icod.DCurses;

public enum WidgetInputKind { Key, Text, Paste, Pointer, Focus, EndOfInput }

public enum WidgetKey {
	None, Character, Enter, Space, Escape, Backspace, Tab, Up, Down, Left,
	Right, Home, End, PageUp, PageDown, Insert, Delete, Function
}

[Flags]
public enum WidgetInputModifiers { None = 0, Shift = 1, Control = 2, Alt = 4 }

/// <summary>Represents public, replayable input independent of DCurses factory visibility.</summary>
public sealed record WidgetInput {
	private WidgetInput( WidgetInputKind kind ) => this.Kind = kind;

	public WidgetInputKind Kind { get; }
	public WidgetKey KeyCode { get; init; }
	public WidgetInputModifiers Modifiers { get; init; }
	public Rune? Character { get; init; }
	public string? Text { get; init; }
	public CursesMouseEvent? Pointer { get; init; }

	public static WidgetInput Key(
		WidgetKey key,
		WidgetInputModifiers modifiers = WidgetInputModifiers.None,
		Rune? character = null ) {
		if ( !Enum.IsDefined( key ) || WidgetKey.None == key ) throw new ArgumentOutOfRangeException( nameof( key ) );
		return new WidgetInput( WidgetInputKind.Key ) { KeyCode = key, Modifiers = modifiers, Character = character };
	}

	public static WidgetInput TextInput( Rune character ) => new( WidgetInputKind.Text ) {
		KeyCode = WidgetKey.Character, Character = character, Text = character.ToString()
	};

	public static WidgetInput Paste( string text ) => new( WidgetInputKind.Paste ) {
		Text = text ?? throw new ArgumentNullException( nameof( text ) )
	};

	public static WidgetInput PointerInput( CursesMouseEvent pointer ) => new( WidgetInputKind.Pointer ) {
		Pointer = pointer ?? throw new ArgumentNullException( nameof( pointer ) )
	};

	public static WidgetInput From( CursesInputEvent input ) {
		ArgumentNullException.ThrowIfNull( input );
		WidgetInputModifiers modifiers = ConvertModifiers( input.Modifiers );
		return input.Kind switch {
			CursesInputEventKind.Text => new WidgetInput( WidgetInputKind.Text ) {
				KeyCode = WidgetKey.Character, Character = input.Character,
				Text = input.Character?.ToString(), Modifiers = modifiers },
			CursesInputEventKind.Key => new WidgetInput( WidgetInputKind.Key ) {
				KeyCode = ConvertKey( input.Key ), Character = input.Character,
				Text = input.AssociatedText, Modifiers = modifiers },
			CursesInputEventKind.Mouse => new WidgetInput( WidgetInputKind.Pointer ) {
				Pointer = input.Mouse, Modifiers = modifiers },
			CursesInputEventKind.Paste => new WidgetInput( WidgetInputKind.Paste ) {
				Text = input.Paste?.Text, Modifiers = modifiers },
			CursesInputEventKind.Focus => new WidgetInput( WidgetInputKind.Focus ),
			_ => new WidgetInput( WidgetInputKind.EndOfInput )
		};
	}

	private static WidgetInputModifiers ConvertModifiers( CursesKeyModifiers value ) {
		WidgetInputModifiers result = WidgetInputModifiers.None;
		if ( 0 != ( value & CursesKeyModifiers.Shift ) ) result |= WidgetInputModifiers.Shift;
		if ( 0 != ( value & CursesKeyModifiers.Control ) ) result |= WidgetInputModifiers.Control;
		if ( 0 != ( value & CursesKeyModifiers.Alt ) ) result |= WidgetInputModifiers.Alt;
		return result;
	}

	private static WidgetKey ConvertKey( CursesKey key ) => key switch {
		CursesKey.Character => WidgetKey.Character,
		CursesKey.Enter or CursesKey.KeypadEnter => WidgetKey.Enter,
		CursesKey.Space => WidgetKey.Space,
		CursesKey.Escape => WidgetKey.Escape,
		CursesKey.Backspace => WidgetKey.Backspace,
		CursesKey.Tab => WidgetKey.Tab,
		CursesKey.Up => WidgetKey.Up,
		CursesKey.Down => WidgetKey.Down,
		CursesKey.Left => WidgetKey.Left,
		CursesKey.Right => WidgetKey.Right,
		CursesKey.Home => WidgetKey.Home,
		CursesKey.End => WidgetKey.End,
		CursesKey.PageUp => WidgetKey.PageUp,
		CursesKey.PageDown => WidgetKey.PageDown,
		CursesKey.Insert => WidgetKey.Insert,
		CursesKey.Delete => WidgetKey.Delete,
		CursesKey.Function => WidgetKey.Function,
		_ => WidgetKey.None
	};
}

