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
	private static readonly CursesStyle Green = new( CursesColor.Indexed( 0 ), CursesColor.Indexed( 2 ) );
	private static readonly CursesStyle Gray = new( CursesColor.Indexed( 15 ), CursesColor.Indexed( 8 ) );

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
	public void EditorCurrentLineIsOptInFullWidthAndSelectionWins() {
		WidgetStyles parts = WidgetStyles.Empty.With( WidgetStyleRole.EditorSurface, Red ).With( WidgetStyleRole.EditorText, Blue )
			.With( WidgetStyleRole.EditorCurrentLine, Gray ).With( WidgetStyleRole.TextSelection, Green );
		WidgetTheme theme = WidgetTheme.Default with { Parts = parts };
		CursesScreen screen = new( 8, 3 );
		using WidgetTextViewState defaultView = new( new WidgetTextDocument( [ "top\nmiddle\nlast".AsMemory() ] ) );
		defaultView.SetSelection( 4, 4 );
		WidgetTextEditor defaultEditor = new( "default", defaultView );
		defaultEditor.Arrange( screen.Bounds ); defaultEditor.OnRender( screen, theme, true );
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 1, 0 ).Style );
		Assert.Equal( CursesStyle.Default, screen.StandardWindow.GetCell( 1, 7 ).Style );

		using WidgetTextViewState view = new( new WidgetTextDocument( [ "top\nmiddle\nlast".AsMemory() ] ) );
		view.SetSelection( 4, 4 );
		WidgetTextEditor editor = new( "highlighted", view, new WidgetEditorOptions { HighlightCurrentLine = true } );
		editor.Arrange( screen.Bounds ); editor.OnRender( screen, theme, true );
		Assert.Equal( CursesStyle.Default, screen.StandardWindow.GetCell( 0, 7 ).Style );
		Assert.Equal( Gray, screen.StandardWindow.GetCell( 1, 0 ).Style );
		Assert.Equal( Gray, screen.StandardWindow.GetCell( 1, 7 ).Style );
		view.SetSelection( 4, 6 ); editor.OnRender( screen, theme, true );
		Assert.Equal( Green, screen.StandardWindow.GetCell( 1, 0 ).Style );
		Assert.Equal( Green, screen.StandardWindow.GetCell( 1, 1 ).Style );
		Assert.Equal( Gray, screen.StandardWindow.GetCell( 1, 2 ).Style );
		Assert.Equal( Gray, screen.StandardWindow.GetCell( 1, 7 ).Style );
	}

	[Fact]
	public void EditorCurrentLogicalLineCoversEveryWrappedVisualRowAndFollowsCaret() {
		WidgetStyles parts = WidgetStyles.Empty.With( WidgetStyleRole.EditorSurface, Red ).With( WidgetStyleRole.EditorText, Blue )
			.With( WidgetStyleRole.EditorCurrentLine, Gray );
		WidgetTheme theme = WidgetTheme.Default with { Parts = parts };
		CursesScreen screen = new( 4, 4 );
		using WidgetTextViewState view = new( new WidgetTextDocument( [ "abcdefgh\nz".AsMemory() ] ) );
		view.SetSelection( 1, 1 );
		WidgetTextEditor editor = new( "editor", view, new WidgetEditorOptions {
			HighlightCurrentLine = true, WrapMode = CursesTextWrapMode.TextElement
		} );
		editor.Arrange( screen.Bounds ); editor.OnRender( screen, theme, true );
		Assert.Equal( Gray, screen.StandardWindow.GetCell( 0, 3 ).Style );
		Assert.Equal( Gray, screen.StandardWindow.GetCell( 1, 3 ).Style );
		Assert.Equal( CursesStyle.Default, screen.StandardWindow.GetCell( 2, 3 ).Style );

		view.SetSelection( 9, 9 ); editor.OnRender( screen, theme, true );
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 0, 3 ).Style );
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 1, 3 ).Style );
		Assert.Equal( Gray, screen.StandardWindow.GetCell( 2, 3 ).Style );
	}

	[Fact]
	public void EditorLineNumberGutterApiDefaultsOffAndCanOptIn() {
		WidgetEditorOptions defaults = new();
		WidgetEditorOptions enabled = defaults with { ShowLineNumbers = true };

		Assert.False( defaults.ShowLineNumbers );
		Assert.True( enabled.ShowLineNumbers );
		Assert.Equal( (int)WidgetStyleRole.EditorCurrentLine + 1, (int)WidgetStyleRole.EditorGutter );
		Assert.Equal( (int)WidgetStyleRole.EditorGutter + 1, (int)WidgetStyleRole.EditorGutterCurrentLine );
	}

	[Fact]
	public void CurrentLineGutterStyleCoversWrappedRows() {
		WidgetStyles parts = WidgetStyles.Empty.With( WidgetStyleRole.EditorSurface, Red ).With( WidgetStyleRole.EditorText, Blue )
			.With( WidgetStyleRole.EditorGutter, Red ).With( WidgetStyleRole.EditorGutterCurrentLine, Gray );
		CursesScreen screen = new( 6, 3 ); using WidgetTextViewState view = new( new WidgetTextDocument( [ "abcdef\nz".AsMemory() ] ) );
		WidgetTextEditor editor = new( "editor", view, new WidgetEditorOptions { ShowLineNumbers = true, WrapMode = CursesTextWrapMode.TextElement } );
		editor.Arrange( screen.Bounds ); editor.OnRender( screen, WidgetTheme.Default with { Parts = parts }, true );

		Assert.Equal( Gray, screen.StandardWindow.GetCell( 0, 0 ).Style );
		Assert.Equal( Gray, screen.StandardWindow.GetCell( 1, 0 ).Style );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 2, 0 ).Style );
	}

	[Fact]
	public void CurrentLineGutterStyleIsIndependentOfTextBand() {
		WidgetStyles parts = WidgetStyles.Empty.With( WidgetStyleRole.EditorSurface, Red ).With( WidgetStyleRole.EditorText, Blue )
			.With( WidgetStyleRole.EditorCurrentLine, Gray ).With( WidgetStyleRole.EditorGutter, Red ).With( WidgetStyleRole.EditorGutterCurrentLine, Green );
		CursesScreen screen = new( 10, 2 ); using WidgetTextViewState view = new( new WidgetTextDocument( [ "top\nbottom".AsMemory() ] ) ); view.SetSelection( 4, 4 );
		WidgetTextEditor editor = new( "editor", view, new WidgetEditorOptions { ShowLineNumbers = true, HighlightCurrentLine = false } );
		editor.Arrange( screen.Bounds ); editor.OnRender( screen, WidgetTheme.Default with { Parts = parts }, true );

		Assert.Equal( Green, screen.StandardWindow.GetCell( 1, 0 ).Style );
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 1, 2 ).Style );
	}

	[Fact]
	public void DisabledEditorUsesDisabledStyleAcrossGutter() {
		WidgetStyles parts = WidgetStyles.Empty.With( WidgetStyleRole.EditorSurface, Red ).With( WidgetStyleRole.EditorText, Blue )
			.With( WidgetStyleRole.EditorGutter, Green ).With( WidgetStyleRole.EditorGutterCurrentLine, Gray ).With( WidgetStyleRole.Disabled, Red );
		CursesScreen screen = new( 10, 2 ); using WidgetTextViewState view = new( new WidgetTextDocument( [ "top\nbottom".AsMemory() ] ) );
		WidgetTextEditor editor = new( "editor", view, new WidgetEditorOptions { ShowLineNumbers = true } ) { IsEnabled = false };
		editor.Arrange( screen.Bounds ); editor.OnRender( screen, WidgetTheme.Default with { Parts = parts }, false );

		Assert.Equal( "1", screen.StandardWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 0, 0 ).Style );
		Assert.True( screen.StandardWindow.GetCell( 0, 1 ).IsBlank );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 0, 1 ).Style );
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
	public void DropDownStylesFramedPopupBorderRowsAndEmptyCells() {
		WidgetStyles parts = WidgetStyles.Empty.With( WidgetStyleRole.PopupSurface, Red ).With( WidgetStyleRole.PopupBorder, Blue )
			.With( WidgetStyleRole.PopupFocused, Blue ).With( WidgetStyleRole.PopupDisabled, Red );
		CursesScreen screen = new( 15, 7 ); using WidgetHost host = new( screen, WidgetTheme.Default with { Parts = parts } );
		WidgetDropDown drop = new( "drop", [ new( "a", "Alpha" ), new( "b", "Beta", false ) ], "a" );
		host.Attach( drop ); drop.Arrange( new CursesRectangle( 0, 0, 1, 15 ) ); host.Focus( drop ); drop.Open(); host.Render();
		CursesWindow popup = host.GetPresentationWindow( drop );
		Assert.Equal( Blue, popup.GetCell( 0, 0 ).Style );
		Assert.Equal( Blue, popup.GetCell( 1, 12 ).Style );
		Assert.Equal( Red, popup.GetCell( 2, 12 ).Style );
		Assert.Equal( Blue, popup.GetCell( 1, 14 ).Style );
	}

	[Fact]
	public void MenuSelectionCoversShortcutAndCheckWhileUnselectedPartsKeepTheirStyles() {
		WidgetStyles parts = WidgetStyles.Empty.With( WidgetStyleRole.PopupSurface, Red ).With( WidgetStyleRole.PopupBorder, Blue )
			.With( WidgetStyleRole.PopupFocused, Red ).With( WidgetStyleRole.PopupShortcut, Blue ).With( WidgetStyleRole.PopupCheck, Blue );
		CursesScreen screen = new( 24, 6 ); using WidgetHost host = new( screen, WidgetTheme.Default with { Parts = parts } );
		WidgetPopupMenu menu = new( "menu", [ WidgetMenuItem.Checkable( "show", "Show", "show", true,
			shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 's' ) ), WidgetMenuItem.Command( "other", "Other", "other" ) ] );
		host.Attach( menu ); menu.Arrange( screen.Bounds ); host.Focus( menu ); menu.Open(); host.Render();
		CursesWindow popup = host.GetPresentationWindow( menu );
		Assert.Equal( Blue, popup.GetCell( 0, 0 ).Style );
		Assert.Equal( Red, popup.GetCell( 1, 1 ).Style );
		Assert.Equal( Red, popup.GetCell( 1, 7 ).Style );
		Assert.Equal( Red, popup.GetCell( 1, 18 ).Style );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.Zero ); host.Render();
		Assert.Equal( Blue, popup.GetCell( 1, 1 ).Style ); Assert.Equal( Blue, popup.GetCell( 1, 18 ).Style );
	}

	[Fact]
	public void DialogCaptionAndBorderCanUseDifferentStyles() {
		WidgetStyles parts = WidgetStyles.Empty.With( WidgetStyleRole.DialogSurface, Red ).With( WidgetStyleRole.DialogBorder, Red )
			.With( WidgetStyleRole.DialogTitle, Blue );
		CursesScreen screen = new( 30, 5 ); using WidgetHost host = new( screen, WidgetTheme.Default with { Parts = parts } );
		WidgetMessageDialog dialog = WidgetMessageDialog.Ok( "ok", "Title", "Body" );
		host.ShowModal( dialog ); host.Render(); CursesWindow panel = host.GetPresentationWindow( dialog );
		Assert.Equal( Red, panel.GetCell( 0, 0 ).Style );
		Assert.Equal( Blue, panel.GetCell( 0, 3 ).Style );
	}

	[Fact]
	public void PaletteOnMonochromeProfileKeepsNonColorFocusSelectionAndErrorCues() {
		WidgetTheme theme = WidgetPalette.Create( default );
		Assert.Equal( CursesColorKind.Default, theme.Resolve( WidgetStyleRole.TabActive ).Foreground.Kind );
		Assert.Equal( CursesColorKind.Default, theme.Resolve( WidgetStyleRole.EditorSurface ).Background.Kind );
		Assert.Equal( CursesStyle.Default, theme.Resolve( WidgetStyleRole.EditorSurface ) );
		WidgetTheme attributesOnly = WidgetPalette.Create( Capabilities( 0, false, false, false, CursesTextAttributes.Reverse | CursesTextAttributes.Bold ) );
		Assert.Equal( CursesColorKind.Default, attributesOnly.Focused.Foreground.Kind );
		Assert.NotEqual( attributesOnly.Normal, attributesOnly.Focused );
		Assert.NotEqual( attributesOnly.Normal, attributesOnly.Error );
	}

	[Fact]
	public void PaletteUsesOnlyAdvertisedColorKindsAndAxes() {
		CursesPresentationCapabilities indexed = Capabilities( 8, false, true, false, CursesTextAttributes.Reverse | CursesTextAttributes.Bold );
		WidgetTheme selected = WidgetPalette.Create( indexed );
		Assert.Equal( CursesColorKind.Indexed, selected.Resolve( WidgetStyleRole.TabActive ).Foreground.Kind );
		Assert.True( selected.Resolve( WidgetStyleRole.TabActive ).Foreground.Index < 8 );
		Assert.Equal( CursesColorKind.Default, selected.Resolve( WidgetStyleRole.TabActive ).Background.Kind );
		CursesPresentationCapabilities rgb = Capabilities( 0, true, false, true, CursesTextAttributes.Reverse );
		WidgetTheme direct = WidgetPalette.Create( rgb, WidgetPaletteVariant.Light );
		Assert.Equal( CursesColorKind.Default, direct.Resolve( WidgetStyleRole.Text ).Foreground.Kind );
		Assert.Equal( CursesColorKind.Rgb, direct.Resolve( WidgetStyleRole.TabActive ).Background.Kind );
		Assert.Equal( CursesColorKind.Rgb, direct.Resolve( WidgetStyleRole.EditorCurrentLine ).Background.Kind );
		Assert.NotEqual( direct.Resolve( WidgetStyleRole.EditorSurface ), direct.Resolve( WidgetStyleRole.EditorCurrentLine ) );
		WidgetTheme indexedLight = WidgetPalette.Create( Capabilities( 16, false, true, true, CursesTextAttributes.Reverse ), WidgetPaletteVariant.Light );
		Assert.Equal( CursesColor.Indexed( 8 ), indexedLight.Resolve( WidgetStyleRole.EditorCurrentLine ).Background );
		Assert.NotEqual( indexedLight.Resolve( WidgetStyleRole.EditorSurface ), indexedLight.Resolve( WidgetStyleRole.EditorCurrentLine ) );
		foreach ( WidgetPaletteVariant variant in Enum.GetValues<WidgetPaletteVariant>() ) {
			WidgetTheme limited = WidgetPalette.Create( Capabilities( 8, false, true, true, CursesTextAttributes.Reverse ), variant );
			Assert.NotEqual( limited.Resolve( WidgetStyleRole.EditorSurface ), limited.Resolve( WidgetStyleRole.EditorCurrentLine ) );
		}
	}

	[Fact]
	public void EditorLineNumberGutterPaletteIsCapabilityAware() {
		WidgetTheme direct = WidgetPalette.Create( Capabilities( 0, true, true, true,
			CursesTextAttributes.Bold | CursesTextAttributes.Dim | CursesTextAttributes.Reverse ) );
		CursesStyle directGutter = direct.Resolve( WidgetStyleRole.EditorGutter );
		CursesStyle directCurrent = direct.Resolve( WidgetStyleRole.EditorGutterCurrentLine );
		Assert.Equal( CursesColorKind.Rgb, directGutter.Foreground.Kind );
		Assert.NotEqual( direct.Resolve( WidgetStyleRole.EditorText ).Foreground, directGutter.Foreground );
		Assert.Equal( direct.Resolve( WidgetStyleRole.EditorSurface ).Background, directGutter.Background );
		Assert.Equal( direct.Resolve( WidgetStyleRole.EditorCurrentLine ).Background, directCurrent.Background );

		WidgetTheme indexed = WidgetPalette.Create( Capabilities( 8, false, true, true,
			CursesTextAttributes.Bold | CursesTextAttributes.Reverse ) );
		foreach ( WidgetStyleRole role in new[] { WidgetStyleRole.EditorGutter, WidgetStyleRole.EditorGutterCurrentLine } ) {
			CursesStyle style = indexed.Resolve( role );
			Assert.Equal( CursesColorKind.Indexed, style.Foreground.Kind );
			Assert.Equal( CursesColorKind.Indexed, style.Background.Kind );
			Assert.InRange( style.Foreground.Index.GetValueOrDefault(), 0, 7 );
			Assert.InRange( style.Background.Index.GetValueOrDefault(), 0, 7 );
		}

		WidgetTheme backgroundOnly = WidgetPalette.Create( Capabilities( 16, false, false, true, CursesTextAttributes.None ) );
		foreach ( WidgetStyleRole role in new[] { WidgetStyleRole.EditorGutter, WidgetStyleRole.EditorGutterCurrentLine } ) {
			CursesStyle style = backgroundOnly.Resolve( role );
			Assert.Equal( CursesColorKind.Default, style.Foreground.Kind );
			Assert.Equal( CursesColorKind.Indexed, style.Background.Kind );
			Assert.Equal( CursesTextAttributes.None, style.Attributes );
		}
	}

	private static CursesPresentationCapabilities Capabilities( int colors, bool rgb, bool foreground, bool background, CursesTextAttributes attributes ) {
		var ctor = typeof( CursesPresentationCapabilities ).GetConstructors( System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance )
			.Single( c => c.GetParameters().Length == 11 );
		return (CursesPresentationCapabilities)ctor.Invoke( [ colors, rgb, foreground, background, true, attributes,
			CursesTextAttributes.None, false, false, false, false ] );
	}

	[Fact]
	public void DisabledSelectionUsesDisabledStyleAndDialogActionsRespectTheirStates() {
		WidgetStyles parts = WidgetStyles.Empty.With( WidgetStyleRole.Disabled, Red ).With( WidgetStyleRole.FieldSelection, Blue )
			.With( WidgetStyleRole.TextSelection, Blue ).With( WidgetStyleRole.DialogDisabledAction, Red )
			.With( WidgetStyleRole.DialogDefaultAction, Blue ).With( WidgetStyleRole.DialogAction, CursesStyle.Default );
		WidgetTheme theme = WidgetTheme.Default with { Parts = parts }; CursesScreen screen = new( 32, 7 );
		WidgetTextInput field = new( "field", "ab" ) { IsEnabled = false };
		field.SetSelection( 0, 1 ); field.Arrange( new CursesRectangle( 0, 0, 1, 32 ) ); field.OnRender( screen, theme, true );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 0, 0 ).Style );
		WidgetFormDialog dialog = new( "form", "Actions", [ new WidgetDialogAction( "save", "Save", WidgetDialogOutcome.Save ) { IsDefault = true },
			new WidgetDialogAction( "cancel", "Cancel", WidgetDialogOutcome.Cancel ) { IsEnabled = false } ] );
		dialog.Arrange( new CursesRectangle( 1, 0, 6, 32 ) ); dialog.OnRender( screen, theme, false );
		Assert.Equal( Blue, screen.StandardWindow.GetCell( 5, 2 ).Style );
		Assert.Equal( Red, screen.StandardWindow.GetCell( 5, 9 ).Style );
	}

	[Fact]
	public void RepeatedModalThemeSwitchesReusePresentationStorage() {
		CursesScreen screen = new( 80, 25 ); using WidgetHost host = new( screen, WidgetTheme.Default );
		WidgetMessageDialog dialog = WidgetMessageDialog.Ok( "modal", "Title", "Body" ); host.ShowModal( dialog ); host.Render();
		WidgetTheme dark = WidgetPalette.Create( default ); WidgetTheme light = WidgetPalette.Create( default, WidgetPaletteVariant.Light );
		long before = GC.GetAllocatedBytesForCurrentThread();
		for ( int iteration = 0; iteration < 100; iteration++ ) {
			host.SetTheme( iteration % 2 == 0 ? dark : light );
			host.Render();
		}
		long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.InRange( allocated, 0, 100_000_000 );
		Assert.Same( dialog, host.FocusedWidget );
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
