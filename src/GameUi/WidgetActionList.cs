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

/// <summary>Invokes the selected caller-supplied action key.</summary>
public sealed class WidgetActionList( string id ) : WidgetList( id, WidgetSelectionMode.Single ) {
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		bool invoke = WidgetInputKind.Key == input.Kind && WidgetKey.Enter == input.KeyCode && this.CursorKey is not null;
		if ( invoke ) {
			bool selectionChanges = 1 != this.SelectedKeys.Count || !StringComparer.Ordinal.Equals( this.CursorKey, this.SelectedKeys[ 0 ] );
			host.EnsureActionCapacity( selectionChanges ? 2 : 1 );
		}
		bool handled = base.OnInput( input, host );
		if ( invoke ) { host.Enqueue( new WidgetInvokedAction( this.Id, this.CursorKey! ) ); return true; }
		return handled;
	}
}

