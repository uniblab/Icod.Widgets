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

public sealed class WidgetGoToLineDialogTests {
	[Fact]
	public void PositiveOneBasedLineCompletesWithContextWithoutMovingView() {
		using WidgetHost host = new( new CursesScreen( 30, 5 ), WidgetTheme.Default ); WidgetTextViewState view = new( new WidgetTextDocument( [ "first\nsecond".AsMemory() ] ) );
		WidgetGoToLineDialog dialog = new( "line", "Go to line" ); dialog.SetContext( view.Document.Id, view.Id, view.Document.Revision );
		using WidgetModalLease lease = host.ShowModal( dialog ); host.Process( WidgetInput.Paste( "42" ), TimeSpan.Zero );
		WidgetGoToLineRequest request = Assert.IsType<WidgetGoToLineRequest>( Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) ).Result.Value );
		Assert.Equal( 42, request.LineNumber ); Assert.Equal( view.Id, request.ViewId ); Assert.Equal( 0, view.Caret );
	}

	[Theory]
	[InlineData( "0" )]
	[InlineData( "-1" )]
	[InlineData( "9223372036854775808" )]
	[InlineData( "oops" )]
	public void InvalidInputKeepsDialogOpen( string text ) {
		using WidgetHost host = new( new CursesScreen( 24, 4 ), WidgetTheme.Default ); WidgetGoToLineDialog dialog = new( "line", "Go to line" );
		dialog.SetContext( Guid.NewGuid(), Guid.NewGuid(), 0 ); using WidgetModalLease lease = host.ShowModal( dialog ); host.Process( WidgetInput.Paste( text ), TimeSpan.Zero );
		Assert.Empty( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) ).Actions ); Assert.NotNull( dialog.Error ); Assert.True( dialog.IsOpen );
	}

	[Fact]
	public void ContextSwitchClearsOldLineAndEscapeCancels() {
		using WidgetHost host = new( new CursesScreen( 24, 4 ), WidgetTheme.Default ); WidgetGoToLineDialog dialog = new( "line", "Go to line" );
		dialog.SetContext( Guid.NewGuid(), Guid.NewGuid(), 0 ); using WidgetModalLease lease = host.ShowModal( dialog ); host.Process( WidgetInput.Paste( "42" ), TimeSpan.Zero );
		dialog.SetContext( Guid.NewGuid(), Guid.NewGuid(), 0 ); Assert.Equal( "", dialog.LineInput.Value );
		Assert.Equal( WidgetDialogOutcome.Cancel, Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) ).Result.Outcome );
	}
}
