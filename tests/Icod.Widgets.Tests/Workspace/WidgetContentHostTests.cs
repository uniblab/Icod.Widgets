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

namespace Icod.Widgets.Tests.Workspace;

using Icod.DCurses;
using Xunit;

public sealed class WidgetContentHostTests {
	[Fact]
	public void ContainerBaseAddCannotGiveContentHostMultipleChildren() {
		WidgetContentHost content = new( "content" ); WidgetContainer container = content;
		WidgetButton first = new( "first", "First", "first" ); WidgetButton second = new( "second", "Second", "second" );
		container.Add( first );
		Assert.Throws<InvalidOperationException>( () => container.Add( second ) );
		Assert.Same( first, content.Content ); Assert.Single( content.Children ); Assert.Null( second.Parent );
	}

	[Fact]
	public void ThreeViewsRetainSelectionScrollAndDocumentIdentity() {
		using WidgetHost host = new( new CursesScreen( 30, 5 ), WidgetTheme.Default ); WidgetContentHost content = new( "content" ); host.Attach( content ); host.Arrange( host.Screen.Bounds );
		WidgetTextViewState[] views = Enumerable.Range( 0, 3 ).Select( _ => new WidgetTextViewState( new WidgetTextDocument( [ "abcdef".AsMemory() ] ) ) ).ToArray();
		WidgetTextEditor[] editors = views.Select( ( view, i ) => new WidgetTextEditor( $"editor{i}", view ) ).ToArray();
		for ( int i = 0; i < 3; i++ ) {
			views[ i ].SetSelection( i, i + 1 ); views[ i ].SetScroll( i, i ); content.SetContent( editors[ i ] );
			Assert.Same( views[ i ], editors[ i ].View ); Assert.Same( editors[ i ], content.Content ); Assert.True( host.Focus( editors[ i ] ) );
		}
		content.SetContent( editors[ 0 ] ); Assert.Equal( 0, views[ 0 ].Anchor ); Assert.Equal( 1, views[ 0 ].Caret ); Assert.Equal( 0, views[ 0 ].VerticalOffset );
		Assert.Same( editors[ 0 ], host.FocusedWidget ); Assert.Equal( 2, host.AttachedCount );
	}

	[Fact]
	public void LateClipboardAndPasteCannotEditReplacement() {
		using WidgetHost host = new( new CursesScreen( 30, 5 ), WidgetTheme.Default ); WidgetContentHost content = new( "content" ); host.Attach( content ); host.Arrange( host.Screen.Bounds );
		WidgetTextEditor old = new( "old", new WidgetTextViewState( new WidgetTextDocument( [ "old".AsMemory() ] ) ) );
		WidgetTextEditor next = new( "next", new WidgetTextViewState( new WidgetTextDocument( [ "new".AsMemory() ] ) ) );
		content.SetContent( old ); host.Focus( old );
		Assert.Equal( WidgetCommandResult.Executed, host.ExecuteEditCommand( WidgetEditCommand.Paste, TimeSpan.Zero ) );
		WidgetClipboardRequest request = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single( host.DrainActions() ) ).Request;
		content.SetContent( next ); Assert.Same( next, host.FocusedWidget );
		Assert.False( host.ApplyClipboardResult( new WidgetClipboardResult( request.RequestId, WidgetClipboardStatus.Success, "wrong" ) ) );
		Assert.Equal( "new", next.View.Document.Read( 0, 3 ) );
	}

	[Fact]
	public void CapturedReleaseCannotReachReplacement() {
		using WidgetHost host = new( new CursesScreen( 20, 2 ), WidgetTheme.Default ); WidgetContentHost content = new( "content" ); host.Attach( content ); host.Arrange( host.Screen.Bounds );
		WidgetButton old = new( "old", "Old", "old" ); WidgetButton next = new( "next", "New", "new" ); content.SetContent( old );
		host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press, CursesMouseButton.Primary, 1, 0 ) ), TimeSpan.Zero );
		content.SetContent( next );
		WidgetDispatchResult release = host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Release, CursesMouseButton.Primary, 1, 0 ) ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Empty( release.Actions );
	}

	[Fact]
	public void FailedReplacementPreservesOldContentAndFocus() {
		using WidgetHost host = new( new CursesScreen( 20, 2 ), WidgetTheme.Default ); WidgetContentHost content = new( "content" ); host.Attach( content ); host.Arrange( host.Screen.Bounds );
		WidgetButton old = new( "old", "Old", "old" ); content.SetContent( old ); host.Focus( old );
		for ( int i = 0; i < WidgetLimits.MaximumWidgetsPerHost - 2; i++ ) host.Attach( new WidgetButton( $"filler{i}", "F", "f" ) );
		WidgetStack oversized = new( "replacement", WidgetOrientation.Vertical ); oversized.Add( new WidgetButton( "nested", "N", "n" ) );
		Assert.Throws<InvalidOperationException>( () => content.SetContent( oversized ) );
		Assert.Same( old, content.Content ); Assert.Same( old, host.FocusedWidget ); Assert.Equal( WidgetLimits.MaximumWidgetsPerHost, host.AttachedCount );
	}

	[Fact]
	public void SameIdReattachCannotReviveOldRequestAndNullShowsEmptyWorkspace() {
		using WidgetHost host = new( new CursesScreen( 20, 2 ), WidgetTheme.Default ); WidgetContentHost content = new( "content" ); host.Attach( content ); host.Arrange( host.Screen.Bounds );
		WidgetTextEditor old = new( "editor", new WidgetTextViewState( new WidgetTextDocument( [ "old".AsMemory() ] ) ) );
		WidgetTextEditor next = new( "editor", new WidgetTextViewState( new WidgetTextDocument( [ "new".AsMemory() ] ) ) );
		content.SetContent( old ); host.Focus( old ); host.ExecuteEditCommand( WidgetEditCommand.Paste, TimeSpan.Zero );
		WidgetClipboardRequest request = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single( host.DrainActions() ) ).Request;
		content.SetContent( next ); Assert.False( host.ApplyClipboardResult( new WidgetClipboardResult( request.RequestId, WidgetClipboardStatus.Success, "bad" ) ) );
		Assert.Equal( "new", next.View.Document.Read( 0, 3 ) );
		content.SetContent( null ); Assert.Null( content.Content ); Assert.Empty( content.Children ); Assert.Equal( 1, host.AttachedCount );
	}
}
