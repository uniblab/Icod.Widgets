/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Tests.Dialogs;

using Icod.DCurses;
using Xunit;

public sealed class WidgetDialogTests {
	[Fact]
	public void OkDialogAcknowledgesEnterAndEscapeCancels() {
		using WidgetHost host = HostWithBackground( out _ );
		WidgetMessageDialog accepted = WidgetMessageDialog.Ok( "notice", "Notice", "Saved." ); host.ShowModal( accepted );
		WidgetDialogCompletedAction ok = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) );
		WidgetMessageDialog dismissed = WidgetMessageDialog.Ok( "notice2", "Notice", "Saved." ); host.ShowModal( dismissed );
		WidgetDialogCompletedAction cancel = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) );

		Assert.Equal( WidgetDialogOutcome.Ok, ok.Result.Outcome ); Assert.Equal( WidgetDialogOutcome.Cancel, cancel.Result.Outcome );
	}

	[Fact]
	public void MessageDialogRendersCaptionedBorderAndInsetContent() {
		using WidgetHost host = new( new CursesScreen( 20, 5 ), WidgetTheme.Default );
		WidgetMessageDialog dialog = WidgetMessageDialog.Ok( "notice", "Notice", "Saved." ); host.ShowModal( dialog ); host.Render();
		CursesWindow window = host.GetPresentationWindow( dialog );

		Assert.Equal( "┌─ Notice ─────────┐", Row( window, 0, 20 ) );
		Assert.Equal( "│", window.GetCell( 1, 0 ).Content ); Assert.Equal( "Saved.", string.Concat( Enumerable.Range( 1, 6 ).Select( column => window.GetCell( 1, column ).Content ) ) ); Assert.Equal( "│", window.GetCell( 1, 19 ).Content );
		Assert.Equal( "│[OK] (Esc cancels)│", Row( window, 3, 20 ) );
		Assert.Equal( "└──────────────────┘", Row( window, 4, 20 ) );
	}

	[Fact]
	public void NoDiffersFromCancel() {
		using WidgetHost host = HostWithBackground( out _ );
		WidgetMessageDialog noDialog = WidgetMessageDialog.YesNo( "question", "Question", "Continue?" ); host.ShowModal( noDialog );
		host.Process( WidgetInput.Key( WidgetKey.Right ), TimeSpan.Zero );
		WidgetDialogCompletedAction no = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) );
		WidgetMessageDialog cancelDialog = WidgetMessageDialog.YesNo( "question2", "Question", "Continue?" ); host.ShowModal( cancelDialog );
		WidgetDialogCompletedAction cancel = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.No, no.Result.Outcome ); Assert.Equal( WidgetDialogOutcome.Cancel, cancel.Result.Outcome );
	}

	[Fact]
	public void SafeDefaultActions() {
		using WidgetHost host = HostWithBackground( out _ ); WidgetMessageDialog dialog = WidgetMessageDialog.YesNo( "q", "Q", "Proceed?" ); host.ShowModal( dialog );
		WidgetDialogCompletedAction action = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.No, action.Result.Outcome );
	}

	[Fact]
	public void EscapeCancelsOnceAndRestoresFocus() {
		using WidgetHost host = HostWithBackground( out WidgetButton background ); WidgetMessageDialog dialog = WidgetMessageDialog.OkCancel( "q", "Q", "Proceed?" ); host.ShowModal( dialog );
		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero );
		Assert.Single( result.Actions ); Assert.Empty( host.DrainActions() ); Assert.Same( background, host.FocusedWidget );
	}

	[Fact]
	public void ModalBlocksBackgroundCommands() {
		using WidgetHost host = HostWithBackground( out _ ); WidgetMessageDialog dialog = WidgetMessageDialog.OkCancel( "q", "Q", "Proceed?" ); host.ShowModal( dialog );
		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Space ), TimeSpan.Zero );
		Assert.Empty( result.Actions ); Assert.True( result.Handled );
	}

	[Fact]
	public void ModalTabCannotFocusBackground() {
		using WidgetHost host = HostWithBackground( out WidgetButton background ); WidgetMessageDialog dialog = WidgetMessageDialog.OkCancel( "q", "Q", "Proceed?" ); host.ShowModal( dialog );
		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Tab ), TimeSpan.Zero );
		Assert.True( result.Handled ); Assert.NotSame( background, host.FocusedWidget ); Assert.Same( dialog, host.FocusedWidget );
	}

	[Fact]
	public void DetachingOpenParentCancelsModalTree() {
		using WidgetHost host = HostWithBackground( out WidgetButton background ); WidgetMessageDialog parent = WidgetMessageDialog.OkCancel( "parent", "P", "P" ); host.ShowModal( parent ); host.ShowModal( WidgetMessageDialog.OkCancel( "child", "C", "C" ) );
		Assert.True( host.Detach( parent ) );
		Assert.Equal( 0, host.ModalDepth ); Assert.False( parent.IsOpen ); Assert.Same( background, host.FocusedWidget );
		Assert.Equal( 2, host.DrainActions().Count );
	}

	[Fact]
	public void ParentCloseCancelsDescendantsOnce() {
		using WidgetHost host = HostWithBackground( out _ ); WidgetModalLease parent = host.ShowModal( WidgetMessageDialog.OkCancel( "parent", "P", "P" ) ); host.ShowModal( WidgetMessageDialog.OkCancel( "child", "C", "C" ) );
		parent.Dispose(); IReadOnlyList<WidgetAction> actions = host.DrainActions();
		Assert.Equal( 2, actions.Count ); Assert.All( actions.Cast<WidgetDialogCompletedAction>(), action => Assert.Equal( WidgetDialogOutcome.Cancel, action.Result.Outcome ) );
	}

	[Fact]
	public void WizardValidationRejectionKeepsPage() {
		using WidgetHost host = HostWithBackground( out _ ); WidgetWizardDialog wizard = new( "wizard", "Setup", [ "One", "Two" ] ); host.ShowModal( wizard );
		Assert.False( wizard.CanPrevious );
		WidgetWizardNavigationRequestedAction request = Assert.IsType<WidgetWizardNavigationRequestedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) );
		Assert.Equal( 1, request.ToPage ); wizard.RejectNavigation( "Invalid" ); Assert.Equal( 0, wizard.PageIndex ); Assert.Equal( "Invalid", wizard.Error ); Assert.True( wizard.IsOpen );
	}

	[Fact]
	public void WizardRendersPageAndCompletesFromFinalPage() {
		using WidgetHost host = HostWithBackground( out _ ); WidgetWizardDialog wizard = new( "wizard", "Setup", [ "First page", "Final page" ] ); host.ShowModal( wizard ); wizard.ApplyNavigation( 1 );
		host.Render();
		Assert.Contains( "Final page", string.Concat( Enumerable.Range( 0, host.Screen.Columns ).Select( column => host.GetPresentationWindow( wizard ).GetCell( 1, column ).Content ) ) );
		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.Accepted, completed.Result.Outcome );
	}

	[Fact]
	public void ModalDepthRejectsSeventeen() {
		using WidgetHost host = HostWithBackground( out _ );
		for ( int i = 0; i < WidgetLimits.MaximumModalDepth; i++ ) host.ShowModal( WidgetMessageDialog.OkCancel( $"d{i}", "D", "D" ) );
		Assert.Throws<InvalidOperationException>( () => host.ShowModal( WidgetMessageDialog.OkCancel( "overflow", "D", "D" ) ) );
		Assert.Equal( WidgetLimits.MaximumModalDepth, host.ModalDepth );
	}

	private static WidgetHost HostWithBackground( out WidgetButton button ) { WidgetHost host = new( new CursesScreen( 40, 10 ), WidgetTheme.Default ); button = new WidgetButton( "background", "Run", "run" ); host.Attach( button ); host.Arrange( host.Screen.Bounds ); host.Focus( button ); return host; }
	private static string Row( CursesWindow window, int row, int columns ) => string.Concat( Enumerable.Range( 0, columns ).Select( column => window.GetCell( row, column ).Content ) );
}
