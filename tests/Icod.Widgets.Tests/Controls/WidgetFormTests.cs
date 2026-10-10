/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Tests.Controls;

using System.Text;
using Icod.DCurses;
using Xunit;

public sealed class WidgetFormTests {
	[Fact]
	public void UnicodeEditsWholeTextElements() {
		WidgetTextInput input = new( "name", "a\u0301界" );
		using WidgetHost host = FocusedHost( input );

		host.Process( WidgetInput.Key( WidgetKey.Backspace ), TimeSpan.Zero );
		host.Process( WidgetInput.Key( WidgetKey.Backspace ), TimeSpan.FromMilliseconds( 1 ) );

		Assert.Equal( "", input.Value );
	}

	[Fact]
	public void PasteNormalizesControls() {
		WidgetTextInput input = new( "name" );
		using WidgetHost host = FocusedHost( input );

		host.Process( WidgetInput.Paste( "one\r\n\t two\u0001three" ), TimeSpan.Zero );

		Assert.Equal( "one two three", input.Value );
	}

	[Fact]
	public void PastePreservesBoundarySeparator() {
		WidgetTextInput input = new( "input", "one" ); using WidgetHost host = FocusedHost( input );
		host.Process( WidgetInput.Paste( "\ntwo" ), TimeSpan.Zero );
		Assert.Equal( "one two", input.Value );
	}

	[Fact]
	public void CaretEditsWholeTextElementsAndKeepsTailVisible() {
		WidgetTextInput input = new( "input", "a\u0301bc" ); using WidgetHost host = FocusedHost( input, columns: 2 );
		host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.Zero );
		host.Process( WidgetInput.Key( WidgetKey.Backspace ), TimeSpan.FromMilliseconds( 1 ) );
		host.Process( WidgetInput.TextInput( new Rune( 'Z' ) ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( "a\u0301Zc", input.Value );
		host.Process( WidgetInput.Key( WidgetKey.End ), TimeSpan.FromMilliseconds( 3 ) );
		host.Render();
		Assert.Equal( "Zc", string.Concat( Enumerable.Range( 0, 2 ).Select( column => host.Screen.StandardWindow.GetCell( 0, column ).Content ) ) );
	}

	[Fact]
	public void LimitDoesNotSplitTextElement() {
		WidgetTextInput input = new( "name" ) { MaxTextElements = 1 };
		using WidgetHost host = FocusedHost( input );
		host.Process( WidgetInput.Paste( "a\u0301b" ), TimeSpan.Zero );
		Assert.Equal( "a\u0301", input.Value );
	}

	[Fact]
	public void DisabledButtonCannotInvoke() {
		WidgetButton button = new( "save", "Save", "file.save" ) { IsEnabled = false };
		using WidgetHost host = FocusedHost( button, focus: false );
		Assert.Empty( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions );
	}

	[Fact]
	public void PointerReleaseElsewhereDoesNotInvoke() {
		WidgetButton button = new( "save", "Save", "file.save" );
		WidgetButton other = new( "other", "Other", "other" );
		WidgetStack stack = new( "root", WidgetOrientation.Horizontal );
		stack.Add( button ); stack.Add( other );
		using WidgetHost host = new( new CursesScreen( 20, 2 ), WidgetTheme.Default );
		host.Attach( stack ); host.Arrange( new CursesRectangle( 0, 0, 1, 20 ) );

		host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press, CursesMouseButton.Primary, 1, 0 ) ), TimeSpan.Zero );
		WidgetDispatchResult result = host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Release, CursesMouseButton.Primary, 15, 0 ) ), TimeSpan.FromMilliseconds( 1 ) );

		Assert.Empty( result.Actions );
		WidgetDispatchResult laterRelease = host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Release, CursesMouseButton.Primary, 1, 0 ) ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Empty( laterRelease.Actions );
	}

	[Fact]
	public void RadioSelectionMovesWithinGroup() {
		WidgetRadioGroup group = new( "difficulty", [ "easy", "normal", "hard" ], "easy" );
		using WidgetHost host = FocusedHost( group );

		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.Zero );

		Assert.Equal( "normal", group.SelectedKey );
		Assert.Equal( "normal", Assert.IsType<WidgetValueChangedAction<string>>( Assert.Single( result.Actions ) ).Value );
	}

	private static WidgetHost FocusedHost( Widget widget, bool focus = true, int columns = 40 ) {
		WidgetHost host = new( new CursesScreen( columns, 5 ), WidgetTheme.Default );
		host.Attach( widget ); host.Arrange( new CursesRectangle( 0, 0, 1, columns ) );
		if ( focus ) Assert.True( host.Focus( widget ) );
		return host;
	}
}
