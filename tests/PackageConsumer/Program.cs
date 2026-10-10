/*
	Icod.Widgets package-only qualification consumer.
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
using Icod.DCurses;
using Icod.Widgets;
using WidgetHost host = new( new CursesScreen( 20, 2 ), WidgetTheme.Default );
WidgetButton button = new( "ok", "OK", "ok" ); host.Attach( button ); host.Arrange( host.Screen.Bounds ); host.Focus( button );
return host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions.Single() is WidgetInvokedAction ? 0 : 1;
