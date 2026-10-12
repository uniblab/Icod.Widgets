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

namespace Icod.Widgets.Tests.Dialogs;

using Icod.DCurses;
using Xunit;

public sealed class WidgetFormDialogTests {
	[Fact]
	public void EscapeCancelsOkOnlyAndSaveDecision() {
		using WidgetHost host = new( new CursesScreen( 40, 6 ), WidgetTheme.Default );
		WidgetFormDialog ok = new( "ok", "OK", [ new WidgetDialogAction( "ok", "OK", WidgetDialogOutcome.Ok ) { IsDefault = true } ] );
		using ( host.ShowModal( ok ) ) Assert.Equal( WidgetDialogOutcome.Cancel, Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) ).Result.Outcome );
		WidgetFormDialog decision = WidgetFormDialog.SaveDontSaveCancel( "decision", "Close", "Save changes?" );
		using ( host.ShowModal( decision ) ) Assert.Equal( WidgetDialogOutcome.Cancel, Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) ).Result.Outcome );
	}

	[Fact]
	public void SaveDontSaveCancelAreDistinctAndCancelIsDefault() {
		using WidgetHost host = new( new CursesScreen( 40, 6 ), WidgetTheme.Default );
		WidgetFormDialog dialog = WidgetFormDialog.SaveDontSaveCancel( "decision", "Close", "Save changes?" );
		using WidgetModalLease lease = host.ShowModal( dialog );
		WidgetDispatchResult cancel = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero );
		Assert.Equal( WidgetDialogOutcome.Cancel, Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( cancel.Actions ) ).Result.Outcome );
		Assert.NotEqual( WidgetDialogOutcome.Save, WidgetDialogOutcome.DontSave );
	}

	[Fact]
	public void DisabledDefaultCannotCompleteAndValidationAcceptsLatestOnly() {
		using WidgetHost host = new( new CursesScreen( 30, 5 ), WidgetTheme.Default );
		WidgetFormDialog dialog = new( "form", "Form", [ new WidgetDialogAction( "accept", "Accept", WidgetDialogOutcome.Accepted ) { IsDefault = true, RequiresValidation = true } ] );
		using WidgetModalLease lease = host.ShowModal( dialog ); dialog.SetActionEnabled( "accept", false );
		Assert.Empty( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions );
		dialog.SetActionEnabled( "accept", true ); WidgetDispatchResult submit = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) );
		WidgetDialogValidationRequest request = Assert.IsType<WidgetDialogValidationRequestedAction>( Assert.Single( submit.Actions ) ).Request;
		Assert.Empty( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 2 ) ).Actions );
		Assert.False( dialog.ApplyValidationResult( new WidgetDialogValidationResult( request with { Generation = request.Generation - 1 }, true ) ) );
		Assert.True( dialog.ApplyValidationResult( new WidgetDialogValidationResult( request, false, "Invalid" ) ) ); Assert.Equal( "Invalid", dialog.Error );
		WidgetDialogValidationRequest retry = Assert.IsType<WidgetDialogValidationRequestedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 3 ) ).Actions ) ).Request;
		Assert.False( dialog.ApplyValidationResult( new WidgetDialogValidationResult( request, true ) ) );
		Assert.True( dialog.ApplyValidationResult( new WidgetDialogValidationResult( retry, true ) ) );
		Assert.Equal( WidgetDialogOutcome.Accepted, Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.DrainActions() ) ).Result.Outcome );
	}

	[Fact]
	public void DuplicateActionsRejectAndInitialFocusUsesChildId() {
		Assert.Throws<ArgumentException>( () => new WidgetFormDialog( "dup", "D", [ new( "x", "X", WidgetDialogOutcome.Ok ) { IsDefault = true }, new( "x", "Y", WidgetDialogOutcome.No ) ] ) );
		using WidgetHost host = new( new CursesScreen( 25, 5 ), WidgetTheme.Default );
		WidgetFormDialog dialog = new( "form", "F", [ new( "ok", "OK", WidgetDialogOutcome.Ok ) { IsDefault = true } ], "second" );
		WidgetTextInput first = new( "first" ), second = new( "second" ); dialog.Add( first ); dialog.Add( second );
		using WidgetModalLease lease = host.ShowModal( dialog ); Assert.Same( second, host.FocusedWidget );
	}
}
