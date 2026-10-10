/*
	Icod.Widgets.Roguelike.Sample
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Roguelike.Sample;

using System.Diagnostics;
using System.Globalization;
using Icod.DCurses;
using Icod.Widgets;

internal static class Program {
	private static async Task<int> Main( string[] args ) => args.Contains( "--smoke", StringComparer.Ordinal ) ? RunSmoke() : await RunInteractiveAsync();

	private static int RunSmoke() {
		using WidgetHost host = new( new CursesScreen( 60, 15 ), WidgetTheme.Default ); WidgetActionList actions = new( "actions" ); actions.SetItems( [ new( "drink", "Drink potion" ), new( "drop", "Drop item" ) ] ); host.Attach( actions ); host.Arrange( host.Screen.Bounds ); host.Focus( actions );
		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ); if ( result.Actions.OfType<WidgetInvokedAction>().SingleOrDefault()?.CommandId != "drink" ) return 1;
		WidgetHelpOverlay help = new( "help", "Commands", [ new( "inventory", "i", "Inventory" ), new( "quit", "Q", "Quit" ) ] ); host.ShowModal( help ); host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 1 ) ); if ( !ReferenceEquals( actions, host.FocusedWidget ) ) return 2;
		Console.WriteLine( "PASS inventory action reached caller once; help restored focus" ); return 0;
	}

	private static async Task<int> RunInteractiveAsync() {
		await using CursesSession session = await CursesSession.OpenAsync(); using WidgetHost host = new( session, WidgetTheme.Default ); Stopwatch clock = Stopwatch.StartNew();
		WidgetStatusBar status = new( "status" ); status.SetFields( [ new( "hp", "HP 24/30", 100 ), new( "level", "Depth 3", 50 ), new( "gold", "Gold 17", 25 ) ] );
		WidgetInventoryList inventory = new( "inventory" ); inventory.SetItems( [ new( "potion", "Healing potion" ), new( "lamp", "Brass lamp" ), new( "scroll", "Scroll of mapping" ) ] );
		WidgetActionList actions = new( "actions" ); actions.SetItems( [ new( "use", "Use selected item" ), new( "drop", "Drop selected item" ), new( "rest", "Rest" ) ] );
		WidgetMessageHistory history = new( "messages", 50 ); history.Append( "Welcome. Tab between inventory and actions; press ? for help." );
		WidgetStack lists = new( "lists", WidgetOrientation.Horizontal ); lists.Add( new WidgetBorder( "inventory-border", inventory ) ); lists.Add( new WidgetBorder( "actions-border", actions ) );
		WidgetStack root = new( "rogue", WidgetOrientation.Vertical ); root.Add( status ); root.Add( lists ); root.Add( new WidgetBorder( "history-border", history ) ); host.Attach( root ); host.Arrange( session.Screen.Bounds ); _ = host.Focus( inventory );
		bool running = true;
		while ( running ) {
			session.StandardScreen.Clear(); host.Render(); await session.RefreshAsync();
			CursesEvent terminalEvent = await session.ReadEventAsync(); if ( terminalEvent.RequiresRepaint ) { _ = session.SynchronizeDimensions(); host.Arrange( session.Screen.Bounds ); }
			if ( terminalEvent.Input is not { } input ) continue;
			if ( IsQuit( input ) ) break;
			if ( 0 == host.ModalDepth && input.Character is { Value: 63 } ) { _ = host.ShowModal( new WidgetHelpOverlay( "help", "Commands", [ new( "focus", "Tab", "Change panel" ), new( "act", "Enter", "Invoke action" ), new( "text", "T", "Name prompt" ), new( "number", "N", "Number prompt" ), new( "quit", "Q", "Quit" ) ] ) ); continue; }
			if ( 0 == host.ModalDepth && input.Character is { Value: 84 or 116 } ) { _ = host.ShowModal( new WidgetTextPrompt( "text-prompt", "Character name", "Ada" ) ); continue; }
			if ( 0 == host.ModalDepth && input.Character is { Value: 78 or 110 } ) { _ = host.ShowModal( new WidgetNumberPrompt( "number-prompt", "How many?", CultureInfo.CurrentCulture, 1, 99, "1" ) ); continue; }
			int modalDepth = host.ModalDepth; WidgetDispatchResult result = host.Process( input, clock.Elapsed ); if ( 0 == modalDepth && IsEscape( input ) && !result.Handled ) break;
			foreach ( WidgetAction action in result.Actions ) {
				if ( action is WidgetInvokedAction invoked ) history.Append( $"Command: {invoked.CommandId}" );
				else if ( action is WidgetSelectionChangedAction selection ) history.Append( $"Selected: {string.Join( ", ", selection.Keys )}" );
				else if ( action is WidgetDialogCompletedAction { Result.Outcome: WidgetDialogOutcome.Accepted } completed ) history.Append( $"Prompt result: {completed.Result.Value}" );
			}
		}
		return 0;
	}

	private static bool IsQuit( CursesInputEvent input ) => input.Character is { Value: 81 or 113 } && 0 != ( input.Modifiers & CursesKeyModifiers.Control );
	private static bool IsEscape( CursesInputEvent input ) => CursesInputEventKind.Key == input.Kind && CursesKey.Escape == input.Key;
}
