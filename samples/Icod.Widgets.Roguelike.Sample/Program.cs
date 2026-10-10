/*
	Icod.Widgets.Roguelike.Sample
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Roguelike.Sample;
using Icod.DCurses;
using Icod.Widgets;

internal static class Program {
	private static int Main( string[] args ) {
		if ( !args.Contains( "--smoke", StringComparer.Ordinal ) ) { Console.WriteLine( "Roguelike UI sample. Use --smoke." ); return 0; }
		using WidgetHost host = new( new CursesScreen( 60, 15 ), WidgetTheme.Default ); WidgetActionList actions = new( "actions" ); actions.SetItems( [ new( "drink", "Drink potion" ), new( "drop", "Drop item" ) ] ); host.Attach( actions ); host.Arrange( host.Screen.Bounds ); host.Focus( actions );
		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ); if ( result.Actions.OfType<WidgetInvokedAction>().SingleOrDefault()?.CommandId != "drink" ) return 1;
		WidgetHelpOverlay help = new( "help", "Commands", [ new( "inventory", "i", "Inventory" ), new( "quit", "Q", "Quit" ) ] ); host.ShowModal( help ); host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 1 ) ); if ( !ReferenceEquals( actions, host.FocusedWidget ) ) return 2;
		Console.WriteLine( "PASS inventory action reached caller once; help restored focus" ); return 0;
	}
}
