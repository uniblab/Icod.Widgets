/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
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
