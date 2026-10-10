/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/

namespace Icod.Widgets;

/// <summary>Defines bounded resource limits used by widget hosts and controls.</summary>
public static class WidgetLimits {
	public const int MaximumWidgetsPerHost = 1024;
	public const int MaximumPendingActions = 4096;
	public const int MaximumModalDepth = 16;
	public const int MaximumSubmenuDepth = 16;
	public const int MaximumItems = 100_000;
}

