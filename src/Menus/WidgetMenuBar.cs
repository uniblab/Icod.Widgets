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

/// <summary>A menu-bar presentation sharing popup menu semantics.</summary>
public sealed class WidgetMenuBar( string id, IReadOnlyList<WidgetMenuItem> items ) : WidgetPopupMenu( id, items ) {
	internal bool TryGlobalInput( WidgetInput input, WidgetHost host ) => this.TryRootMnemonic( input, host ) || this.TryShortcut( input, host );
}
