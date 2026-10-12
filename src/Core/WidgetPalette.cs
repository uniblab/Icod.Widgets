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

/// <summary>Selects one of the two built-in color palettes.</summary>
public enum WidgetPaletteVariant { Dark, Light }

/// <summary>Creates a theme from DCurses' public terminal presentation observations.</summary>
public static class WidgetPalette {
	/// <summary>Returns a theme using only advertised color kinds and independently supported axes.</summary>
	public static WidgetTheme Create( CursesPresentationCapabilities capabilities, WidgetPaletteVariant variant = WidgetPaletteVariant.Dark ) {
		if ( !Enum.IsDefined( variant ) ) throw new ArgumentOutOfRangeException( nameof( variant ) );
		bool light = WidgetPaletteVariant.Light == variant;
		CursesColor Color( byte red, byte green, byte blue, int index ) => capabilities.SupportsDirectRgb
			? CursesColor.Rgb( red, green, blue )
			: capabilities.IndexedColorCount > index ? CursesColor.Indexed( index ) : CursesColor.Default;
		CursesColor background = light ? Color( 238, 238, 238, 7 ) : Color( 24, 24, 36, 0 );
		CursesColor foreground = light ? Color( 28, 31, 39, 0 ) : Color( 224, 228, 239, 7 );
		CursesColor accent = light ? Color( 24, 65, 154, 4 ) : Color( 101, 185, 238, 6 );
		CursesColor accentBackground = light ? Color( 190, 213, 248, 6 ) : Color( 34, 67, 112, 4 );
		CursesColor error = Color( 207, 52, 61, 1 );
		CursesColor dim = light ? Color( 86, 90, 98, 6 ) : Color( 153, 160, 177, 3 );
		CursesColor Axis( CursesColor color, bool supported ) => supported ? color : CursesColor.Default;
		CursesStyle Style( CursesColor ink, CursesColor paper, CursesTextAttributes attributes = CursesTextAttributes.None ) {
			CursesColor front = Axis( ink, capabilities.SupportsForegroundColor );
			CursesColor back = Axis( paper, capabilities.SupportsBackgroundColor );
			CursesTextAttributes allowed = capabilities.SupportedAttributes;
			if ( !front.IsDefault || !back.IsDefault ) allowed &= ~capabilities.ColorRestrictedAttributes;
			return new CursesStyle( front, back, attributes & allowed );
		}
		CursesTextAttributes emphasis = capabilities.SupportsBold ? CursesTextAttributes.Bold : capabilities.SupportsUnderline ? CursesTextAttributes.Underline : CursesTextAttributes.None;
		CursesTextAttributes highlight = capabilities.SupportsReverse ? CursesTextAttributes.Reverse : capabilities.SupportsStandout ? CursesTextAttributes.Standout : emphasis;
		CursesStyle normal = Style( foreground, background );
		CursesStyle focused = Style( foreground, accentBackground, highlight );
		CursesStyle selected = Style( foreground, accentBackground, highlight );
		CursesStyle disabled = Style( dim, background, capabilities.SupportsDim ? CursesTextAttributes.Dim : CursesTextAttributes.None );
		CursesStyle issue = Style( error, background, emphasis );
		CursesStyle border = Style( accent, background );
		WidgetStyles parts = WidgetStyles.Empty
			.With( WidgetStyleRole.Surface, normal ).With( WidgetStyleRole.Text, normal )
			.With( WidgetStyleRole.TabStrip, border ).With( WidgetStyleRole.TabActive, focused )
			.With( WidgetStyleRole.TabInactive, normal ).With( WidgetStyleRole.TabModified, issue )
			.With( WidgetStyleRole.TabClose, border ).With( WidgetStyleRole.TabOverflow, border )
			.With( WidgetStyleRole.EditorSurface, normal ).With( WidgetStyleRole.EditorText, normal )
			.With( WidgetStyleRole.TextSelection, selected ).With( WidgetStyleRole.TextSelectionInactive, Style( accent, background, emphasis ) )
			.With( WidgetStyleRole.FieldSurface, normal ).With( WidgetStyleRole.FieldText, normal ).With( WidgetStyleRole.FieldSelection, selected )
			.With( WidgetStyleRole.PopupSurface, normal ).With( WidgetStyleRole.PopupBorder, border )
			.With( WidgetStyleRole.PopupItem, normal ).With( WidgetStyleRole.PopupFocused, focused )
			.With( WidgetStyleRole.PopupDisabled, disabled ).With( WidgetStyleRole.PopupShortcut, border )
			.With( WidgetStyleRole.PopupCheck, border )
			.With( WidgetStyleRole.ListSurface, normal ).With( WidgetStyleRole.ListItem, normal )
			.With( WidgetStyleRole.ListSelected, selected ).With( WidgetStyleRole.ListFocused, focused ).With( WidgetStyleRole.ListDisabled, disabled )
			.With( WidgetStyleRole.DialogSurface, normal ).With( WidgetStyleRole.DialogTitle, Style( accent, background, emphasis ) )
			.With( WidgetStyleRole.DialogBorder, border ).With( WidgetStyleRole.DialogAction, normal )
			.With( WidgetStyleRole.DialogDefaultAction, selected ).With( WidgetStyleRole.DialogFocusedAction, focused )
			.With( WidgetStyleRole.DialogDisabledAction, disabled )
			.With( WidgetStyleRole.ScrollTrack, Style( dim, background ) ).With( WidgetStyleRole.ScrollThumb, border )
			.With( WidgetStyleRole.ScrollFocusedThumb, focused )
			.With( WidgetStyleRole.StatusSurface, border ).With( WidgetStyleRole.StatusField, normal )
			.With( WidgetStyleRole.StatusAction, Style( accent, background, emphasis ) ).With( WidgetStyleRole.StatusFocusedAction, focused )
			.With( WidgetStyleRole.Indicator, normal ).With( WidgetStyleRole.MessageSurface, normal )
			.With( WidgetStyleRole.Button, normal ).With( WidgetStyleRole.ButtonFocused, focused ).With( WidgetStyleRole.ButtonDisabled, disabled )
			.With( WidgetStyleRole.CheckBox, normal ).With( WidgetStyleRole.Radio, normal )
			.With( WidgetStyleRole.PickerSurface, normal ).With( WidgetStyleRole.PickerPath, border ).With( WidgetStyleRole.PickerError, issue );
		return new WidgetTheme( normal, focused, disabled, selected, issue, border ) { Parts = parts };
	}
}
