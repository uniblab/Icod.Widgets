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
return host.Process( WidgetInput.Key( WidgetKey.Character, character: new( 'r' ) ), TimeSpan.FromMilliseconds( 1 ) ).Actions.Single() is WidgetInvokedAction ? 0 : 1;
