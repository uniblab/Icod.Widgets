/*
	Icod.Widgets.Gallery.Sample
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Gallery.Sample;

using System.Diagnostics;
using Icod.DCurses;
using Icod.Widgets;

internal static class Program {
	private static async Task<int> Main( string[] args ) => args.Contains( "--smoke", StringComparer.Ordinal ) ? RunSmoke() : await RunInteractiveAsync();

	private static int RunSmoke() {
		using WidgetHost host = new( new CursesScreen( 60, 8 ), WidgetTheme.Default );
		WidgetStack form = new( "gallery", WidgetOrientation.Vertical ); WidgetTextInput name = new( "name" ); WidgetCheckBox enabled = new( "enabled", "Enabled" ); WidgetButton accept = new( "accept", "Accept", "gallery.accept" );
		form.Add( new WidgetLabel( "title", "Icod.Widgets form gallery" ) ); form.Add( name ); form.Add( enabled ); form.Add( accept );
		host.Attach( form ); host.Arrange( host.Screen.Bounds ); host.Focus( name ); host.Process( WidgetInput.Paste( "Ada\tLovelace" ), TimeSpan.Zero ); host.Focus( accept );
		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) );
		if ( "Ada Lovelace" != name.Value || result.Actions.SingleOrDefault() is not WidgetInvokedAction ) return 1;
		host.Render(); Console.WriteLine( "PASS forms, focus, actions, and compact retained rendering" ); return 0;
	}

	private static async Task<int> RunInteractiveAsync() {
		await using CursesSession session = await CursesSession.OpenAsync(); using WidgetHost host = new( session, WidgetTheme.Default ); Stopwatch clock = Stopwatch.StartNew();
		WidgetStack gallery = BuildGallery(); host.Attach( gallery ); host.Arrange( session.Screen.Bounds ); _ = host.Focus( gallery.Descendants().First() );
		bool running = true;
		while ( running ) {
			session.StandardScreen.Clear(); host.Render(); await session.RefreshAsync();
			CursesEvent terminalEvent = await session.ReadEventAsync(); if ( terminalEvent.RequiresRepaint ) { _ = session.SynchronizeDimensions(); host.Arrange( session.Screen.Bounds ); }
			if ( terminalEvent.Input is not { } input ) continue;
			if ( IsQuit( input ) ) break;
			int modalDepth = host.ModalDepth; WidgetDispatchResult result = host.Process( input, clock.Elapsed );
			if ( 0 == modalDepth && IsEscape( input ) && !result.Handled ) break;
			foreach ( WidgetAction action in result.Actions ) {
				if ( action is WidgetInvokedAction { CommandId: "gallery.dialog" } ) _ = host.ShowModal( WidgetMessageDialog.YesNo( "gallery.question", "Question", "Continue exercising the gallery?" ) );
				else if ( action is WidgetInvokedAction { CommandId: "gallery.wizard" } ) _ = host.ShowModal( new WidgetWizardDialog( "gallery.wizard-dialog", "Wizard", [ "Previous/Next page one", "Finish page two" ] ) );
				else if ( action is WidgetWizardNavigationRequestedAction navigation && host.FocusedWidget is WidgetWizardDialog wizard ) wizard.ApplyNavigation( navigation.ToPage );
				else if ( action is WidgetInvokedAction { CommandId: "gallery.quit" } ) running = false;
			}
		}
		return 0;
	}

	private static WidgetStack BuildGallery() {
		WidgetStack root = new( "gallery", WidgetOrientation.Vertical );
		root.Add( new WidgetLabel( "instructions", "Tab/Shift+Tab focus | arrows navigate | Enter/Space act | Ctrl+Q/Escape exit" ) );
		root.Add( new WidgetMenuBar( "menu", [ WidgetMenuItem.Command( "dialog", "Dialog", "gallery.dialog" ), WidgetMenuItem.Command( "wizard", "Wizard", "gallery.wizard" ), WidgetMenuItem.Command( "quit", "Quit", "gallery.quit" ) ] ) );
		WidgetGrid buttons = new( "buttons", 3 ); buttons.Add( new WidgetButton( "dialog-button", "Yes/No", "gallery.dialog" ) ); buttons.Add( new WidgetButton( "wizard-button", "Wizard", "gallery.wizard" ) ); buttons.Add( new WidgetButton( "quit-button", "Quit", "gallery.quit" ) ); root.Add( new WidgetBorder( "button-border", buttons ) );
		root.Add( new WidgetTextInput( "text", "Unicode: résumé 界" ) ); root.Add( new WidgetCheckBox( "check", "Check box" ) ); root.Add( new WidgetRadioGroup( "radio", [ "easy", "normal", "hard" ], "normal" ) );
		WidgetList list = new( "list", WidgetSelectionMode.Multiple ); list.SetItems( [ new( "one", "One" ), new( "two", "Two" ), new( "three", "Three" ) ] ); root.Add( list );
		root.Add( new WidgetDropDown( "drop-down", [ new( "red", "Red" ), new( "green", "Green" ), new( "blue", "Blue" ) ], "green" ) );
		root.Add( new WidgetScrollIndicator( "scroll" ) { Total = 100, Offset = 25, Visible = 20 } ); return root;
	}

	private static bool IsQuit( CursesInputEvent input ) => input.Character is { Value: 81 or 113 } && 0 != ( input.Modifiers & CursesKeyModifiers.Control );
	private static bool IsEscape( CursesInputEvent input ) => CursesInputEventKind.Key == input.Kind && CursesKey.Escape == input.Key;

	private static IEnumerable<Widget> Descendants( this WidgetContainer container ) { foreach ( Widget child in container.Children ) { if ( child.IsFocusable ) yield return child; if ( child is WidgetContainer nested ) foreach ( Widget descendant in nested.Descendants() ) yield return descendant; } }
}
