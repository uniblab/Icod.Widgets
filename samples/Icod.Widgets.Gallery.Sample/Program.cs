/*
	Icod.Widgets.Gallery.Sample
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Gallery.Sample;

using Icod.DCurses;
using Icod.Widgets;

internal static class Program {
	private static int Main( string[] args ) {
		if ( !args.Contains( "--smoke", StringComparer.Ordinal ) ) {
			Console.WriteLine( "Icod.Widgets gallery. Run with --smoke for deterministic public-API checks." );
			return 0;
		}
		using WidgetHost host = new( new CursesScreen( 60, 8 ), WidgetTheme.Default );
		WidgetStack form = new( "gallery", WidgetOrientation.Vertical );
		WidgetTextInput name = new( "name" );
		WidgetCheckBox enabled = new( "enabled", "Enabled" );
		WidgetButton accept = new( "accept", "Accept", "gallery.accept" );
		form.Add( new WidgetLabel( "title", "Icod.Widgets form gallery" ) );
		form.Add( name ); form.Add( enabled ); form.Add( accept );
		host.Attach( form ); host.Arrange( host.Screen.Bounds );
		host.Focus( name ); host.Process( WidgetInput.Paste( "Ada\tLovelace" ), TimeSpan.Zero );
		host.Focus( accept );
		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) );
		if ( "Ada Lovelace" != name.Value || result.Actions.SingleOrDefault() is not WidgetInvokedAction ) return 1;
		host.Render();
		Console.WriteLine( "PASS forms, focus, actions, and compact retained rendering" );
		return 0;
	}
}
