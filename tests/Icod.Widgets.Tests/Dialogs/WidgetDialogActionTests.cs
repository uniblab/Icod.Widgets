/*
	Icod.Widgets.Tests
	Checks the generic message-dialog action contract.
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

using System.Collections.ObjectModel;
using System.Reflection;
using Icod.DCurses;
using Xunit;

public sealed class WidgetDialogActionTests {
	[Fact]
	public void GenericConstructorCopiesAndExposesReadOnlyActions() {
		WidgetDialogAction original = new( "accept", "Accept", WidgetDialogOutcome.Accepted ) { IsDefault = true };
		WidgetDialogAction[] source = [ original ];
		WidgetMessageDialog dialog = new( "generic", "Generic", "Choose.", source );
		source[ 0 ] = new( "other", "Other", WidgetDialogOutcome.Cancel ) { IsDefault = true };

		Assert.IsAssignableFrom<ReadOnlyCollection<WidgetDialogAction>>( dialog.Actions );
		Assert.Same( original, Assert.Single( dialog.Actions ) );
		Assert.Throws<NotSupportedException>( () =>
			( (IList<WidgetDialogAction>)dialog.Actions )[ 0 ] = source[ 0 ] );
	}

	[Fact]
	public void CompletesWithConfiguredOutcomeAndValue() {
		object value = new();
		WidgetMessageDialog dialog = new( "generic", "Generic", "Choose.", [
			new( "accept", "Accept", WidgetDialogOutcome.Accepted ) { IsDefault = true, Value = value }
		] );
		using WidgetHost host = new( new CursesScreen( 30, 8 ), WidgetTheme.Default );
		host.ShowModal( dialog );

		WidgetDialogCompletedAction completed = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) );

		Assert.Equal( WidgetDialogOutcome.Accepted, completed.Result.Outcome );
		Assert.Same( value, completed.Result.Value );
	}

	[Fact]
	public void RejectsInvalidActionArrays() {
		Assert.Throws<ArgumentOutOfRangeException>( () => New( [] ) );
		Assert.Throws<ArgumentOutOfRangeException>( () => New( Enumerable.Range( 0, WidgetLimits.MaximumItems + 1 )
			.Select( index => new WidgetDialogAction( $"action-{index}", "Action", WidgetDialogOutcome.Ok ) { IsDefault = 0 == index } ).ToArray() ) );
		Assert.Throws<ArgumentException>( () => New( [ null! ] ) );
		Assert.Throws<ArgumentException>( () => New( [ new( " ", "Blank", WidgetDialogOutcome.Ok ) { IsDefault = true } ] ) );
		Assert.Throws<ArgumentException>( () => New( [
			new( "same", "First", WidgetDialogOutcome.Ok ) { IsDefault = true },
			new( "same", "Second", WidgetDialogOutcome.Cancel )
		] ) );
		Assert.Throws<ArgumentException>( () => New( [ new( "null-label", null!, WidgetDialogOutcome.Ok ) { IsDefault = true } ] ) );
		Assert.Throws<ArgumentException>( () => New( [ new( "no-default", "No default", WidgetDialogOutcome.Ok ) ] ) );
		Assert.Throws<ArgumentException>( () => New( [
			new( "first", "First", WidgetDialogOutcome.Ok ) { IsDefault = true },
			new( "second", "Second", WidgetDialogOutcome.Cancel ) { IsDefault = true }
		] ) );
		Assert.Throws<ArgumentException>( () => New( [
			new( "disabled", "Disabled", WidgetDialogOutcome.Ok ) { IsDefault = true, IsEnabled = false },
			new( "enabled", "Enabled", WidgetDialogOutcome.Cancel )
		] ) );
		Assert.Throws<ArgumentException>( () => New( [
			new( "first", "First", WidgetDialogOutcome.Ok ) { IsDefault = true, IsCancel = true },
			new( "second", "Second", WidgetDialogOutcome.Cancel ) { IsCancel = true }
		] ) );
		Assert.Throws<ArgumentException>( () => New( [
			new( "first", "First", WidgetDialogOutcome.Ok ) { IsDefault = true, Mnemonic = 'A' },
			new( "second", "Second", WidgetDialogOutcome.Cancel ) { Mnemonic = 'a' }
		] ) );
		Assert.Throws<ArgumentException>( () => New( [
			new( "validate", "Validate", WidgetDialogOutcome.Accepted ) { IsDefault = true, RequiresValidation = true }
		] ) );
	}

	[Fact]
	public void DisablingSelectionAdvancesAndFinalEnabledActionIsRequired() {
		WidgetMessageDialog dialog = new( "generic", "Generic", "Choose.", [
			new( "first", "First", WidgetDialogOutcome.Yes ) { IsDefault = true },
			new( "second", "Second", WidgetDialogOutcome.No )
		] );

		dialog.SetActionEnabled( "first", false );
		Assert.Equal( WidgetDialogOutcome.No, dialog.SelectedOutcome );
		Assert.Throws<InvalidOperationException>( () => dialog.SetActionEnabled( "second", false ) );
		Assert.True( dialog.Actions.Single( action => "second" == action.Id ).IsEnabled );
		Assert.Throws<ArgumentException>( () => dialog.SetActionEnabled( "missing", false ) );
	}

	[Fact]
	public void LegacyYesNoIsWarningObsoleteAndBehaviorCompatible() {
		MethodInfo method = Assert.Single( typeof( WidgetMessageDialog ).GetMethods( BindingFlags.Public | BindingFlags.Static ),
			candidate => "YesNo" == candidate.Name );
		ObsoleteAttribute obsolete = Assert.IsType<ObsoleteAttribute>( method.GetCustomAttribute<ObsoleteAttribute>() );
		Assert.Equal( "Use the WidgetMessageDialog action-array constructor so captions, mnemonics, default, and cancel behavior are explicit.", obsolete.Message );
		Assert.False( obsolete.IsError );

		WidgetMessageDialog noDialog = Assert.IsType<WidgetMessageDialog>( method.Invoke( null, [ "no", "Question", "Continue?" ] ) );
		using WidgetHost host = new( new CursesScreen( 30, 8 ), WidgetTheme.Default );
		host.ShowModal( noDialog );
		WidgetDialogCompletedAction no = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.No, no.Result.Outcome );

		WidgetMessageDialog cancelDialog = Assert.IsType<WidgetMessageDialog>( method.Invoke( null, [ "cancel", "Question", "Continue?" ] ) );
		host.ShowModal( cancelDialog );
		WidgetDialogCompletedAction cancel = Assert.IsType<WidgetDialogCompletedAction>( Assert.Single(
			host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) );
		Assert.Equal( WidgetDialogOutcome.Cancel, cancel.Result.Outcome );
	}

	private static WidgetMessageDialog New( IReadOnlyList<WidgetDialogAction> actions ) =>
		new( "generic", "Generic", "Choose.", actions );
}
