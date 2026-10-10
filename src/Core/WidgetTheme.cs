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
}
