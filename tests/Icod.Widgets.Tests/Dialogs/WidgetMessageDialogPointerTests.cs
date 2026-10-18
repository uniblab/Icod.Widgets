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

public sealed class WidgetMessageDialogPointerTests {
	[Theory]
	[InlineData( "ok", 3, WidgetDialogOutcome.Ok )]
	[InlineData( "yesno", 3, WidgetDialogOutcome.Yes )]
	[InlineData( "yesno", 10, WidgetDialogOutcome.No )]
	[InlineData( "okcancel", 3, WidgetDialogOutcome.Ok )]
	[InlineData( "okcancel", 9, WidgetDialogOutcome.Cancel )]
	public void ClickCompletesChosenActionOnceAndRestoresFocus( string kind, int column, WidgetDialogOutcome outcome ) {
		using WidgetHost host = new( new CursesScreen( 40, 10 ), WidgetTheme.Default );
		WidgetButton background = new( "background", "Run", "run" ); host.Attach( background ); host.Arrange( host.Screen.Bounds ); host.Focus( background );
		WidgetMessageDialog dialog = kind switch {
			"yesno" => new WidgetMessageDialog( "message", "Question", "Continue?", [
				new( "yes", "Yes", WidgetDialogOutcome.Yes ) { IsDefault = true },
				new( "no", "No", WidgetDialogOutcome.No )
			] ),
			"okcancel" => WidgetMessageDialog.OkCancel( "message", "Question", "Continue?" ),
			_ => WidgetMessageDialog.Ok( "message", "Notice", "Saved." )
		};
		using WidgetModalLease lease = host.ShowModal( dialog ); dialog.Arrange( new( 2, 2, 5, 30 ) ); host.Render();
		Assert.Empty( Pointer( host, CursesMouseAction.Press, 5, column ).Actions ); Assert.True( dialog.IsOpen ); host.Render();
		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( Pointer( host, CursesMouseAction.Release, 5, column ).Actions ) );
		Assert.Equal( outcome, completed.Result.Outcome ); Assert.False( dialog.IsOpen ); Assert.Equal( 0, host.ModalDepth ); Assert.Same( background, host.FocusedWidget );
		Assert.Empty( Pointer( host, CursesMouseAction.Release, 5, column ).Actions );
	}

	[Theory]
	[InlineData( 1, 7, CursesMouseButton.Primary )] // Release on a different choice.
	[InlineData( 1, 5, CursesMouseButton.Primary )] // Release in the gap.
	[InlineData( 5, 1, CursesMouseButton.Primary )] // Press in the gap.
	[InlineData( 16, 16, CursesMouseButton.Primary )] // Cancellation hint is not a button.
	[InlineData( 1, 1, CursesMouseButton.Secondary )]
	public void NonmatchingClicksDoNotComplete( int pressColumn, int releaseColumn, CursesMouseButton button ) {
		using WidgetHost host = new( new CursesScreen( 40, 6 ), WidgetTheme.Default );
		WidgetMessageDialog dialog = WidgetMessageDialog.OkCancel( "message", "Question", "Continue?" ); using WidgetModalLease lease = host.ShowModal( dialog ); host.Render();
		Assert.Empty( Pointer( host, CursesMouseAction.Release, 4, 1 ).Actions );
		Assert.Empty( Pointer( host, CursesMouseAction.Press, 4, pressColumn, button ).Actions );
		Assert.Empty( Pointer( host, CursesMouseAction.Release, 4, releaseColumn, button ).Actions ); Assert.True( dialog.IsOpen );
		Assert.Equal( WidgetDialogOutcome.Cancel, dialog.SelectedOutcome );
	}

	[Theory]
	[InlineData( "focus" )]
	[InlineData( "arrange" )]
	[InlineData( "disable" )]
	[InlineData( "reopen" )]
	[InlineData( "keyboard" )]
	public void InterruptedPressCannotCompleteLater( string interruption ) {
		using WidgetHost host = new( new CursesScreen( 40, 6 ), WidgetTheme.Default );
		WidgetMessageDialog dialog = WidgetMessageDialog.Ok( "message", "Notice", "Saved." ); using WidgetModalLease lease = host.ShowModal( dialog ); host.Render();
		Pointer( host, CursesMouseAction.Press, 4, 1 );
		switch ( interruption ) {
			case "focus": host.Process( WidgetInput.FocusChanged( CursesFocusState.Unfocused ), TimeSpan.Zero ); break;
			case "arrange": dialog.Arrange( dialog.Bounds ); break;
			case "disable": dialog.IsEnabled = false; dialog.IsEnabled = true; break;
			case "reopen": lease.Dispose(); host.DrainActions(); host.ShowModal( dialog ); break;
			case "keyboard": host.Process( WidgetInput.Key( WidgetKey.Tab ), TimeSpan.Zero ); break;
		}
		Assert.Empty( Pointer( host, CursesMouseAction.Release, 4, 1 ).Actions ); Assert.True( dialog.IsOpen );
		Assert.Empty( Pointer( host, CursesMouseAction.Press, 4, 1 ).Actions );
		Assert.Single( Pointer( host, CursesMouseAction.Release, 4, 1 ).Actions );
	}

	[Fact]
	public void ClickUsesClippedActionRowAndKeyboardSelectedLabelWidths() {
		using WidgetHost host = new( new CursesScreen( 12, 6 ), WidgetTheme.Default );
		WidgetMessageDialog dialog = WidgetMessageDialog.OkCancel( "message", "Question", "Continue?" ); using WidgetModalLease lease = host.ShowModal( dialog );
		dialog.Arrange( new( 2, 6, 8, 20 ) ); host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.Zero ); host.Render();
		Assert.Equal( "[", host.GetPresentationWindow( dialog ).GetCell( 2, 1 ).Content );
		Assert.Empty( Pointer( host, CursesMouseAction.Press, 4, 11 ).Actions ); // Frame, not the clipped OK label.
		Assert.Empty( Pointer( host, CursesMouseAction.Release, 4, 11 ).Actions );
		Assert.Empty( Pointer( host, CursesMouseAction.Press, 4, 10 ).Actions ); // Closing bracket of [OK].
		Assert.Equal( WidgetDialogOutcome.Ok, Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( Pointer( host, CursesMouseAction.Release, 4, 10 ).Actions ) ).Result.Outcome );
	}

	[Fact]
	public void UnicodeCaptionUsesDisplayColumnsForRenderAndHitTest() {
		using WidgetHost host = new( new CursesScreen( 30, 6 ), WidgetTheme.Default );
		WidgetMessageDialog dialog = new( "unicode", "Unicode", "Choose.", [
			new( "wide", "界", WidgetDialogOutcome.Yes ) { IsDefault = true },
			new( "no", "No", WidgetDialogOutcome.No )
		] );
		host.ShowModal( dialog ); host.Render(); CursesWindow window = host.GetPresentationWindow( dialog );
		Assert.Equal( "[", window.GetCell( 4, 1 ).Content ); Assert.Equal( "界", window.GetCell( 4, 2 ).Content );
		Assert.True( window.GetCell( 4, 3 ).IsContinuation ); Assert.Equal( "]", window.GetCell( 4, 4 ).Content );
		Assert.Equal( "[", window.GetCell( 4, 7 ).Content ); Assert.Equal( "]", window.GetCell( 4, 10 ).Content );

		Assert.Empty( Pointer( host, CursesMouseAction.Press, 4, 10 ).Actions );
		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			Pointer( host, CursesMouseAction.Release, 4, 10 ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.No, completed.Result.Outcome );
	}

	[Fact]
	public void DisabledActionDoesNotCaptureOrActivatePointer() {
		using WidgetHost host = new( new CursesScreen( 30, 6 ), WidgetTheme.Default );
		WidgetMessageDialog dialog = new( "disabled", "Disabled", "Choose.", [
			new( "ok", "OK", WidgetDialogOutcome.Ok ) { IsDefault = true },
			new( "cancel", "Cancel", WidgetDialogOutcome.Cancel ) { IsEnabled = false }
		] );
		host.ShowModal( dialog ); host.Render();

		Assert.Empty( Pointer( host, CursesMouseAction.Press, 4, 8 ).Actions );
		Assert.Empty( Pointer( host, CursesMouseAction.Release, 4, 8 ).Actions );
		Assert.True( dialog.IsOpen ); Assert.Equal( WidgetDialogOutcome.Ok, dialog.SelectedOutcome );
	}

	private static WidgetDispatchResult Pointer( WidgetHost host, CursesMouseAction action, int row, int column, CursesMouseButton button = CursesMouseButton.Primary ) =>
		host.Process( WidgetInput.PointerInput( new( action, button, column, row ) ), TimeSpan.Zero );
}
