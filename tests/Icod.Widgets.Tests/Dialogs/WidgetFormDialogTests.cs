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

using System.Text;
using Icod.DCurses;
using Xunit;

public sealed class WidgetFormDialogTests {
	[Fact]
	public void MnemonicUsesSameActionPathAsEnterAndPointer() {
		object value = new();
		foreach ( string route in new[] { "enter", "mnemonic", "pointer" } ) {
			using WidgetHost host = new( new CursesScreen( 30, 6 ), WidgetTheme.Default );
			WidgetFormDialog dialog = new( route, "Form", [
				new( "accept", "Accept", WidgetDialogOutcome.Accepted ) { IsDefault = true, Mnemonic = 'A', Value = value }
			] );
			host.ShowModal( dialog ); host.Render();
			WidgetDispatchResult result = route switch {
				"enter" => host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ),
				"mnemonic" => host.Process( WidgetInput.TextInput( new Rune( 'a' ) ), TimeSpan.Zero ),
				_ => Click( host, 4, 2 )
			};
			WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( result.Actions ) );
			Assert.Equal( WidgetDialogOutcome.Accepted, completed.Result.Outcome ); Assert.Same( value, completed.Result.Value );
		}
	}

	[Fact]
	public void ValidationMnemonicEmitsOneCurrentRequest() {
		using WidgetHost host = new( new CursesScreen( 30, 6 ), WidgetTheme.Default );
		WidgetFormDialog dialog = new( "validation", "Form", [
			new( "submit", "Submit", WidgetDialogOutcome.Accepted ) { IsDefault = true, Mnemonic = 'S', RequiresValidation = true }
		] );
		host.ShowModal( dialog );
		WidgetInput release = WidgetInput.Key( WidgetKey.Character, character: new Rune( 's' ) ) with { KeyPhase = CursesKeyEventPhase.Release };
		Assert.Empty( host.Process( release, TimeSpan.Zero ).Actions );
		WidgetDialogValidationRequest request = Assert.IsType<WidgetDialogValidationRequestedAction>( Assert.Single(
			host.Process( WidgetInput.TextInput( new Rune( 's' ) ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) ).Request;
		dialog.SetActionEnabled( "submit", true ); // A no-op enablement must not invalidate the current request.
		Assert.Empty( host.Process( WidgetInput.TextInput( new Rune( 'S' ) ), TimeSpan.FromMilliseconds( 2 ) ).Actions );
		Assert.False( dialog.ApplyValidationResult( new WidgetDialogValidationResult( request with { Generation = request.Generation - 1 }, true ) ) );
		Assert.True( dialog.ApplyValidationResult( new WidgetDialogValidationResult( request, true ) ) );
		Assert.Equal( WidgetDialogOutcome.Accepted,
			Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.DrainActions() ) ).Result.Outcome );
	}

	[Fact]
	public void FormButtonsRenderBracketedUnicodeCaptions() {
		using WidgetHost host = new( new CursesScreen( 30, 6 ), WidgetTheme.Default );
		WidgetFormDialog dialog = new( "unicode", "Form", [
			new( "wide", "界", WidgetDialogOutcome.Yes ) { IsDefault = true },
			new( "no", "No", WidgetDialogOutcome.No )
		] );
		host.ShowModal( dialog ); host.Render(); CursesWindow window = host.GetPresentationWindow( dialog );
		Assert.Equal( "[", window.GetCell( 4, 1 ).Content ); Assert.Equal( "界", window.GetCell( 4, 2 ).Content );
		Assert.True( window.GetCell( 4, 3 ).IsContinuation ); Assert.Equal( "]", window.GetCell( 4, 4 ).Content );
		Assert.Equal( "[", window.GetCell( 4, 6 ).Content ); Assert.Equal( "]", window.GetCell( 4, 9 ).Content );

		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( Click( host, 4, 9 ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.No, completed.Result.Outcome );
	}

	[Fact]
	public void ClippedWideFormGlyphLeavesNoInvisibleClickableColumn() {
		using WidgetHost host = new( new CursesScreen( 12, 6 ), WidgetTheme.Default );
		WidgetFormDialog dialog = new( "unicode", "Form", [
			new( "wide", "界", WidgetDialogOutcome.Yes ) { IsDefault = true }
		] );
		host.ShowModal( dialog ); dialog.Arrange( new( 0, 8, 6, 20 ) ); host.Render();
		CursesWindow window = host.GetPresentationWindow( dialog );
		Assert.Equal( "[", window.GetCell( 4, 1 ).Content );
		Assert.Equal( " ", window.GetCell( 4, 2 ).Content );

		host.Process( WidgetInput.PointerInput( new( CursesMouseAction.Press, CursesMouseButton.Primary, 10, 4 ) ), TimeSpan.Zero );
		Assert.Empty( host.Process( WidgetInput.PointerInput( new( CursesMouseAction.Release, CursesMouseButton.Primary, 10, 4 ) ), TimeSpan.Zero ).Actions );
		Assert.True( dialog.IsOpen );
	}

	[Fact]
	public void EscapeActivatesDeclaredCancelAction() {
		object value = new(); using WidgetHost host = new( new CursesScreen( 30, 6 ), WidgetTheme.Default );
		WidgetFormDialog dialog = new( "cancel", "Form", [
			new( "ok", "OK", WidgetDialogOutcome.Ok ) { IsDefault = true },
			new( "keep", "Keep", WidgetDialogOutcome.No ) { IsCancel = true, Value = value }
		] );
		host.ShowModal( dialog );

		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) );

		Assert.Equal( WidgetDialogOutcome.No, completed.Result.Outcome ); Assert.Same( value, completed.Result.Value );
	}

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
	public void DisablingFinalActionIsRejectedAndValidationAcceptsLatestOnly() {
		using WidgetHost host = new( new CursesScreen( 30, 5 ), WidgetTheme.Default );
		WidgetFormDialog dialog = new( "form", "Form", [ new WidgetDialogAction( "accept", "Accept", WidgetDialogOutcome.Accepted ) { IsDefault = true, RequiresValidation = true } ] );
		using WidgetModalLease lease = host.ShowModal( dialog );
		Assert.Throws<InvalidOperationException>( () => dialog.SetActionEnabled( "accept", false ) );
		Assert.True( Assert.Single( dialog.Actions ).IsEnabled );
		WidgetDispatchResult submit = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) );
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

	private static WidgetDispatchResult Click( WidgetHost host, int row, int column ) {
		host.Process( WidgetInput.PointerInput( new( CursesMouseAction.Press, CursesMouseButton.Primary, column, row ) ), TimeSpan.Zero );
		return host.Process( WidgetInput.PointerInput( new( CursesMouseAction.Release, CursesMouseButton.Primary, column, row ) ), TimeSpan.Zero );
	}
}
