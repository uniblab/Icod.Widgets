/*
	Icod.Widgets.Tests
	Automated tests for the Icod.Widgets library.
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
	public void HelpAndTextPromptsRenderInsideCaptionedFrames() {
		using WidgetHost helpHost = new( new CursesScreen( 20, 5 ), WidgetTheme.Default ); WidgetHelpOverlay help = new( "help", "Help", [ new( "inventory", "i", "Inventory" ) ] ); helpHost.ShowModal( help ); helpHost.Render();
		CursesWindow helpWindow = helpHost.GetPresentationWindow( help ); Assert.Equal( "┌", helpWindow.GetCell( 0, 0 ).Content ); Assert.Equal( "│", helpWindow.GetCell( 1, 0 ).Content ); Assert.Equal( "i", helpWindow.GetCell( 1, 1 ).Content ); Assert.Equal( "│", helpWindow.GetCell( 1, 19 ).Content );

		using WidgetHost promptHost = new( new CursesScreen( 20, 5 ), WidgetTheme.Default ); WidgetTextPrompt prompt = new( "prompt", "Name", "Ada" ); promptHost.ShowModal( prompt ); promptHost.Render();
		CursesWindow promptWindow = promptHost.GetPresentationWindow( prompt ); Assert.Equal( "┌", promptWindow.GetCell( 0, 0 ).Content ); Assert.Equal( "│", promptWindow.GetCell( 1, 0 ).Content ); Assert.Equal( "A", promptWindow.GetCell( 1, 1 ).Content ); Assert.Equal( "│", promptWindow.GetCell( 1, 19 ).Content );
	}

	[Fact]
	public void NumberPromptErrorRemainsInsideCaptionedFrame() {
		using WidgetHost host = new( new CursesScreen( 30, 5 ), WidgetTheme.Default ); WidgetNumberPrompt prompt = new( "number", "Amount", CultureInfo.InvariantCulture, 1, 2, "9" ); host.ShowModal( prompt ); host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ); host.Render();
		CursesWindow window = host.GetPresentationWindow( prompt ); Assert.Equal( "│", window.GetCell( 2, 0 ).Content ); Assert.Equal( "E", window.GetCell( 2, 1 ).Content ); Assert.Equal( "│", window.GetCell( 2, 29 ).Content );
	}

	[Fact]
	public void NumberValidationUsesCallerCultureAndRange() {
		WidgetNumberPrompt prompt = new( "number", "Amount", CultureInfo.GetCultureInfo( "fr-FR" ), 1.0, 2.0 ); using WidgetHost host = new( new CursesScreen( 30, 5 ), WidgetTheme.Default ); host.ShowModal( prompt );
		host.Process( WidgetInput.Paste( "1,5" ), TimeSpan.Zero ); WidgetDialogCompletedAction action = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) );
		Assert.Equal( 1.5, Assert.IsType<double>( action.Result.Value ) );
	}
}
