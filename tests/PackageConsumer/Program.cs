/*
	Icod.Widgets package-only qualification consumer.
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
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
