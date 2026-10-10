/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Tests.GameUi;

using System.Globalization;
using Icod.DCurses;
using Xunit;

public sealed class WidgetGameUiTests {
	[Fact]
	public void InventoryHasNoGameModelDependency() {
		WidgetInventoryList inventory = new( "inventory" ); inventory.SetItems( [ new WidgetListItem( "potion", "Potion" ) ] );
		Assert.Equal( "potion", inventory.CursorKey ); Assert.DoesNotContain( typeof( WidgetInventoryList ).Assembly.GetReferencedAssemblies(), name => name.Name?.Contains( "Game", StringComparison.OrdinalIgnoreCase ) == true );
	}

	[Fact]
	public void HistoryDropsOldestAt1001() {
		WidgetMessageHistory history = new( "messages" ); for ( int i = 0; i < 1001; i++ ) history.Append( i.ToString( CultureInfo.InvariantCulture ) );
		Assert.Equal( 1000, history.Messages.Count ); Assert.Equal( "1", history.Messages[ 0 ] );
	}

	[Fact]
	public void StatusFitsCompactScreen() {
		WidgetStatusBar status = new( "status" ); status.SetFields( [ new( "hp", "HP 10", 100 ), new( "mana", "Mana 5", 50 ) ] );
		using WidgetHost host = new( new CursesScreen( 3, 1 ), WidgetTheme.Default ); host.Attach( status ); host.Arrange( host.Screen.Bounds ); host.Render();
		Assert.Equal( 3, host.Screen.Columns );
	}

	[Fact]
	public void HelpReflectsEffectiveBindings() {
		WidgetHelpOverlay help = new( "help", "Help", [ new( "inventory", "i", "Open inventory" ), new( "quit", "Q", "Quit" ) ] );
		Assert.Equal( "i", help.Bindings.Single( binding => binding.CommandId == "inventory" ).Gesture );
	}

	[Fact]
	public void NumberValidationUsesCallerCultureAndRange() {
		WidgetNumberPrompt prompt = new( "number", "Amount", CultureInfo.GetCultureInfo( "fr-FR" ), 1.0, 2.0 ); using WidgetHost host = new( new CursesScreen( 30, 5 ), WidgetTheme.Default ); host.ShowModal( prompt );
		host.Process( WidgetInput.Paste( "1,5" ), TimeSpan.Zero ); WidgetDialogCompletedAction action = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) );
		Assert.Equal( 1.5, Assert.IsType<double>( action.Result.Value ) );
	}
}
