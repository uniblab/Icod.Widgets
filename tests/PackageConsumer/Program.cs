/*
	PackageConsumer
	Package-only acceptance consumer for Icod.Widgets.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using Icod.DCurses;
using Icod.Widgets;
using WidgetHost host = new( new CursesScreen( 24, 5 ), WidgetTheme.Default );
WidgetMenuBar menu = new( "menu", [ WidgetMenuItem.Submenu( "file", "File", [ WidgetMenuItem.Command( "run", "Run", "run", 'r' ) ], 'f' ) ] );
host.Attach( menu ); host.Arrange( host.Screen.Bounds ); host.Focus( menu );
host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new( 'f' ) ), TimeSpan.Zero );
WidgetDispatchResult menuResult = host.Process( WidgetInput.Key( WidgetKey.Character, character: new( 'r' ) ), TimeSpan.FromMilliseconds( 1 ) );
WidgetMessageDialog notice = WidgetMessageDialog.Ok( "notice", "Notice", "Packaged API works." ); host.ShowModal( notice );
WidgetDispatchResult dialogResult = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 2 ) );
return menuResult.Actions.Single() is WidgetInvokedAction && dialogResult.Actions.Single() is WidgetDialogCompletedAction { Result.Outcome: WidgetDialogOutcome.Ok } ? 0 : 1;
