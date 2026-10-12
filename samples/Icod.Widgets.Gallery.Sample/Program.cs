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
		if ( !args.Contains( "--smoke", StringComparer.Ordinal ) ) return await WidgetSampleApplication.RunAsync( screen => new Gallery( screen ) );
		using Gallery app = new( new CursesScreen( 80, 30 ) );
		app.Arrange(); app.Host.Focus( app.Name ); app.Input( WidgetInput.Paste( "Ada\tLovelace" ) );
		if ( app.Name.Value != "Ada Lovelace" ) return 1;
		app.Name.SetSelection( 0, 3 ); app.Input( WidgetInput.TextInput( new System.Text.Rune( 'X' ) ) );
		if ( app.Name.Value != "X Lovelace" || WidgetCommandResult.Executed != app.Host.ExecuteEditCommand( WidgetEditCommand.Undo, app.Host.CurrentTime + TimeSpan.FromMilliseconds( 1 ) ) || app.Name.Value != "Ada Lovelace" ) return 1;
		app.Host.DrainActions();
		for ( int page = 0; page < 6; page++ ) { app.SelectPage( page ); app.Draw(); }
		app.SelectPage( 4 ); app.OpenFiles( true ); await app.PollAsync( true ); app.Draw(); app.Input( WidgetInput.Key( WidgetKey.Escape ) );
		app.SelectPage( 3 ); app.OpenWizard(); app.Input( WidgetInput.Key( WidgetKey.Enter ) ); app.Draw(); app.Input( WidgetInput.Key( WidgetKey.Enter ) ); app.Input( WidgetInput.Key( WidgetKey.Escape ) );
		if ( app.Host.ModalDepth != 0 ) return 2;
		Console.WriteLine( "PASS six gallery pages, forms, provider-backed picker cancellation, wizard validation, rendering" ); return 0;
	}
	private sealed class Gallery : WidgetSampleApplication {
		private readonly List<Widget[]> pages = [];
		private int page;
		private int dialogId;
		private WidgetWizardDialog? wizard;
		private bool rejected;
		internal WidgetTextInput Name { get; } = new( "name" );
		protected override string Heading => "Icod.Widgets gallery — " + new[] { "Forms", "Lists / drop-down", "Menus", "Dialogs / wizard", "Folders / files", "Game UI" }[ this.page ];
		protected override string Keys => "Ctrl+N next page | Ctrl+P previous page | Tab chooses control | Enter activates";
		internal Gallery( CursesScreen screen ) : base( screen ) {
			WidgetList single = new( "single", WidgetSelectionMode.Single ); single.SetItems( Items() );
			WidgetList multiple = new( "multiple", WidgetSelectionMode.Multiple ); multiple.SetItems( Items() );
			WidgetMenuShortcut runShortcut = new( WidgetKey.Character, WidgetInputModifiers.Control, 'r' );
			WidgetMenuItem[] menuItems = [
				WidgetMenuItem.Submenu( "file", "File", [ WidgetMenuItem.Command( "run", "Run command", "menu.run", 'r', shortcut: runShortcut ), WidgetMenuItem.Command( "disabled", "Disabled", "disabled", 'd', isEnabled: false ) ], 'f' ),
				WidgetMenuItem.Submenu( "view", "View", [ WidgetMenuItem.Checkable( "check", "Checked option", "menu.check", 'c' ), WidgetMenuItem.Separator( "separator" ), WidgetMenuItem.Submenu( "sub", "Submenu", [ WidgetMenuItem.Command( "nested", "Nested action", "menu.nested", 'n' ) ], 's' ) ], 'v' )
			];
			WidgetPopupMenu menu = new( "menu", menuItems );
			WidgetInventoryList inventory = new( "inventory" ); inventory.SetItems( Items() );
			WidgetMessageHistory history = new( "history" ); history.Append( "The library receives items and messages from the application." ); history.Append( "No game rules live in Icod.Widgets." );
			WidgetStatusBar status = new( "status" ); status.SetFields( [ new( "hp", "HP 12/20", 10 ), new( "depth", "Depth 3", 5 ) ] );
			this.pages.Add( [ new WidgetLabel( "form-hint", "Name (Unicode, selection, paste, undo), checkbox, radio, button" ), this.Name, new WidgetCheckBox( "enabled", "Enabled" ), new WidgetRadioGroup( "radio", [ "Red", "Green", "Blue" ] ), new WidgetButton( "accept", "Accept form", "form.accept" ), new WidgetScrollIndicator( "scroll" ) { Total = 100, Offset = 40, Visible = 20 } ] );
			this.pages.Add( [ new WidgetLabel( "list-hint", "Single: Enter commits | Multi: Space toggles, Ctrl+A all, Shift+arrows range" ), single, multiple, new WidgetDropDown( "drop", Items(), "potion" ) ] );
			this.pages.Add( [ new WidgetLabel( "menu-hint", "Alt+F/V opens | letter invokes | Ctrl+R shortcut | arrows navigate | Esc closes" ), new WidgetMenuBar( "bar", menuItems ), menu ] );
			this.pages.Add( [ Button( "ok", "OK", "dialog.ok" ), Button( "yesno", "Yes / No", "dialog.yesno" ), Button( "okcancel", "OK / Cancel", "dialog.okcancel" ), Button( "wizard", "Previous / Next / Cancel", "dialog.wizard" ), Button( "prompt", "Text prompt", "dialog.text" ), Button( "number", "Number prompt (0 to 100)", "dialog.number" ) ] );
			this.pages.Add( [ Button( "folder", "Select folder", "picker.folder" ), Button( "file", "Select one existing file", "picker.file" ), Button( "files", "Select multiple existing files", "picker.files" ) ] );
			this.pages.Add( [ inventory, history, status, Button( "help", "Show help", "game.help" ) ] );
			foreach ( Widget[] widgets in this.pages ) foreach ( Widget widget in widgets ) this.Host.Attach( widget );
			this.SelectPage( 0 );
		}
		private static WidgetButton Button( string id, string text, string command ) => new( id, text, command );
		private static WidgetListItem[] Items() => [ new( "potion", "Healing potion" ), new( "sword", "Sword 界" ), new( "disabled", "Unavailable item", false ), new( "armour", "Armour" ), new( "scroll", "Scroll á" ), new( "key", "Key" ) ];
		internal void SelectPage( int next ) {
			this.page = next;
			for ( int index = 0; index < this.pages.Count; index++ ) foreach ( Widget widget in this.pages[ index ] ) widget.IsVisible = index == next;
			this.Arrange(); Widget? focus = this.pages[ next ].FirstOrDefault( widget => widget.IsFocusable ); if ( focus is not null ) this.Host.Focus( focus );
		}
		internal override void Arrange() {
			CursesScreen screen = this.Host.Screen; int available = Math.Max( 0, screen.Rows - 4 ); int row = Math.Min( 2, screen.Rows );
			Widget[] widgets = this.pages[ this.page ]; int growing = widgets.Count( widget => widget is WidgetList or WidgetPopupMenu or WidgetMessageHistory );
			int fixedCount = widgets.Length - growing; int growSize = growing == 0 ? 1 : Math.Max( 1, ( available - fixedCount ) / growing );
			foreach ( Widget widget in widgets ) { int rows = Math.Min( Math.Max( 0, screen.Rows - 2 - row ), widget is WidgetList or WidgetPopupMenu or WidgetMessageHistory ? growSize : 1 ); widget.Arrange( new CursesRectangle( row, 0, rows, screen.Columns ) ); row += rows; }
			if ( this.Dialog is { IsOpen: true } dialog ) dialog.Arrange( screen.Bounds );
		}
		protected override bool OnShortcut( WidgetInput input ) {
			if ( ( input.Modifiers & WidgetInputModifiers.Control ) == 0 ) return false;
			if ( input.Character?.Value is 'n' or 'N' ) { this.SelectPage( ( this.page + 1 ) % 6 ); return true; }
			if ( input.Character?.Value is 'p' or 'P' ) { this.SelectPage( ( this.page + 5 ) % 6 ); return true; }
			return false;
		}
		internal void OpenFiles( bool multiple ) => this.Show( new WidgetFilePicker( "files." + ++this.dialogId, new LocalWidgetFileSystemProvider(), Environment.CurrentDirectory, multiple, [ new WidgetFileFilter( "Text / C#", [ ".txt", ".md", ".cs" ] ), WidgetFileFilter.AllFiles ] ) );
		internal void OpenWizard() { this.rejected = false; this.wizard = new( "wizard." + ++this.dialogId, "Application-validated wizard", [ "First page: first Next is rejected to demonstrate validation.", "Second page: Left returns; Esc closes." ] ); this.Show( this.wizard ); }
		protected override void OnAction( WidgetAction action ) {
			if ( action is WidgetWizardNavigationRequestedAction request && this.wizard is not null ) { if ( !this.rejected ) { this.rejected = true; this.wizard.RejectNavigation( "Application rejected Next. Press Enter again to continue." ); } else this.wizard.ApplyNavigation( request.ToPage ); return; }
			if ( action is not WidgetInvokedAction invoked ) { base.OnAction( action ); return; }
			string id = "dialog." + ++this.dialogId;
			switch ( invoked.CommandId ) {
				case "dialog.ok": this.Show( WidgetMessageDialog.Ok( id, "Notice", "Enter acknowledges; Escape consistently reports Cancel." ) ); break;
				case "dialog.yesno": this.Show( WidgetMessageDialog.YesNo( id, "Continue?", "This is a wrapped message. No is the safe default; Escape reports Cancel, independently of No." ) ); break;
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
	}
}
