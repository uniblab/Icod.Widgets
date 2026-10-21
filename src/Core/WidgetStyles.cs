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

/// <summary>Names a finite widget part or interaction state without replacing the six legacy theme roles.</summary>
public enum WidgetStyleRole {
	Surface, Text, Focused, Disabled, Selected, Error, Border,
	TabStrip, TabActive, TabInactive, TabModified, TabClose, TabOverflow,
	EditorSurface, EditorText, TextSelection, TextSelectionInactive,
	FieldSurface, FieldText, FieldSelection,
	PopupSurface, PopupBorder, PopupItem, PopupFocused, PopupDisabled, PopupShortcut, PopupCheck,
	ListSurface, ListItem, ListSelected, ListFocused, ListDisabled,
	DialogSurface, DialogTitle, DialogBorder, DialogAction, DialogDefaultAction, DialogFocusedAction, DialogDisabledAction,
	ScrollTrack, ScrollThumb, ScrollFocusedThumb,
	StatusSurface, StatusField, StatusAction, StatusFocusedAction,
	Indicator, MessageSurface, Button, ButtonFocused, ButtonDisabled, CheckBox, Radio,
	PickerSurface, PickerPath, PickerError,
	MenuBarSurface, MenuBarItem, MenuBarActive, MenuBarDisabled, MenuBarMnemonic, MenuBarActiveMnemonic, MenuBarHover,
	PopupMnemonic, PopupFocusedMnemonic, PopupHover, ModalBackdrop,
	EditorCurrentLine, EditorGutter, EditorGutterCurrentLine,
	ScrollButton, ScrollHover, ScrollPressed, ScrollCorner
}

/// <summary>An immutable, value-equal set of optional styles for widget parts.</summary>
public sealed class WidgetStyles : IEquatable<WidgetStyles> {
	private readonly CursesStyle?[] values;
	private WidgetStyles( CursesStyle?[] values ) => this.values = values;
	public static WidgetStyles Empty { get; } = new( new CursesStyle?[ Enum.GetValues<WidgetStyleRole>().Length ] );

	public WidgetStyles With( WidgetStyleRole role, CursesStyle style ) {
		int index = Index( role ); CursesStyle?[] copy = (CursesStyle?[])this.values.Clone(); copy[ index ] = style;
		return new WidgetStyles( copy );
	}
	public WidgetStyles Without( WidgetStyleRole role ) {
		int index = Index( role ); if ( !this.values[ index ].HasValue ) return this;
		CursesStyle?[] copy = (CursesStyle?[])this.values.Clone(); copy[ index ] = null; return new WidgetStyles( copy );
	}
	public bool TryGetValue( WidgetStyleRole role, out CursesStyle style ) {
		CursesStyle? value = this.values[ Index( role ) ]; style = value.GetValueOrDefault(); return value.HasValue;
	}
	public bool Equals( WidgetStyles? other ) => ReferenceEquals( this, other ) || other is not null && this.values.AsSpan().SequenceEqual( other.values );
	public override bool Equals( object? obj ) => obj is WidgetStyles styles && this.Equals( styles );
	public override int GetHashCode() { HashCode hash = new(); foreach ( CursesStyle? value in this.values ) hash.Add( value ); return hash.ToHashCode(); }
	private static int Index( WidgetStyleRole role ) => Enum.IsDefined( role ) ? (int)role : throw new ArgumentOutOfRangeException( nameof( role ) );
}
