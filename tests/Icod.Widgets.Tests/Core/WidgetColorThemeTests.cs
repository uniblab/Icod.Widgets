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

namespace Icod.Widgets.Tests.Core;

using Icod.DCurses;
using Xunit;

public sealed class WidgetColorThemeTests {
	private static readonly CursesStyle Blue = new( CursesColor.Indexed( 15 ), CursesColor.Indexed( 4 ) );
	private static readonly CursesStyle Red = new( CursesColor.Indexed( 11 ), CursesColor.Indexed( 1 ) );

	[Fact]
	public void ExistingSixRoleRecordRetainsConstructionDeconstructionWithAndEquality() {
		WidgetTheme original = WidgetTheme.Default;
		WidgetTheme copy = new( original.Normal, original.Focused, original.Disabled, original.Selected, original.Error, original.Border );
		var ( normal, focused, disabled, selected, error, border ) = copy;
		Assert.Equal( original, copy );
		Assert.Equal( original, copy with { } );
		Assert.Equal( ( original.Normal, original.Focused, original.Disabled, original.Selected, original.Error, original.Border ),
			( normal, focused, disabled, selected, error, border ) );
	}

	[Fact]
	public void PartStylesAreImmutableAndFallBackToLegacyRoles() {
		WidgetStyles first = WidgetStyles.Empty.With( WidgetStyleRole.TabActive, Blue );
		WidgetStyles second = first.With( WidgetStyleRole.TabActive, Red );
		WidgetTheme theme = WidgetTheme.Default with { Parts = first };
		Assert.Equal( Blue, theme.Resolve( WidgetStyleRole.TabActive ) );
		Assert.Equal( Red, ( theme with { Parts = second } ).Resolve( WidgetStyleRole.TabActive ) );
		Assert.Equal( WidgetTheme.Default.Border, theme.Resolve( WidgetStyleRole.DialogBorder ) );
		Assert.Equal( WidgetTheme.Default.Disabled, theme.Resolve( WidgetStyleRole.PopupDisabled ) );
		Assert.Equal( WidgetTheme.Default.Selected, theme.Resolve( WidgetStyleRole.TextSelection ) );
		Assert.Equal( WidgetTheme.Default, theme with { Parts = WidgetStyles.Empty } );
		Assert.Equal( first, WidgetStyles.Empty.With( WidgetStyleRole.TabActive, Blue ) );
		Assert.Equal( WidgetTheme.Default.Normal, ( theme with { Parts = first.Without( WidgetStyleRole.TabActive ) } ).Resolve( WidgetStyleRole.TabActive ) );
	}

	[Fact]
	public void NearestScopedOverrideAffectsOnlyItsSubtreeAndBothColorAxes() {
		CursesScreen screen = new( 5, 3 );
		using WidgetHost host = new( screen, WidgetTheme.Default with { Parts = WidgetStyles.Empty.With( WidgetStyleRole.Text, Red ) } );
		WidgetStack root = new( "root", WidgetOrientation.Vertical );
		Probe first = new( "first" ); Probe second = new( "second" );
		first.Styles = WidgetStyles.Empty.With( WidgetStyleRole.Text, Blue );
		root.Add( first ); root.Add( second ); host.Attach( root ); host.Arrange( screen.Bounds ); host.Render();
		Assert.Equal( Blue, screen.StandardWindow.GetCell( first.Bounds.Row, 0 ).Style );
		Assert.Equal( Red, screen.StandardWindow.GetCell( second.Bounds.Row, 0 ).Style );
		first.Styles = WidgetStyles.Empty; host.Render();
		Assert.Equal( Red, screen.StandardWindow.GetCell( first.Bounds.Row, 0 ).Style );
	}

