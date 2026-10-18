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

namespace Icod.Widgets.Tests.Menus;

using Icod.DCurses;
using Xunit;

public sealed class WidgetMenuRoutingTests {
	[Fact]
	public void FocusedEditorReceivesEditCommandExactlyOnce() {
		using WidgetHost host = new( new CursesScreen( 30, 8 ), WidgetTheme.Default );
		WidgetTextDocument document = new( [ "abc".AsMemory() ] ); WidgetTextEditor editor = new( "editor", new WidgetTextViewState( document ) );
		WidgetMenuBar menu = new( "menu", [ WidgetMenuItem.Command( "select", "Select", "app.select", focusedEditCommand: WidgetEditCommand.SelectAll,
			shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'A' ) ) ] );
		host.Attach( editor ); host.Attach( menu ); editor.Arrange( host.Screen.Bounds ); menu.Arrange( host.Screen.Bounds ); host.Focus( editor );
		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new System.Text.Rune( 'a' ) ), TimeSpan.Zero );
		Assert.True( editor.View.HasSelection ); Assert.Empty( result.Actions.OfType<WidgetInvokedAction>() );
		Assert.Single( result.Actions.OfType<WidgetTextSelectionChangedAction>() );
	}

	[Fact]
	public void DisabledEditTargetDoesNotFallThroughAndNoTargetInvokesApplication() {
		using WidgetHost host = new( new CursesScreen( 30, 8 ), WidgetTheme.Default );
		WidgetTextEditor editor = new( "editor", new WidgetTextViewState( new WidgetTextDocument( [ "abc".AsMemory() ] ) ) ) { IsReadOnly = true };
		WidgetMenuBar menu = new( "menu", [ WidgetMenuItem.Command( "paste", "Paste", "app.paste", focusedEditCommand: WidgetEditCommand.Paste,
			shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'V' ) ) ] );
		host.Attach( editor ); host.Attach( menu ); editor.Arrange( host.Screen.Bounds ); menu.Arrange( host.Screen.Bounds ); host.Focus( editor );
		WidgetInput shortcut = WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new System.Text.Rune( 'v' ) );
		Assert.Empty( host.Process( shortcut, TimeSpan.Zero ).Actions );
		host.Focus( menu ); WidgetDispatchResult fallback = host.Process( shortcut, TimeSpan.FromMilliseconds( 1 ) );
		Assert.Equal( "app.paste", Assert.IsType<WidgetInvokedAction>( Assert.Single( fallback.Actions ) ).CommandId );
	}

	[Fact]
	public void PointerAndControlShortcutUseSameFocusedPasteRoute() {
		using WidgetHost host = new( new CursesScreen( 30, 8 ), WidgetTheme.Default );
		WidgetTextDocument document = new( [ "abc".AsMemory() ] ); WidgetTextEditor editor = new( "editor", new WidgetTextViewState( document ) );
		WidgetMenuBar menu = new( "menu", [ WidgetMenuItem.Submenu( "edit", "Edit", [
			WidgetMenuItem.Command( "paste", "Paste", "app.paste", focusedEditCommand: WidgetEditCommand.Paste,
				shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'V' ) )
		], 'e' ) ] );
		host.Attach( editor ); host.Attach( menu ); editor.Arrange( host.Screen.Bounds ); menu.Arrange( host.Screen.Bounds ); Assert.True( host.Focus( editor ) );

		WidgetClipboardRequest shortcut = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single( host.Process(
			WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new System.Text.Rune( 'v' ) ), TimeSpan.Zero ).Actions ) ).Request;
		Assert.Empty( host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press, CursesMouseButton.Primary, 1, 0 ) ), TimeSpan.FromMilliseconds( 1 ) ).Actions );
		Assert.Empty( host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Release, CursesMouseButton.Primary, 1, 0 ) ), TimeSpan.FromMilliseconds( 2 ) ).Actions );
		host.Render(); int column = menu.RenderBounds.Column + 1; int row = menu.RenderBounds.Row + 1;
		Assert.Empty( host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press, CursesMouseButton.Primary, column, row ) ), TimeSpan.FromMilliseconds( 3 ) ).Actions );
		WidgetClipboardRequest pointer = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single( host.Process(
			WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Release, CursesMouseButton.Primary, column, row ) ), TimeSpan.FromMilliseconds( 4 ) ).Actions ) ).Request;

		Assert.Equal( ( shortcut.Operation, shortcut.ViewId, shortcut.DocumentId, shortcut.SelectionStart, shortcut.SelectionEnd ),
			( pointer.Operation, pointer.ViewId, pointer.DocumentId, pointer.SelectionStart, pointer.SelectionEnd ) );
		Assert.Equal( WidgetClipboardOperation.Paste, pointer.Operation ); Assert.Same( editor, host.FocusedWidget );
	}

	[Fact]
	public void PointerReleaseOutsideCannotActivateItem() {
		using WidgetHost host = new( new CursesScreen( 30, 8 ), WidgetTheme.Default );
		WidgetPopupMenu menu = new( "menu", [ WidgetMenuItem.Command( "a", "A", "a" ), WidgetMenuItem.Command( "b", "B", "b" ) ] );
		host.Attach( menu ); menu.Arrange( new CursesRectangle( 0, 0, 5, 12 ) ); host.Focus( menu ); menu.Open(); host.Render();
		host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press, CursesMouseButton.Primary, 2, 1 ) ), TimeSpan.Zero );
		WidgetDispatchResult outside = host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Release, CursesMouseButton.Primary, 20, 7 ) ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Empty( outside.Actions );
		WidgetDispatchResult insidePress = host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press, CursesMouseButton.Primary, 2, 2 ) ), TimeSpan.FromMilliseconds( 2 ) );
		WidgetDispatchResult insideRelease = host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Release, CursesMouseButton.Primary, 2, 2 ) ), TimeSpan.FromMilliseconds( 3 ) );
		Assert.Empty( insidePress.Actions ); Assert.Equal( "b", Assert.IsType<WidgetInvokedAction>( Assert.Single( insideRelease.Actions ) ).CommandId );
	}
}
