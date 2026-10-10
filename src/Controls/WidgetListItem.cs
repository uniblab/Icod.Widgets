/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

/// <summary>One immutable, stable-key list entry.</summary>
public sealed record WidgetListItem( string Key, string Text, bool IsEnabled = true );

