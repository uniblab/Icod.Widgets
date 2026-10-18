/*
	Icod.Widgets.Gallery.Sample
	Interactive control gallery for Icod.Widgets.
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

namespace Icod.Widgets.Gallery.Sample;
using System.Globalization;
using Icod.DCurses;
using Icod.Widgets.Samples;

internal static class Program {
	private static async Task<int> Main( string[] args ) {
		if ( !args.Contains( "--smoke", StringComparer.Ordinal ) ) return await WidgetSampleApplication.RunAsync( screen => new Gallery( screen ), ( app, capabilities ) => ( (Gallery)app ).UseCapabilities( capabilities ) );
		using Gallery app = new( new CursesScreen( 80, 30 ) );
		app.Arrange(); app.Host.Focus( app.Name ); app.Input( WidgetInput.Paste( "Ada\tLovelace" ) );
		if ( app.Name.Value != "Ada Lovelace" ) return 1;
		app.Name.SetSelection( 0, 3 ); app.Input( WidgetInput.TextInput( new System.Text.Rune( 'X' ) ) );
		if ( app.Name.Value != "X Lovelace" || WidgetCommandResult.Executed != app.Host.ExecuteEditCommand( WidgetEditCommand.Undo, app.Host.CurrentTime + TimeSpan.FromMilliseconds( 1 ) ) || app.Name.Value != "Ada Lovelace" ) return 1;
		app.Host.DrainActions();
		for ( int palette = 0; palette < 3; palette++ ) {
			for ( int page = 0; page < 7; page++ ) { app.SelectPage( page ); app.Draw(); }
			Widget? focusBeforeThemeSwitch = app.Host.FocusedWidget;
			app.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 't' ) ) ); app.Draw();
			if ( !ReferenceEquals( focusBeforeThemeSwitch, app.Host.FocusedWidget ) ) return 3;
		}
		if ( app.PaletteIndex != 0 ) return 3;
		if ( !app.SmokeMenus() ) return 5;
		if ( !app.SmokeDialogs() ) return 6;
		app.SelectPage( 6 );
		if ( app.Host.FocusedWidget is not WidgetTabStrip ) return 4;
		app.Input( WidgetInput.FunctionKey( 6 ) );
		if ( app.Host.FocusedWidget is not WidgetTextEditor ) return 4;
		app.Input( WidgetInput.FunctionKey( 6 ) );
		if ( app.Host.FocusedWidget is not WidgetStatusBar ) return 4;
		app.Input( WidgetInput.FunctionKey( 6 ) );
		if ( app.Host.FocusedWidget is not WidgetTabStrip ) return 4;
		app.Input( WidgetInput.FunctionKey( 6, WidgetInputModifiers.Shift ) );
		if ( app.Host.FocusedWidget is not WidgetStatusBar ) return 4;
		app.SelectPage( 4 ); app.OpenFiles( true ); await app.PollAsync( true ); app.Draw(); app.Input( WidgetInput.Key( WidgetKey.Escape ) );
		app.SelectPage( 3 ); app.OpenWizard(); app.Input( WidgetInput.Key( WidgetKey.Enter ) ); app.Draw(); app.Input( WidgetInput.Key( WidgetKey.Enter ) ); app.Input( WidgetInput.Key( WidgetKey.Escape ) );
		if ( app.Host.ModalDepth != 0 ) return 2;
		Console.WriteLine( "PASS seven gallery pages, theme cycle, menu navigation/hover/modal, generic dialog mnemonic/default/pointer actions, provider-backed picker cancellation, wizard validation, rendering" ); return 0;
	}
	private sealed class Gallery : WidgetSampleApplication {
		private readonly List<Widget[]> pages = [];
		private int page;
		private int dialogId;
		private WidgetWizardDialog? wizard;
		private bool rejected;
		private WidgetDialogOutcome? lastDialogOutcome;
		private CursesPresentationCapabilities capabilities;
		private int paletteIndex;
		private bool highlightMenuHover;
		private readonly WidgetTextViewState menuView = new( new WidgetTextDocument( [ "Menus overlay this document without moving it.\nTry Alt+F, Up/Down, Left/Right, then Esc.\nF8 toggles theme hover; Enter still activates keyboard selection.".AsMemory() ] ) );
		private WidgetMenuBar Menu => this.pages[ 2 ].OfType<WidgetMenuBar>().Single();
		private WidgetTextEditor MenuEditor => this.pages[ 2 ].OfType<WidgetTextEditor>().Single();
		private readonly WidgetTextViewState workspaceView = new( new WidgetTextDocument( [ "Colors and styles across tabs, editor selections, scrollbars, and status.\nSecond line with 界 wide text.".AsMemory() ] ) );
		internal int PaletteIndex => this.paletteIndex;
		internal WidgetTextInput Name { get; } = new( "name" );
		protected override string Heading => "Icod.Widgets gallery — " + new[] { "Forms", "Lists / drop-down", "Menus", "Dialogs / wizard", "Folders / files", "Game UI", "Workspace color" }[ this.page ] + " — " + new[] { "dark", "light", "default" }[ this.paletteIndex ];
		protected override string Keys => this.page == 2 ? "Alt+F/E/V/H menus | Ctrl+T theme | F8 hover | F6 focus | Ctrl+Q exit" : this.page == 6 ? "Ctrl+N/P pages | Ctrl+T theme | F6 / Shift+F6 focus | Enter activate" : "Ctrl+N/P pages | Ctrl+T theme | Tab focus | Enter activate";
		internal Gallery( CursesScreen screen ) : base( screen ) {
			WidgetList single = new( "single", WidgetSelectionMode.Single ); single.SetItems( Items() );
			WidgetList multiple = new( "multiple", WidgetSelectionMode.Multiple ); multiple.SetItems( Items() );
			WidgetMenuShortcut runShortcut = new( WidgetKey.Character, WidgetInputModifiers.Control, 'r' );
			WidgetMenuItem[] menuItems = [
				WidgetMenuItem.Submenu( "file", "File", [ WidgetMenuItem.Command( "run", "Run command", "menu.run", 'r', shortcut: runShortcut ), WidgetMenuItem.Command( "disabled", "Disabled", "disabled", 'd', isEnabled: false ) ], 'f' ),
				WidgetMenuItem.Submenu( "edit", "Edit", [ WidgetMenuItem.Command( "undo", "Undo", "edit.undo", 'u', WidgetEditCommand.Undo, shortcut: new( WidgetKey.Character, WidgetInputModifiers.Control, 'z' ) ),
					WidgetMenuItem.Command( "redo", "Redo", "edit.redo", 'r', WidgetEditCommand.Redo, shortcut: new( WidgetKey.Character, WidgetInputModifiers.Control, 'y' ) ),
					WidgetMenuItem.Command( "select-all", "Select All", "edit.select", 'a', WidgetEditCommand.SelectAll, shortcut: new( WidgetKey.Character, WidgetInputModifiers.Control, 'a' ) ) ], 'e' ),
				WidgetMenuItem.Submenu( "view", "View", [ WidgetMenuItem.Checkable( "check", "Checked option", "menu.check", 'c' ), WidgetMenuItem.Separator( "separator" ), WidgetMenuItem.Submenu( "sub", "Submenu", [ WidgetMenuItem.Command( "nested", "Nested action", "menu.nested", 'n' ) ], 's' ) ], 'v' ),
				WidgetMenuItem.Submenu( "help-menu", "Help", [ WidgetMenuItem.Command( "about", "About", "menu.about", 'a' ) ], 'h' )
			];
			WidgetInventoryList inventory = new( "inventory" ); inventory.SetItems( Items() );
			WidgetMessageHistory history = new( "history" ); history.Append( "The library receives items and messages from the application." ); history.Append( "No game rules live in Icod.Widgets." );
			WidgetStatusBar status = new( "status" ); status.SetFields( [ new( "hp", "HP 12/20", 10 ), new( "depth", "Depth 3", 5 ) ] );
			WidgetTabStrip tabs = new( "workspace-tabs" ); tabs.SetTabs( [ new( "first", "First", IsModified: true ), new( "second", "Second" ) ], "first" );
			WidgetScrollBar scrollbar = new( "workspace-scroll", WidgetScrollOrientation.Horizontal ); scrollbar.SetRange( 100, 20, 30 );
			WidgetStatusBar workspaceStatus = new( "workspace-status" ); workspaceStatus.SetFields( [ new( "line", "Ln 2, Col 8", 10 ), new( "mode", "Go to line", 5 ) { CommandId = "workspace.goto" } ] );
			this.pages.Add( [ new WidgetLabel( "form-hint", "Name (Unicode, selection, paste, undo), checkbox, radio, button" ), this.Name, new WidgetCheckBox( "enabled", "Enabled" ), new WidgetRadioGroup( "radio", [ "Red", "Green", "Blue" ] ), new WidgetButton( "accept", "Accept form", "form.accept" ), new WidgetScrollIndicator( "scroll" ) { Total = 100, Offset = 40, Visible = 20 } ] );
			this.pages.Add( [ new WidgetLabel( "list-hint", "Single: Enter commits | Multi: Space toggles, Ctrl+A all, Shift+arrows range" ), single, multiple, new WidgetDropDown( "drop", Items(), "potion" ) ] );
			this.pages.Add( [ new WidgetLabel( "menu-hint", "Hover off initially | Ctrl+R runs a command | Help > About shows a modal" ), new WidgetMenuBar( "bar", menuItems ), new WidgetTextEditor( "menu-editor", this.menuView ) ] );
			this.pages.Add( [ Button( "ok", "OK", "dialog.ok" ), Button( "yesno", "Yes / Don't Save / Cancel", "dialog.yesno" ), Button( "okcancel", "OK / Cancel", "dialog.okcancel" ), Button( "wizard", "Previous / Next / Cancel", "dialog.wizard" ), Button( "prompt", "Text prompt", "dialog.text" ), Button( "number", "Number prompt (0 to 100)", "dialog.number" ) ] );
			this.pages.Add( [ Button( "folder", "Select folder", "picker.folder" ), Button( "file", "Select one existing file", "picker.file" ), Button( "files", "Select multiple existing files", "picker.files" ) ] );
			this.pages.Add( [ inventory, history, status, Button( "help", "Show help", "game.help" ) ] );
			this.pages.Add( [ tabs, new WidgetTextEditor( "workspace-editor", this.workspaceView ), scrollbar, workspaceStatus ] );
			foreach ( Widget[] widgets in this.pages ) foreach ( Widget widget in widgets ) this.Host.Attach( widget );
			this.SelectPage( 0 );
		}
		private static WidgetButton Button( string id, string text, string command ) => new( id, text, command );
		internal void UseCapabilities( CursesPresentationCapabilities capabilities ) { this.capabilities = capabilities; this.ApplyPalette(); }
		private void ApplyPalette() {
			WidgetTheme theme = this.paletteIndex == 2 ? WidgetTheme.Default : WidgetPalette.Create( this.capabilities, this.paletteIndex == 0 ? WidgetPaletteVariant.Dark : WidgetPaletteVariant.Light );
			this.Host.SetTheme( theme with { HighlightMenuHover = this.highlightMenuHover } );
		}
		private static WidgetListItem[] Items() => [ new( "potion", "Healing potion" ), new( "sword", "Sword 界" ), new( "disabled", "Unavailable item", false ), new( "armour", "Armour" ), new( "scroll", "Scroll á" ), new( "key", "Key" ) ];
		internal void SelectPage( int next ) {
			this.page = next;
			for ( int index = 0; index < this.pages.Count; index++ ) foreach ( Widget widget in this.pages[ index ] ) widget.IsVisible = index == next;
			this.Arrange(); Widget? focus = next == 2 ? this.MenuEditor : this.pages[ next ].FirstOrDefault( widget => widget.IsFocusable ); if ( focus is not null ) this.Host.Focus( focus );
		}
		internal override void Arrange() {
			CursesScreen screen = this.Host.Screen; int available = Math.Max( 0, screen.Rows - 4 ); int row = Math.Min( 2, screen.Rows );
			Widget[] widgets = this.pages[ this.page ]; int growing = widgets.Count( widget => widget is not WidgetMenuBar && widget is WidgetList or WidgetPopupMenu or WidgetMessageHistory or WidgetTextEditor );
			int fixedCount = widgets.Length - growing; int growSize = growing == 0 ? 1 : Math.Max( 1, ( available - fixedCount ) / growing );
			foreach ( Widget widget in widgets ) { int rows = Math.Min( Math.Max( 0, screen.Rows - 2 - row ), widget is not WidgetMenuBar && widget is WidgetList or WidgetPopupMenu or WidgetMessageHistory or WidgetTextEditor ? growSize : 1 ); widget.Arrange( new CursesRectangle( row, 0, rows, screen.Columns ) ); row += rows; }
			this.ArrangeDialog();
		}
		private void ArrangeDialog() {
			if ( this.Dialog is not { IsOpen: true } dialog ) return;
			CursesScreen screen = this.Host.Screen;
			if ( dialog.Id.StartsWith( "menu-about.", StringComparison.Ordinal ) ) {
				int columns = Math.Min( 56, screen.Columns ); int rows = Math.Min( 7, screen.Rows );
				dialog.Arrange( new( ( screen.Rows - rows ) / 2, ( screen.Columns - columns ) / 2, rows, columns ) );
			} else dialog.Arrange( screen.Bounds );
		}
		protected override bool OnShortcut( WidgetInput input ) {
			if ( this.page == 2 && input.Kind == WidgetInputKind.Key && input.KeyCode == WidgetKey.Function ) {
				if ( input.FunctionKeyNumber == 8 ) { this.highlightMenuHover = !this.highlightMenuHover; this.ApplyPalette(); this.Status = "Menu hover: " + ( this.highlightMenuHover ? "on" : "off" ); return true; }
				if ( input.FunctionKeyNumber == 6 ) { bool fromMenu = ReferenceEquals( this.Host.FocusedWidget, this.Menu ); this.Menu.Close(); this.Host.Focus( fromMenu ? this.MenuEditor : this.Menu ); return true; }
			}
			if ( this.page == 6 && input.Kind == WidgetInputKind.Key && input.KeyCode == WidgetKey.Function && input.FunctionKeyNumber == 6 ) {
				Widget[] targets = this.pages[ 6 ].Where( widget => widget.IsFocusable && widget is WidgetTabStrip or WidgetTextEditor or WidgetStatusBar ).ToArray();
				int current = Array.FindIndex( targets, widget => ReferenceEquals( widget, this.Host.FocusedWidget ) );
				if ( targets.Length > 0 ) this.Host.Focus( targets[ current < 0 ? 0 : ( current + ( ( input.Modifiers & WidgetInputModifiers.Shift ) != 0 ? targets.Length - 1 : 1 ) ) % targets.Length ] );
				return true;
			}
			if ( ( input.Modifiers & WidgetInputModifiers.Control ) == 0 ) return false;
			if ( input.Character?.Value is 't' or 'T' ) { this.paletteIndex = ( this.paletteIndex + 1 ) % 3; this.ApplyPalette(); this.Status = "Theme: " + new[] { "dark", "light", "terminal default" }[ this.paletteIndex ]; return true; }
			if ( input.Character?.Value is 'n' or 'N' ) { this.SelectPage( ( this.page + 1 ) % 7 ); return true; }
			if ( input.Character?.Value is 'p' or 'P' ) { this.SelectPage( ( this.page + 6 ) % 7 ); return true; }
			return false;
		}
		internal void OpenFiles( bool multiple ) => this.Show( new WidgetFilePicker( "files." + ++this.dialogId, new LocalWidgetFileSystemProvider(), Environment.CurrentDirectory, multiple, [ new WidgetFileFilter( "Text / C#", [ ".txt", ".md", ".cs" ] ), WidgetFileFilter.AllFiles ] ) );
		internal void OpenWizard() { this.rejected = false; this.wizard = new( "wizard." + ++this.dialogId, "Application-validated wizard", [ "First page: first Next is rejected to demonstrate validation.", "Second page: Left returns; Esc closes." ] ); this.Show( this.wizard ); }
		private WidgetMessageDialog CreateDecisionDialog( string id ) => new( id, "Generic action dialog", "Choose by mnemonic, navigation, default Enter, or pointer.", [
			new( "yes", "Yes", WidgetDialogOutcome.Yes ) { Mnemonic = 'Y' },
			new( "no", "Don't Save", WidgetDialogOutcome.No ) { Mnemonic = 'N' },
			new( "cancel", "Cancel", WidgetDialogOutcome.Cancel ) { Mnemonic = 'C', IsDefault = true, IsCancel = true }
		] ) { Styles = WidgetStyles.Empty
			.With( WidgetStyleRole.DialogSurface, this.Host.Theme.Resolve( WidgetStyleRole.Error ) )
			.With( WidgetStyleRole.Text, this.Host.Theme.Resolve( WidgetStyleRole.Error ) )
			.With( WidgetStyleRole.DialogTitle, this.Host.Theme.Resolve( WidgetStyleRole.Error ) )
			.With( WidgetStyleRole.DialogBorder, this.Host.Theme.Resolve( WidgetStyleRole.Error ) )
			.With( WidgetStyleRole.DialogAction, this.Host.Theme.Resolve( WidgetStyleRole.Error ) )
			.With( WidgetStyleRole.DialogFocusedAction, this.Host.Theme.Resolve( WidgetStyleRole.Focused ) ) };
		protected override void OnAction( WidgetAction action ) {
			if ( action is WidgetWizardNavigationRequestedAction request && this.wizard is not null ) { if ( !this.rejected ) { this.rejected = true; this.wizard.RejectNavigation( "Application rejected Next. Press Enter again to continue." ); } else this.wizard.ApplyNavigation( request.ToPage ); return; }
			if ( action is WidgetDialogCompletedAction completed ) this.lastDialogOutcome = completed.Result.Outcome;
			if ( action is not WidgetInvokedAction invoked ) { base.OnAction( action ); return; }
			string id = "dialog." + ++this.dialogId;
			switch ( invoked.CommandId ) {
				case "menu.about":
					this.Show( WidgetMessageDialog.Ok( "menu-about." + ++this.dialogId, "About this sample", "Reusable menus and user-defined themes.\nClick OK, press Enter, or Escape to return." ) ); this.ArrangeDialog(); break;
				case "dialog.ok": this.Show( WidgetMessageDialog.Ok( id, "Notice", "Click OK or press Enter to acknowledge; Escape reports Cancel." ) ); break;
				case "dialog.yesno": this.Show( this.CreateDecisionDialog( id ) ); break;
				case "dialog.okcancel": this.Show( WidgetMessageDialog.OkCancel( id, "Apply?", "Cancel is the safe default. Left selects OK; Enter completes." ) ); break;
				case "dialog.wizard": this.OpenWizard(); break;
				case "dialog.text": this.Show( new WidgetTextPrompt( id, "Enter text / Unicode / paste" ) ); break;
				case "dialog.number": this.Show( new WidgetNumberPrompt( id, "Enter a number (0–100)", CultureInfo.CurrentCulture, 0, 100 ) ); break;
				case "picker.folder": this.Show( new WidgetFolderPicker( id, new LocalWidgetFileSystemProvider(), Environment.CurrentDirectory ) ); break;
				case "picker.file": this.OpenFiles( false ); break;
				case "picker.files": this.OpenFiles( true ); break;
				case "game.help": this.Show( new WidgetHelpOverlay( id, "Effective application bindings", [ new( "pages", "Ctrl+N/P", "Change gallery page" ), new( "quit", "Ctrl+Q", "Exit" ) ] ) ); break;
				default: base.OnAction( action ); break;
			}
		}
		internal bool SmokeMenus() {
			this.SelectPage( 2 );
			long originalLength = this.menuView.Document.Length;
			this.Input( WidgetInput.TextInput( new( 'x' ) ) ); this.Draw();
			WidgetCaretIntent? caret = this.Host.GetCaretIntent();
			if ( caret is null || this.Host.Screen.StandardWindow.CursorRow != caret.Row || this.Host.Screen.StandardWindow.CursorColumn != caret.Column ) return false;
			this.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 'z' ) ) );
			if ( !this.Running || this.menuView.Document.Length != originalLength ) return false;
			this.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 'y' ) ) );
			if ( this.menuView.Document.Length != originalLength + 1 ) return false;
			CursesRectangle bounds = this.MenuEditor.Bounds;
			this.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new( 'f' ) ) ); this.Draw();
			if ( !this.Menu.IsOpen || this.Menu.CurrentItem?.Id != "run" || this.Menu.RenderBounds.Row != this.Menu.Bounds.Row + 1 || this.MenuEditor.Bounds != bounds ) return false;
			this.Input( WidgetInput.Key( WidgetKey.Right ) ); if ( this.Menu.CurrentItem?.Id != "undo" ) return false;
			this.Input( WidgetInput.Key( WidgetKey.Left ) ); this.Input( WidgetInput.Key( WidgetKey.Escape ) );
			if ( !this.Running || this.Menu.IsOpen || !ReferenceEquals( this.Host.FocusedWidget, this.MenuEditor ) ) return false;
			this.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new( 'v' ) ) ); this.Draw();
			this.Input( WidgetInput.FunctionKey( 8 ) );
			this.Input( WidgetInput.PointerInput( new( CursesMouseAction.Move, CursesMouseButton.None, this.Menu.RenderBounds.Column + 2, this.Menu.RenderBounds.Row + 3 ) ) ); this.Draw();
			if ( !this.Host.Theme.HighlightMenuHover || this.Menu.CurrentItem?.Id != "check" ) return false;
			this.Input( WidgetInput.Key( WidgetKey.Enter ) ); if ( this.Menu.IsOpen ) return false;
			this.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new( 'h' ) ) ); this.Input( WidgetInput.Key( WidgetKey.Enter ) ); this.Draw();
			if ( this.Menu.IsOpen || this.Host.ModalDepth != 1 || this.Dialog is not { IsOpen: true } about || about.Bounds.Rows != 7 ) return false;
			this.Input( WidgetInput.Key( WidgetKey.Escape ) ); this.Draw();
			if ( this.Host.ModalDepth != 0 || !ReferenceEquals( this.Host.FocusedWidget, this.MenuEditor ) ) return false;
			this.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new( 'h' ) ) ); this.Input( WidgetInput.Key( WidgetKey.Enter ) ); this.Draw();
			if ( this.Dialog is not { IsOpen: true } clickableAbout ) return false;
			int okColumn = clickableAbout.Bounds.Column + 2, okRow = clickableAbout.Bounds.BottomExclusive - 2;
			this.Input( WidgetInput.PointerInput( new( CursesMouseAction.Press, CursesMouseButton.Primary, okColumn, okRow ) ) ); this.Draw();
			if ( !clickableAbout.IsOpen ) return false;
			this.Input( WidgetInput.PointerInput( new( CursesMouseAction.Release, CursesMouseButton.Primary, okColumn, okRow ) ) ); this.Draw();
			if ( this.Host.ModalDepth != 0 || !ReferenceEquals( this.Host.FocusedWidget, this.MenuEditor ) ) return false;
			this.Input( WidgetInput.FunctionKey( 8 ) ); return true;
		}
		internal bool SmokeDialogs() {
			this.SelectPage( 3 ); this.lastDialogOutcome = null;
			this.Show( this.CreateDecisionDialog( "smoke-mnemonic" ) ); this.Input( WidgetInput.TextInput( new( 'n' ) ) );
			if ( this.lastDialogOutcome != WidgetDialogOutcome.No || this.Host.ModalDepth != 0 ) return false;
			this.Show( this.CreateDecisionDialog( "smoke-default" ) ); this.Input( WidgetInput.Key( WidgetKey.Enter ) );
			if ( this.lastDialogOutcome != WidgetDialogOutcome.Cancel || this.Host.ModalDepth != 0 ) return false;
			this.Show( this.CreateDecisionDialog( "smoke-pointer" ) ); this.ArrangeDialog(); this.Draw();
			if ( this.Dialog is not { IsOpen: true } pointer || !pointer.Styles.TryGetValue( WidgetStyleRole.DialogSurface, out _ ) ) return false;
			int row = pointer.Bounds.BottomExclusive - 2, column = pointer.Bounds.Column + 2;
			this.Input( WidgetInput.PointerInput( new( CursesMouseAction.Press, CursesMouseButton.Primary, column, row ) ) );
			this.Input( WidgetInput.PointerInput( new( CursesMouseAction.Release, CursesMouseButton.Primary, column, row ) ) );
			return this.lastDialogOutcome == WidgetDialogOutcome.Yes && this.Host.ModalDepth == 0;
		}
		public override void Dispose() { base.Dispose(); this.workspaceView.Dispose(); this.menuView.Dispose(); }
	}
}
