/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/

namespace Icod.Widgets;

/// <summary>Returns whether input was handled and a detached action snapshot.</summary>
public sealed record WidgetDispatchResult( bool Handled, IReadOnlyList<WidgetAction> Actions );

