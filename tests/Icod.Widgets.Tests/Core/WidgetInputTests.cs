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

namespace Icod.Widgets.Tests.Core;

using System.Text;
using Icod.DCurses;
using Xunit;

public sealed class WidgetInputTests {
	[Fact]
	public void RichFactoriesPreserveMetadata() {
		WidgetInput function = WidgetInput.FunctionKey( 6, WidgetInputModifiers.Control );
		WidgetInput pasteBegin = WidgetInput.PasteChunk( CursesPastePhase.Begin );
		WidgetInput paste = WidgetInput.PasteChunk( CursesPastePhase.Data, "a\nb" );
		WidgetInput pasteEnd = WidgetInput.PasteChunk( CursesPastePhase.End );
		WidgetInput focused = WidgetInput.FocusChanged( CursesFocusState.Focused );
		WidgetInput unfocused = WidgetInput.FocusChanged( CursesFocusState.Unfocused );

		Assert.Equal( WidgetKey.Function, function.KeyCode );
		Assert.Equal( 6, function.FunctionKeyNumber );
		Assert.Equal( CursesKeyEventPhase.Press, function.KeyPhase );
		Assert.Equal( CursesKeyModifiers.Control, function.SourceModifiers );
		Assert.Equal( CursesPastePhase.Begin, pasteBegin.PastePhase );
		Assert.Equal( CursesPastePhase.Data, paste.PastePhase );
		Assert.Equal( "a\nb", paste.Text );
		Assert.Equal( CursesPastePhase.End, pasteEnd.PastePhase );
		Assert.Equal( CursesFocusState.Focused, focused.FocusState );
		Assert.Equal( CursesFocusState.Unfocused, unfocused.FocusState );
		Assert.NotEqual( focused, unfocused );
	}

	[Fact]
	public void LegacyFactoriesKeepTheirMeaning() {
		WidgetInput key = WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new Rune( 'x' ) );
		WidgetInput paste = WidgetInput.Paste( "one\ntwo" );

		Assert.Equal( CursesKeyEventPhase.Press, key.KeyPhase );
		Assert.Equal( CursesKeyModifiers.Alt, key.SourceModifiers );
		Assert.Null( key.FunctionKeyNumber );
		Assert.Null( paste.PastePhase );
		Assert.Equal( "one\ntwo", paste.Text );
	}

	[Fact]
	public void FunctionShortcutsRemainDistinct() {
		WidgetMenuShortcut f6 = new( WidgetKey.Function ) { FunctionKeyNumber = 6 };
		WidgetMenuShortcut f10 = new( WidgetKey.Function ) { FunctionKeyNumber = 10 };
		WidgetMenuBar menu = new( "menu", [
			WidgetMenuItem.Command( "focus", "Focus", "focus", shortcut: f6 ),
			WidgetMenuItem.Command( "main-menu", "Main menu", "main-menu", shortcut: f10 )
		] );
		using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default );
		host.Attach( menu ); menu.Arrange( new CursesRectangle( 0, 0, 1, 40 ) );

		WidgetInvokedAction action = Assert.IsType<WidgetInvokedAction>( Assert.Single(
			host.Process( WidgetInput.FunctionKey( 10 ), TimeSpan.Zero ).Actions ) );

		Assert.Equal( "main-menu", action.CommandId );
	}
}
