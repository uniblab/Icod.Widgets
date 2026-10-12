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

public sealed class WidgetWorkspaceScenarioTests {
	[Fact]
	public void ThreeDocumentsSwitchAndCancelCloseKeepsModifiedView() {
		using WidgetHost host = new( new CursesScreen( 40, 7 ), WidgetTheme.Default );
		WidgetTabStrip tabs = new( "tabs" ); WidgetContentHost content = new( "content" ); host.Attach( tabs ); host.Attach( content );
		tabs.Arrange( new CursesRectangle( 0, 0, 1, 40 ) ); content.Arrange( new CursesRectangle( 1, 0, 6, 40 ) );
		WidgetTextViewState[] views = Enumerable.Range( 0, 3 ).Select( i => new WidgetTextViewState( new WidgetTextDocument( [ $"doc{i}".AsMemory() ] ) ) ).ToArray();
		WidgetTextEditor[] editors = views.Select( ( view, i ) => new WidgetTextEditor( $"edit{i}", view ) ).ToArray();
		tabs.SetTabs( [ new( "0", "Zero" ), new( "1", "One", true ), new( "2", "Two" ) ], "1" ); content.SetContent( editors[ 1 ] ); host.Focus( editors[ 1 ] );
		views[ 1 ].SetSelection( 1, 2 ); content.SetContent( editors[ 2 ] ); content.SetContent( editors[ 1 ] );
		Assert.Same( views[ 1 ], editors[ 1 ].View ); Assert.Equal( 2, views[ 1 ].Caret );
		WidgetFormDialog decision = WidgetFormDialog.SaveDontSaveCancel( "close", "Close", "Save changes?" );
		using WidgetModalLease lease = host.ShowModal( decision );
		WidgetDialogCompletedAction cancel = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.Cancel, cancel.Result.Outcome ); Assert.Equal( 3, tabs.Tabs.Count ); Assert.Same( editors[ 1 ], content.Content );
	}

	[Fact]
	public void OldSearchAndSaveObservationsCannotChangeNewActiveView() {
		using WidgetHost host = new( new CursesScreen( 40, 7 ), WidgetTheme.Default ); WidgetContentHost content = new( "content" ); host.Attach( content ); content.Arrange( host.Screen.Bounds );
		WidgetTextViewState first = new( new WidgetTextDocument( [ "first".AsMemory() ] ) ), second = new( new WidgetTextDocument( [ "second".AsMemory() ] ) );
		WidgetTextEditor firstEditor = new( "first", first ), secondEditor = new( "second", second ); content.SetContent( firstEditor );
		WidgetSearchForm form = new( "search", "Find", false ); form.SetContext( first.Document.Id, first.Id, first.Document.Revision );
		using WidgetModalLease lease = host.ShowModal( form ); host.Process( WidgetInput.TextInput( new System.Text.Rune( 'f' ) ), TimeSpan.Zero );
		WidgetSearchRequest pending = form.Submit( WidgetSearchIntent.Find ); _ = host.DrainActions(); content.SetContent( secondEditor );
		form.SetContext( second.Document.Id, second.Id, second.Document.Revision );
		Assert.False( form.ApplyResult( new WidgetSearchResult( pending, true, 0, 1 ) ) ); Assert.Equal( 0, second.Caret );
	}
}
