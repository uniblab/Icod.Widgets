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

namespace Icod.Widgets.Tests.Search;

using Icod.DCurses;
using Xunit;

public sealed class WidgetSearchFormTests {
	[Fact]
	public void FindAndReplaceEmitTypedIntentWithoutEditingDocument() {
		using WidgetHost host = new( new CursesScreen( 40, 7 ), WidgetTheme.Default );
		WidgetTextDocument document = new( [ "hello".AsMemory() ] ); WidgetTextViewState view = new( document );
		WidgetSearchForm form = new( "search", "Find", true ); form.SetContext( document.Id, view.Id, document.Revision );
		using WidgetModalLease lease = host.ShowModal( form ); host.Focus( form.QueryInput );
		host.Process( WidgetInput.Paste( "ell" ), TimeSpan.Zero );
		WidgetSearchRequest request = Assert.IsType<WidgetSearchRequestedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) ).Request;
		Assert.Equal( WidgetSearchIntent.Find, request.Intent ); Assert.Equal( "ell", request.Query ); Assert.Equal( document.Id, request.DocumentId );
		form.SetOptions( new WidgetSearchOptions( MatchCase: true ) );
		WidgetSearchRequest replace = form.Submit( WidgetSearchIntent.ReplaceAll ); Assert.Single( host.DrainActions() );
		Assert.True( replace.Options.MatchCase ); Assert.Equal( "hello", document.Read( 0, 5 ) );
	}

	[Fact]
	public void FindOnlyRejectsReplaceAndEscapeCancels() {
		using WidgetHost host = new( new CursesScreen( 30, 6 ), WidgetTheme.Default ); WidgetSearchForm form = new( "search", "Find", false );
		form.SetContext( Guid.NewGuid(), Guid.NewGuid(), 0 ); using WidgetModalLease lease = host.ShowModal( form );
		Assert.Null( form.ReplacementInput ); Assert.Throws<InvalidOperationException>( () => form.Submit( WidgetSearchIntent.ReplaceOne ) );
		Assert.Equal( WidgetDialogOutcome.Cancel, Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) ).Result.Outcome );
	}

	[Fact]
	public void FindOnlyPromptDoesNotAdvertiseReplace() {
		using WidgetHost host = new( new CursesScreen( 40, 7 ), WidgetTheme.Default ); WidgetSearchForm form = new( "search", "Find", false );
		form.SetContext( Guid.NewGuid(), Guid.NewGuid(), 0 ); using WidgetModalLease lease = host.ShowModal( form ); host.Render(); CursesWindow window = host.GetPresentationWindow( form );
		string rendered = string.Concat( Enumerable.Range( 0, window.Rows ).SelectMany( row => Enumerable.Range( 0, window.Columns ).Select( column => window.GetCell( row, column ).Content ) ) );
		Assert.Contains( "Enter: Find", rendered ); Assert.DoesNotContain( "Replace", rendered );
	}

	[Fact]
	public void QueryChangedOptionsChangedAndForeignContextRejectOldResults() {
		using WidgetHost host = new( new CursesScreen( 30, 7 ), WidgetTheme.Default ); WidgetSearchForm form = new( "search", "Find", true );
		Guid document = Guid.NewGuid(), view = Guid.NewGuid(); form.SetContext( document, view, 0 ); using WidgetModalLease lease = host.ShowModal( form );
		host.Process( WidgetInput.TextInput( new System.Text.Rune( 'a' ) ), TimeSpan.Zero );
		WidgetSearchRequest first = form.Submit( WidgetSearchIntent.Find ); _ = host.DrainActions();
		host.Process( WidgetInput.TextInput( new System.Text.Rune( 'b' ) ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.False( form.ApplyResult( new WidgetSearchResult( first, true, 0, 1 ) ) );
		WidgetSearchRequest second = form.Submit( WidgetSearchIntent.Find ); _ = host.DrainActions();
		form.SetOptions( new WidgetSearchOptions( WholeWord: true ) ); Assert.False( form.ApplyResult( new WidgetSearchResult( second, true, 0, 1 ) ) );
		WidgetSearchRequest third = form.Submit( WidgetSearchIntent.Find ); _ = host.DrainActions();
		form.SetContext( Guid.NewGuid(), view, 0 ); Assert.False( form.ApplyResult( new WidgetSearchResult( third, true, 0, 1 ) ) );
	}
}
