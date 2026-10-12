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

namespace Icod.Widgets.Tests.Pickers;

using Icod.DCurses;
using Xunit;

public sealed class WidgetSaveTargetPickerTests {
	private sealed class Provider : IWidgetFileSystemProvider {
		public StringComparer PathComparer => StringComparer.OrdinalIgnoreCase;
		public ValueTask<IReadOnlyList<WidgetFileSystemEntry>> GetRootsAsync( CancellationToken token ) => throw new NotImplementedException();
		public ValueTask<WidgetDirectorySnapshot> ReadDirectoryAsync( string location, CancellationToken token ) => throw new NotImplementedException();
		public ValueTask<WidgetFileSelectionValidation> ValidateSelectionAsync( IReadOnlyList<string> paths, WidgetFileSelectionKind kind, CancellationToken token ) => throw new NotImplementedException();
	}

	[Fact]
	public void NewAndExistingTargetObservationsCompleteWithoutWriting() {
		using WidgetHost host = new( new CursesScreen( 35, 8 ), WidgetTheme.Default );
		WidgetSaveTargetPicker picker = new( "save", new Provider(), new Inspector(), "/drafts", "draft.txt" );
		using WidgetModalLease lease = host.ShowModal( picker );
		WidgetSaveTargetRequest request = picker.RequestAccept(); Assert.Equal( "draft.txt", request.FileName );
		Assert.True( picker.ApplyInspectionResult( new WidgetSaveTargetInspectionResult( request, new WidgetSaveTargetObservation( "/drafts/draft.txt", false, true ) ) ) );
		WidgetSaveTargetSelection selection = Assert.IsType<WidgetSaveTargetSelection>( Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.DrainActions() ) ).Result.Value );
		Assert.False( selection.ExistedAtInspection ); Assert.Equal( "/drafts/draft.txt", selection.TargetPath );
	}

	[Fact]
	public void NameChangeAndNavigationInvalidateOldInspection() {
		using WidgetHost host = new( new CursesScreen( 35, 8 ), WidgetTheme.Default ); WidgetSaveTargetPicker picker = new( "save", new Provider(), new Inspector(), "/drafts", "draft.txt" );
		using WidgetModalLease lease = host.ShowModal( picker ); WidgetSaveTargetRequest old = picker.RequestAccept();
		picker.NameInput.SetSelection( 0, picker.NameInput.Value.Length ); host.Focus( picker.NameInput );
		host.Process( WidgetInput.TextInput( new System.Text.Rune( 'x' ) ), TimeSpan.Zero );
		Assert.False( picker.ApplyInspectionResult( new WidgetSaveTargetInspectionResult( old, new( "/drafts/draft.txt", true, true ) ) ) );
		WidgetSaveTargetRequest next = picker.RequestAccept(); Assert.Equal( "x", next.FileName );
		picker.RequestDirectory( "/other" ); Assert.False( picker.ApplyInspectionResult( new WidgetSaveTargetInspectionResult( next, new( "/drafts/x", false, true ) ) ) );
	}

	[Fact]
	public void UnsupportedCapabilityAndInvalidObservationLeaveDialogOpen() {
		using WidgetHost host = new( new CursesScreen( 35, 8 ), WidgetTheme.Default ); WidgetSaveTargetPicker unsupported = new( "save", new Provider(), null, "/drafts", "x" );
		using ( host.ShowModal( unsupported ) ) {
			Assert.Throws<InvalidOperationException>( () => unsupported.RequestAccept() ); Assert.NotNull( unsupported.ValidationError ); Assert.True( unsupported.IsOpen );
			Assert.Equal( WidgetDialogOutcome.Cancel, Assert.IsType<WidgetDialogCompletedAction>( Assert.Single( host.Process( WidgetInput.Key( WidgetKey.Escape ), TimeSpan.Zero ).Actions ) ).Result.Outcome );
		}
		WidgetSaveTargetPicker picker = new( "next", new Provider(), new Inspector(), "/drafts", "x" );
		using ( host.ShowModal( picker ) ) {
			WidgetSaveTargetRequest request = picker.RequestAccept();
			Assert.True( picker.ApplyInspectionResult( new( request, new( "", false, false, "Invalid path" ) ) ) );
			Assert.Equal( "Invalid path", picker.ValidationError ); Assert.True( picker.IsOpen );
		}
	}

	private sealed class Inspector : IWidgetSaveTargetProvider {
		public ValueTask<WidgetSaveTargetObservation> InspectTargetAsync( string directory, string fileName, CancellationToken token ) =>
			ValueTask.FromResult( new WidgetSaveTargetObservation( directory + "/" + fileName, false, true ) );
	}
}
