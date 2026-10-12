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

/// <summary>Defines bounded resource limits used by widget hosts and controls.</summary>
public static class WidgetLimits {
	public const int MaximumWidgetsPerHost = 1024;
	public const int MaximumPendingActions = 4096;
	public const int MaximumModalDepth = 16;
	public const int MaximumSubmenuDepth = 16;
	public const int MaximumItems = 100_000;
	public const int MaximumTabs = 1024;
}

