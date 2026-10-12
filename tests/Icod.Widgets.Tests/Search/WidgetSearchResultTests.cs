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

public sealed class WidgetSearchResultTests {
	[Fact]
	public void ChangedThenRestoredQueryStillInvalidatesOldResult() {
		using WidgetHost host = new( new CursesScreen( 30, 6 ), WidgetTheme.Default ); WidgetSearchForm form = new( "search", "Find", false );
		form.SetContext( Guid.NewGuid(), Guid.NewGuid(), 0 ); using WidgetModalLease lease = host.ShowModal( form );
		host.Process( WidgetInput.TextInput( new System.Text.Rune( 'a' ) ), TimeSpan.Zero ); WidgetSearchRequest old = form.Submit( WidgetSearchIntent.Find ); _ = host.DrainActions();
		host.Process( WidgetInput.Key( WidgetKey.Backspace ), TimeSpan.FromMilliseconds( 1 ) );
		host.Process( WidgetInput.TextInput( new System.Text.Rune( 'a' ) ), TimeSpan.FromMilliseconds( 2 ) );
		Assert.Equal( "a", form.QueryInput.Value ); Assert.False( form.ApplyResult( new WidgetSearchResult( old, true, 0, 1 ) ) );
	}

	[Fact]
	public void ForeignRequestAndCloseRejectResult() {
		using WidgetHost host = new( new CursesScreen( 30, 6 ), WidgetTheme.Default ); WidgetSearchForm form = new( "search", "Find", false );
		form.SetContext( Guid.NewGuid(), Guid.NewGuid(), 0 ); using WidgetModalLease lease = host.ShowModal( form );
		host.Process( WidgetInput.TextInput( new System.Text.Rune( 'a' ) ), TimeSpan.Zero ); WidgetSearchRequest current = form.Submit( WidgetSearchIntent.Find ); _ = host.DrainActions();
		Assert.False( form.ApplyResult( new WidgetSearchResult( current with { DocumentId = Guid.NewGuid() }, true, 0, 1 ) ) );
		Assert.True( form.ApplyResult( new WidgetSearchResult( current, false ) ) ); Assert.Equal( "Not found.", form.Error );
		WidgetSearchRequest next = form.Submit( WidgetSearchIntent.Find ); _ = host.DrainActions();
		lease.Dispose(); _ = host.DrainActions(); Assert.False( form.ApplyResult( new WidgetSearchResult( next, true, 0, 1 ) ) );
	}
}
