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

public sealed class WidgetDialogTests {
	[Theory]
	[InlineData( 'y', WidgetDialogOutcome.Yes )]
	[InlineData( 'N', WidgetDialogOutcome.No )]
	[InlineData( 'c', WidgetDialogOutcome.Cancel )]
	public void MnemonicActivatesConfiguredOutcomeCaseInsensitively( char key, WidgetDialogOutcome expected ) {
		using WidgetHost host = HostWithBackground( out _ ); WidgetMessageDialog dialog = DecisionDialog(); host.ShowModal( dialog );

		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.TextInput( new Rune( key ) ), TimeSpan.Zero ).Actions ) );

		Assert.Equal( expected, completed.Result.Outcome );
	}

	[Theory]
	[InlineData( WidgetInputModifiers.Shift )]
	[InlineData( WidgetInputModifiers.Control )]
	[InlineData( WidgetInputModifiers.Alt )]
	public void ModifiedMnemonicDoesNotActivate( WidgetInputModifiers modifiers ) {
		using WidgetHost host = HostWithBackground( out _ ); WidgetMessageDialog dialog = DecisionDialog(); host.ShowModal( dialog );

		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Character, modifiers, new Rune( 'y' ) ), TimeSpan.Zero );

		Assert.Empty( result.Actions ); Assert.True( dialog.IsOpen );
	}

	[Theory]
	[InlineData( false )]
	[InlineData( true )]
	public void MnemonicCompletesOnceForTextOrPressedKeyAndIgnoresRelease( bool useText ) {
		using WidgetHost host = HostWithBackground( out _ ); WidgetMessageDialog dialog = DecisionDialog(); host.ShowModal( dialog );
		WidgetInput release = WidgetInput.Key( WidgetKey.Character, character: new Rune( 'y' ) ) with { KeyPhase = CursesKeyEventPhase.Release };
		Assert.Empty( host.Process( release, TimeSpan.Zero ).Actions ); Assert.True( dialog.IsOpen );
		WidgetInput activation = useText ? WidgetInput.TextInput( new Rune( 'Y' ) ) :
			WidgetInput.Key( WidgetKey.Character, character: new Rune( 'y' ) );

		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( activation, TimeSpan.FromMilliseconds( 1 ) ).Actions ) );

		Assert.Equal( WidgetDialogOutcome.Yes, completed.Result.Outcome ); Assert.Empty( host.DrainActions() );
	}

	[Fact]
	public void EscapeUsesDeclaredCancelActionAndValue() {
		object value = new(); using WidgetHost host = HostWithBackground( out _ );
		WidgetMessageDialog dialog = new( "cancel", "Cancel", "Choose.", [
			new( "ok", "OK", WidgetDialogOutcome.Ok ) { IsDefault = true },
			new( "cancel", "Cancel", WidgetDialogOutcome.Cancel ) { IsCancel = true, Value = value }
		] );
		host.ShowModal( dialog );

		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) );

		Assert.Equal( WidgetDialogOutcome.Cancel, completed.Result.Outcome ); Assert.Same( value, completed.Result.Value );
	}

	[Fact]
	public void ArrowsAndTabSkipDisabledActions() {
		using WidgetHost host = HostWithBackground( out _ ); WidgetMessageDialog dialog = DecisionDialog( disableNo: true ); host.ShowModal( dialog );

		host.Process( WidgetInput.Key( WidgetKey.Right ), TimeSpan.Zero ); Assert.Equal( WidgetDialogOutcome.Cancel, dialog.SelectedOutcome );
		host.Process( WidgetInput.Key( WidgetKey.Left ), TimeSpan.Zero ); Assert.Equal( WidgetDialogOutcome.Yes, dialog.SelectedOutcome );
		host.Process( WidgetInput.Key( WidgetKey.Tab ), TimeSpan.Zero ); Assert.Equal( WidgetDialogOutcome.Cancel, dialog.SelectedOutcome );
	}

	[Fact]
	public void EnterUsesExplicitDefault() {
		object value = new(); using WidgetHost host = HostWithBackground( out _ ); WidgetMessageDialog dialog = DecisionDialog( cancelValue: value ); host.ShowModal( dialog );

		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) );

		Assert.Equal( WidgetDialogOutcome.Cancel, completed.Result.Outcome ); Assert.Same( value, completed.Result.Value );
	}

	[Fact]
	public void AllActionsRenderAsBracketedButtons() {
		using WidgetHost host = new( new CursesScreen( 50, 6 ), WidgetTheme.Default ); WidgetMessageDialog dialog = DecisionDialog();
		host.ShowModal( dialog ); host.Render();

		Assert.Contains( "[Yes]  [Don't Save]  [Cancel]", Row( host.GetPresentationWindow( dialog ), 4, 50 ), StringComparison.Ordinal );
	}

	[Fact]
	public void MnemonicUnderlinesFirstCaptionMatchWithoutChangingColors() {
		CursesStyle actionStyle = new( CursesColor.Indexed( 11 ), CursesColor.Indexed( 1 ), CursesTextAttributes.Bold );
		using WidgetHost host = new( new CursesScreen( 30, 6 ), WidgetTheme.Default );
		WidgetMessageDialog dialog = new( "mnemonic", "Mnemonic", "Choose.", [
			new( "never", "Never Now", WidgetDialogOutcome.Accepted ) { IsDefault = true, Mnemonic = 'N' }
		] ) { Styles = WidgetStyles.Empty.With( WidgetStyleRole.DialogFocusedAction, actionStyle ) };
		host.ShowModal( dialog ); host.Render(); CursesWindow window = host.GetPresentationWindow( dialog );

		Assert.Equal( actionStyle, window.GetCell( 4, 1 ).Style );
		Assert.Equal( actionStyle.WithAttributes( CursesTextAttributes.Bold | CursesTextAttributes.Underline ), window.GetCell( 4, 2 ).Style );
		Assert.Equal( actionStyle, window.GetCell( 4, 8 ).Style );
	}

	[Fact]
	public void NarrowDialogTruncatesWithoutChangingActionIdentity() {
		using WidgetHost host = new( new CursesScreen( 8, 5 ), WidgetTheme.Default );
		WidgetMessageDialog dialog = new( "narrow", "Narrow", "Choose.", [
			new( "long", "Long Zebra", WidgetDialogOutcome.Accepted ) { IsDefault = true, Mnemonic = 'Z' }
		] );
		host.ShowModal( dialog ); host.Render(); CursesWindow window = host.GetPresentationWindow( dialog );
		Assert.DoesNotContain( Enumerable.Range( 0, 8 ).Select( column => window.GetCell( 3, column ).Style.Attributes ),
			attributes => 0 != ( attributes & CursesTextAttributes.Underline ) );

		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.TextInput( new Rune( 'z' ) ), TimeSpan.Zero ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.Accepted, completed.Result.Outcome );
	}

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
	public void CaptionControlCharactersAreSanitized() {
		using WidgetHost host = new( new CursesScreen( 20, 5 ), WidgetTheme.Default ); WidgetMessageDialog dialog = WidgetMessageDialog.Ok( "notice", "A\nB", "Saved." ); host.ShowModal( dialog );

		Exception? error = Record.Exception( host.Render );

		Assert.Null( error ); Assert.Contains( "A B", Row( host.GetPresentationWindow( dialog ), 0, 20 ), StringComparison.Ordinal );
	}

	[Fact]
	public void WideAmbiguousGlyphsUseSingleColumnDialogFrame() {
		using WidgetHost host = new( new CursesScreen( 20, 5, UnicodeCursesTextWidthProvider.WideAmbiguousInstance ), WidgetTheme.Default ); WidgetMessageDialog dialog = WidgetMessageDialog.Ok( "notice", "Notice", "Saved." ); host.ShowModal( dialog ); host.Render(); CursesWindow window = host.GetPresentationWindow( dialog );

		Assert.Equal( "+", window.GetCell( 0, 0 ).Content ); Assert.Equal( "+", window.GetCell( 0, 19 ).Content ); Assert.Equal( "|", window.GetCell( 1, 0 ).Content ); Assert.Equal( "|", window.GetCell( 1, 19 ).Content ); Assert.Equal( "+", window.GetCell( 4, 19 ).Content );
	}

	[Fact]
	public void NoDiffersFromCancel() {
		using WidgetHost host = HostWithBackground( out _ );
		WidgetMessageDialog noDialog = LegacyQuestion( "question" ); host.ShowModal( noDialog );
		host.Process( WidgetInput.Key( WidgetKey.Right ), TimeSpan.Zero );
		WidgetDialogCompletedAction no = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 1 ) ).Actions ) );
		WidgetMessageDialog cancelDialog = LegacyQuestion( "question2" ); host.ShowModal( cancelDialog );
		WidgetDialogCompletedAction cancel = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.FromMilliseconds( 2 ) ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.No, no.Result.Outcome ); Assert.Equal( WidgetDialogOutcome.Cancel, cancel.Result.Outcome );
	}

	[Fact]
	public void SafeDefaultActions() {
		using WidgetHost host = HostWithBackground( out _ ); WidgetMessageDialog dialog = LegacyQuestion( "q" ); host.ShowModal( dialog );
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
	public void WizardRendersPageInsideCaptionedFrame() {
		using WidgetHost host = new( new CursesScreen( 20, 5 ), WidgetTheme.Default ); WidgetWizardDialog wizard = new( "wizard", "Setup", [ "First", "Second" ] ); host.ShowModal( wizard ); host.Render();
		CursesWindow window = host.GetPresentationWindow( wizard );

		Assert.Equal( "┌─ Setup (1/2) ────┐", Row( window, 0, 20 ) ); Assert.Equal( "│", window.GetCell( 1, 0 ).Content ); Assert.Equal( "F", window.GetCell( 1, 1 ).Content ); Assert.Equal( "│", window.GetCell( 1, 19 ).Content );
		Assert.Equal( "└──────────────────┘", Row( window, 4, 20 ) );
	}

	[Fact]
	public void WizardCompactLayoutsPreservePageAndError() {
		using WidgetHost pageHost = new( new CursesScreen( 20, 2 ), WidgetTheme.Default ); WidgetWizardDialog pageWizard = new( "page", "Setup", [ "First", "Second" ] ); pageHost.ShowModal( pageWizard ); pageHost.Render();
		Assert.Equal( "F", pageHost.GetPresentationWindow( pageWizard ).GetCell( 1, 0 ).Content );

		using WidgetHost errorHost = new( new CursesScreen( 20, 4 ), WidgetTheme.Default ); WidgetWizardDialog errorWizard = new( "error", "Setup", [ "First", "Second" ] ); errorWizard.RejectNavigation( "Invalid" ); errorHost.ShowModal( errorWizard ); errorHost.Render();
		Assert.Equal( "I", errorHost.GetPresentationWindow( errorWizard ).GetCell( 2, 1 ).Content );
	}

	[Fact]
	public void ModalDepthRejectsSeventeen() {
		using WidgetHost host = HostWithBackground( out _ );
		for ( int i = 0; i < WidgetLimits.MaximumModalDepth; i++ ) host.ShowModal( WidgetMessageDialog.OkCancel( $"d{i}", "D", "D" ) );
		Assert.Throws<InvalidOperationException>( () => host.ShowModal( WidgetMessageDialog.OkCancel( "overflow", "D", "D" ) ) );
		Assert.Equal( WidgetLimits.MaximumModalDepth, host.ModalDepth );
	}

	private static WidgetHost HostWithBackground( out WidgetButton button ) { WidgetHost host = new( new CursesScreen( 40, 10 ), WidgetTheme.Default ); button = new WidgetButton( "background", "Run", "run" ); host.Attach( button ); host.Arrange( host.Screen.Bounds ); host.Focus( button ); return host; }
	private static WidgetMessageDialog DecisionDialog( bool disableNo = false, object? cancelValue = null ) => new( "decision", "Unsaved Changes", "Do you want to save these changes?", [
		new( "yes", "Yes", WidgetDialogOutcome.Yes ) { Mnemonic = 'Y' },
		new( "no", "Don't Save", WidgetDialogOutcome.No ) { Mnemonic = 'N', IsEnabled = !disableNo },
		new( "cancel", "Cancel", WidgetDialogOutcome.Cancel ) { Mnemonic = 'C', IsDefault = true, IsCancel = true, Value = cancelValue }
	] );
	private static WidgetMessageDialog LegacyQuestion( string id ) => new( id, "Question", "Continue?", [
		new( "yes", "Yes", WidgetDialogOutcome.Yes ), new( "no", "No", WidgetDialogOutcome.No ) { IsDefault = true }
	] );
	private static string Row( CursesWindow window, int row, int columns ) => string.Concat( Enumerable.Range( 0, columns ).Select( column => window.GetCell( row, column ).Content ) );
}