	[Fact]
	public void ThemeSwitchRepaintsWithoutChangingFocusOrQueuedActions() {
		CursesScreen screen = new( 5, 2 ); using WidgetHost host = new( screen, WidgetTheme.Default with { Parts = WidgetStyles.Empty.With( WidgetStyleRole.Text, Red ) } );
		Probe control = new( "control" ); host.Attach( control ); control.Arrange( screen.Bounds ); host.Focus( control ); host.Render();
		Assert.Equal( Red, screen.StandardWindow.GetCell( 0, 0 ).Style );
		host.SetTheme( WidgetTheme.Default with { Parts = WidgetStyles.Empty.With( WidgetStyleRole.Text, Blue ) } ); host.Render();
		Assert.Same( control, host.FocusedWidget ); Assert.Equal( Blue, screen.StandardWindow.GetCell( 0, 0 ).Style );
		Assert.Empty( host.DrainActions() );
	}

	[Fact]
	public void CoreControlsStyleOwnedBlankCellsAndSelectedRows() {
		WidgetStyles parts = WidgetStyles.Empty.With( WidgetStyleRole.Surface, Red ).With( WidgetStyleRole.Text, Blue )
			.With( WidgetStyleRole.ListSurface, Red ).With( WidgetStyleRole.ListSelected, Blue );
		CursesScreen screen = new( 12, 5 ); using WidgetHost host = new( screen, WidgetTheme.Default with { Parts = parts } );
		WidgetLabel label = new( "label", "Hi" ); WidgetList list = new( "list", WidgetSelectionMode.Single );
		list.SetItems( [ new WidgetListItem( "one", "One" ) ] );
		WidgetStack root = new( "root", WidgetOrientation.Vertical ); root.Add( label ); root.Add( list ); host.Attach( root );
		label.Arrange( new CursesRectangle( 0, 0, 1, 12 ) ); list.Arrange( new CursesRectangle( 1, 0, 3, 12 ) );
		host.Render();
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 0, 0 ).Style );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 0, 11 ).Style );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 3, 11 ).Style );
		host.Focus( list ); host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero ); list.OnRender( screen, host.Theme, false );
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 1, 11 ).Style );
	}

	[Fact]
	public void EditorAndFieldUseDistinctSurfaceTextAndSelectionStyles() {
		WidgetStyles parts = WidgetStyles.Empty.With( WidgetStyleRole.EditorSurface, Red ).With( WidgetStyleRole.EditorText, Blue )
			.With( WidgetStyleRole.TextSelectionInactive, Red ).With( WidgetStyleRole.FieldSurface, Blue )
			.With( WidgetStyleRole.FieldText, Red ).With( WidgetStyleRole.FieldSelection, Blue );
		CursesScreen screen = new( 10, 3 ); using WidgetHost host = new( screen, WidgetTheme.Default with { Parts = parts } );
		WidgetTextInput field = new( "field", "ab" ); field.Arrange( new CursesRectangle( 0, 0, 1, 10 ) );
		field.SetSelection( 0, 1 ); field.OnRender( screen, host.Theme, false );
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 0, 0 ).Style );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 0, 1 ).Style );
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 0, 9 ).Style );
		using WidgetTextViewState view = new( new WidgetTextDocument( [ "界z".AsMemory() ] ) );
		WidgetTextEditor editor = new( "editor", view ); editor.Arrange( new CursesRectangle( 1, 0, 2, 10 ) );
		view.SetSelection( 0, 1 ); editor.OnRender( screen, host.Theme, false );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 1, 0 ).Style );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 1, 1 ).Style );
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 1, 2 ).Style );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 2, 9 ).Style );
	}

	[Fact]
	public void ActiveUnfocusedTabModifiedMarkerAndScrollbarPartsKeepIndependentStyles() {
		WidgetStyles parts = WidgetStyles.Empty.With( WidgetStyleRole.TabStrip, Red ).With( WidgetStyleRole.TabActive, Blue )
			.With( WidgetStyleRole.TabModified, Red ).With( WidgetStyleRole.TabClose, Red )
			.With( WidgetStyleRole.ScrollTrack, Blue ).With( WidgetStyleRole.ScrollThumb, Red );
		CursesScreen screen = new( 16, 3 ); WidgetTheme theme = WidgetTheme.Default with { Parts = parts };
		WidgetTabStrip tabs = new( "tabs" ); tabs.Arrange( new CursesRectangle( 0, 0, 1, 16 ) );
		tabs.SetTabs( [ new WidgetTab( "one", "File", IsClosable: true, IsModified: true ) ], "one" );
		tabs.OnRender( screen, theme, false );
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 0, 1 ).Style );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 0, 6 ).Style );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 0, 8 ).Style );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 0, 15 ).Style );
		WidgetScrollBar bar = new( "bar", WidgetScrollOrientation.Horizontal ); bar.Arrange( new CursesRectangle( 1, 0, 1, 10 ) );
		bar.SetRange( 100, 20, 0 ); bar.OnRender( screen, theme, false );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 1, 0 ).Style );
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 1, 9 ).Style );
	}

	[Fact]
	public void StatusActionsUseTheirOwnStyleWithinStyledSurface() {
		WidgetStyles parts = WidgetStyles.Empty.With( WidgetStyleRole.StatusSurface, Red )
			.With( WidgetStyleRole.StatusAction, Blue ).With( WidgetStyleRole.StatusField, Blue );
		CursesScreen screen = new( 12, 1 ); WidgetStatusBar bar = new( "bar" );
		bar.SetFields( [ new WidgetStatusField( "line", "Ln1", 10 ) { CommandId = "go" } ] );
		bar.Arrange( screen.Bounds ); bar.OnRender( screen, WidgetTheme.Default with { Parts = parts }, false );
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 0, 0 ).Style );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 0, 11 ).Style );
	}

	[Fact]
	public void MessageHistoryRepaintsEmptyRowsOnThemeSwitch() {
		CursesScreen screen = new( 8, 3 ); WidgetMessageHistory history = new( "history" ); history.Append( "Hi" );
		using WidgetHost host = new( screen, WidgetTheme.Default with { Parts = WidgetStyles.Empty.With( WidgetStyleRole.MessageSurface, Red ) } );
		host.Attach( history ); history.Arrange( screen.Bounds ); host.Render();
		Assert.Equal( Red, screen.StandardWindow.GetCell( 2, 7 ).Style );
		host.SetTheme( host.Theme with { Parts = WidgetStyles.Empty.With( WidgetStyleRole.MessageSurface, Blue ) } ); host.Render();
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 0, 0 ).Style );
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 2, 7 ).Style );
	}

	[Fact]
	public void OpaqueDialogPaddingUsesItsSurfaceAndClosePreservesBorrowedBackground() {
		CursesScreen screen = new( 20, 5 ); screen.StandardWindow.FillRectangle( 0, 0, 5, 20, CursesCell.Blank( Red ) );
		WidgetTheme theme = WidgetTheme.Default with { Parts = WidgetStyles.Empty.With( WidgetStyleRole.DialogSurface, Blue ) };
		using WidgetHost host = new( screen, theme ); WidgetMessageDialog dialog = WidgetMessageDialog.Ok( "ok", "Title", "Short" );
		host.ShowModal( dialog ); host.Render();
		Assert.Equal( Blue, host.GetPresentationWindow( dialog ).GetCell( 2, 18 ).Style );
		host.CloseModal( dialog, WidgetDialogOutcome.Cancel ); host.Render();
		Assert.Equal( Red, screen.StandardWindow.GetCell( 2, 18 ).Style );
	}

	private sealed class Probe( string id ) : Widget( id ) {
		public override bool IsFocusable => true;
		protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
			screen.StandardWindow.Move( this.Bounds.Row, this.Bounds.Column );
			screen.StandardWindow.Write( "X", this.Style( theme, WidgetStyleRole.Text ) );
		}
	}
}
