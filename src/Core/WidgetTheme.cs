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

/// <summary>Defines immutable visual roles shared by widgets.</summary>
public sealed record WidgetTheme(
	CursesStyle Normal,
	CursesStyle Focused,
	CursesStyle Disabled,
	CursesStyle Selected,
	CursesStyle Error,
	CursesStyle Border ) {
	/// <summary>Gets a terminal-default, attribute-based theme.</summary>
	public static WidgetTheme Default { get; } = new(
		CursesStyle.Default,
		CursesStyle.Default.WithAttributes( CursesTextAttributes.Reverse ),
		CursesStyle.Default.WithAttributes( CursesTextAttributes.Dim ),
		CursesStyle.Default.WithAttributes( CursesTextAttributes.Reverse ),
		CursesStyle.Default.WithAttributes( CursesTextAttributes.Bold ),
		CursesStyle.Default );

	/// <summary>Gets optional immutable part styles. Unspecified parts fall back to the six established roles.</summary>
	public WidgetStyles Parts { get; init; } = WidgetStyles.Empty;

	/// <summary>Gets whether pointer movement highlights menu headings and rows. It never changes keyboard selection or activates a command.</summary>
	public bool HighlightMenuHover { get; init; }

	/// <summary>Resolves a complete foreground, background and attribute style for a widget part.</summary>
	public CursesStyle Resolve( WidgetStyleRole role ) {
		if ( this.Parts.TryGetValue( role, out CursesStyle style ) ) return style;
		return role switch {
			WidgetStyleRole.ScrollButton => this.Resolve( WidgetStyleRole.ScrollTrack ),
			WidgetStyleRole.ScrollHover or WidgetStyleRole.ScrollPressed => this.Resolve( WidgetStyleRole.ScrollFocusedThumb ),
			WidgetStyleRole.ScrollCorner => this.Resolve( WidgetStyleRole.EditorSurface ),
			WidgetStyleRole.Focused or WidgetStyleRole.PopupFocused or WidgetStyleRole.ListFocused
				or WidgetStyleRole.MenuBarActive or WidgetStyleRole.MenuBarHover or WidgetStyleRole.PopupHover
				or WidgetStyleRole.DialogFocusedAction or WidgetStyleRole.ButtonFocused or WidgetStyleRole.StatusFocusedAction => this.Focused,
			WidgetStyleRole.Disabled or WidgetStyleRole.PopupDisabled or WidgetStyleRole.ListDisabled
				or WidgetStyleRole.MenuBarDisabled or WidgetStyleRole.DialogDisabledAction or WidgetStyleRole.ButtonDisabled => this.Disabled,
			WidgetStyleRole.Selected or WidgetStyleRole.TextSelection or WidgetStyleRole.TextSelectionInactive
				or WidgetStyleRole.FieldSelection or WidgetStyleRole.ListSelected => this.Selected,
			WidgetStyleRole.Error or WidgetStyleRole.PickerError => this.Error,
			WidgetStyleRole.Border or WidgetStyleRole.PopupBorder or WidgetStyleRole.DialogBorder or WidgetStyleRole.DialogTitle => this.Border,
			_ when Enum.IsDefined( role ) => this.Normal,
			_ => throw new ArgumentOutOfRangeException( nameof( role ) )
		};
	}
}
