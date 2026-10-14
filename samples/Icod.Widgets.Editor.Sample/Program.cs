/*
	Icod.Widgets.Editor.Sample
	Interactive editor sample for Icod.Widgets.
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

namespace Icod.Widgets.Editor.Sample;
using Icod.DCurses;
using Icod.Widgets.Samples;

internal static class Program {
	private static async Task<int> Main( string[] args ) {
		if ( !args.Contains( "--smoke", StringComparer.Ordinal ) ) return await WidgetSampleApplication.RunAsync( screen => new Editor( screen, Environment.CurrentDirectory ) );
		string directory = Path.Combine( Path.GetTempPath(), $"icod-editor-{Guid.NewGuid():N}" ); Directory.CreateDirectory( directory );
		try {
			await File.WriteAllTextAsync( Path.Combine( directory, "note.txt" ), "hello\r\nwidgets\n" );
			using Editor app = new( new CursesScreen( 60, 12 ), directory ); app.Arrange(); app.Open(); await app.PollAsync( true );
			app.Input( WidgetInput.Key( WidgetKey.Enter ) );
			await app.PollAsync( true ); await app.PollAsync( true ); app.Draw();
			if ( app.Text != "hello\r\nwidgets\n" || app.Host.ModalDepth != 0 || app.IsModified ) return 1;
			app.Input( WidgetInput.TextInput( new System.Text.Rune( '!' ) ) );
			if ( app.Text != "!hello\r\nwidgets\n" || !app.IsModified ) return 2;
			app.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 'z' ) ) );
			if ( app.Text != "hello\r\nwidgets\n" || app.IsModified ) return 3;
			app.Input( WidgetInput.FunctionKey( 7 ) ); app.Input( WidgetInput.FunctionKey( 8 ) );
			if ( app.Status != "Enter CRLF | Indent 2 spaces" || app.Text != "hello\r\nwidgets\n" || app.IsModified ) return 9;
			app.Input( WidgetInput.Key( WidgetKey.Home, WidgetInputModifiers.Control ) ); app.Input( WidgetInput.Key( WidgetKey.Enter ) );
			if ( app.Text != "\r\nhello\r\nwidgets\n" ) return 10;
			app.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 'z' ) ) );
			if ( app.Text != "hello\r\nwidgets\n" || app.IsModified ) return 11;
			app.Input( WidgetInput.Key( WidgetKey.Home, WidgetInputModifiers.Control ) ); app.Input( WidgetInput.Key( WidgetKey.Tab ) );
			if ( app.Text != "  hello\r\nwidgets\n" ) return 12;
			app.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 'z' ) ) );
			if ( app.Text != "hello\r\nwidgets\n" || app.IsModified ) return 13;
			app.Input( WidgetInput.TextInput( new System.Text.Rune( 'q' ) ) );
			if ( app.Text != "qhello\r\nwidgets\n" || !app.Running ) return 4;
			app.Input( WidgetInput.Key( WidgetKey.Escape ) );
			if ( app.Text != "qhello\r\nwidgets\n" || !app.Running ) return 5;
			app.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 'z' ) ) );
			if ( app.Text != "hello\r\nwidgets\n" || app.IsModified ) return 6;
			// Complete the public clipboard requests locally so the smoke stays terminal-free.
			app.Host.DrainActions();
			app.Input( WidgetInput.Key( WidgetKey.Home, WidgetInputModifiers.Control ) );
			if ( app.Host.ExecuteEditCommand( WidgetEditCommand.Copy, TimeSpan.Zero ) != WidgetCommandResult.Executed ) return 14;
			WidgetClipboardRequest copied = app.Host.DrainActions().OfType<WidgetClipboardRequestedAction>().Single().Request;
			if ( !copied.IsLinewise || copied.Text != "hello\r\n" ||
				!app.Host.ApplyClipboardResult( new WidgetClipboardResult( copied.RequestId, WidgetClipboardStatus.Success ) ) ) return 15;
			app.Input( WidgetInput.Key( WidgetKey.Down ) );
			if ( app.Host.ExecuteEditCommand( WidgetEditCommand.Paste, TimeSpan.Zero ) != WidgetCommandResult.Executed ) return 16;
			WidgetClipboardRequest paste = app.Host.DrainActions().OfType<WidgetClipboardRequestedAction>().Single().Request;
			if ( !app.Host.ApplyClipboardResult( new WidgetClipboardResult( paste.RequestId, WidgetClipboardStatus.Success, copied.Text ) { IsLinewise = copied.IsLinewise } ) ||
				app.Text != "hello\r\nhello\r\nwidgets\n" ) return 17;
			app.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 'z' ) ) );
			if ( app.Text != "hello\r\nwidgets\n" || app.IsModified ) return 18;
			app.Open(); await app.PollAsync( true ); app.Input( WidgetInput.Key( WidgetKey.Escape ) );
			if ( app.Text != "hello\r\nwidgets\n" || !app.Running ) return 7;
			app.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 'q' ) ) );
			if ( app.Running ) return 8;
			Console.WriteLine( "PASS exact file load, mutable insertion policies, linewise clipboard, in-memory edit, undo to checkpoint, cancellation preserves document" ); return 0;
		} finally { Directory.Delete( directory, true ); }
	}
	private sealed class Editor : WidgetSampleApplication {
		private readonly string start;
		private readonly WidgetButton open = new( "open", "Open file (Ctrl+O)", "file.open" );
		private WidgetTextDocument document = new( Array.Empty<ReadOnlyMemory<char>>() );
		private WidgetTextViewState view;
		private WidgetTextEditor editor;
		private int nextId;
		private string newLine = "\n";
		private string indentation = "\t";
		internal string Text => this.document.Read( 0, checked( (int)this.document.Length ) );
		internal bool IsModified => this.document.IsModified;
		protected override string Heading => "Icod.Widgets in-memory editor example";
		protected override string Keys => "F7 Enter style | F8 indent | Ctrl+O open | Ctrl+Z/Y undo/redo | F6 focus | Ctrl+Q exit";
		internal Editor( CursesScreen screen, string start ) : base( screen ) {
			this.start = start; this.view = new WidgetTextViewState( this.document ); this.editor = new WidgetTextEditor( "editor", this.view );
			this.Host.Attach( this.open ); this.Host.Attach( this.editor );
		}
		internal override void Arrange() {
			CursesScreen screen = this.Host.Screen;
			this.open.Arrange( new CursesRectangle( Math.Min( 2, screen.Rows ), 0, Math.Min( 1, Math.Max( 0, screen.Rows - 2 ) ), screen.Columns ) );
			this.editor.Arrange( new CursesRectangle( Math.Min( 3, screen.Rows ), 0, Math.Max( 0, screen.Rows - 5 ), screen.Columns ) );
			if ( this.Dialog is { IsOpen: true } dialog ) dialog.Arrange( screen.Bounds );
			if ( this.Host.FocusedWidget is null ) this.Host.Focus( this.open );
		}
		internal void Open() => this.Show( new WidgetFilePicker( "open." + ++this.nextId, new LocalWidgetFileSystemProvider(), this.start, false, [ new WidgetFileFilter( "Text", [ ".txt", ".md", ".cs", ".json" ] ), WidgetFileFilter.AllFiles ] ) );
		protected override bool OnShortcut( WidgetInput input ) {
			if ( input.KeyCode == WidgetKey.Function && input.KeyPhase != CursesKeyEventPhase.Release ) {
				if ( input.FunctionKeyNumber == 7 ) {
					this.newLine = this.newLine switch { "\n" => "\r\n", "\r\n" => "\r", _ => "\n" };
					this.editor.NewLine = this.newLine; this.ReportPolicy(); return true;
				}
				if ( input.FunctionKeyNumber == 8 ) {
					this.indentation = this.indentation switch { "\t" => "  ", "  " => "    ", _ => "\t" };
					this.editor.Indentation = this.indentation; this.ReportPolicy(); return true;
				}
			}
			if ( input.KeyCode == WidgetKey.Function && input.FunctionKeyNumber == 6 ) { this.Host.Focus( ReferenceEquals( this.Host.FocusedWidget, this.editor ) ? this.open : this.editor ); return true; }
			if ( ( input.Modifiers & WidgetInputModifiers.Control ) == 0 ) return false;
			if ( input.Character?.Value is 'o' or 'O' ) { this.Open(); return true; }
			WidgetEditCommand? command = input.Character?.Value switch {
				'z' or 'Z' => WidgetEditCommand.Undo, 'y' or 'Y' => WidgetEditCommand.Redo,
				'x' or 'X' => WidgetEditCommand.Cut, 'c' or 'C' => WidgetEditCommand.Copy,
				'v' or 'V' => WidgetEditCommand.Paste, 'a' or 'A' => WidgetEditCommand.SelectAll, _ => null
			};
			if ( command is null ) return false; WidgetCommandResult result = this.Host.ExecuteEditCommand( command.Value, this.CurrentTime );
			this.Status = result == WidgetCommandResult.Executed ? command.Value.ToString() : $"{command.Value}: {result}"; return true;
		}
		protected override void OnAction( WidgetAction action ) {
			if ( action is WidgetInvokedAction { CommandId: "file.open" } ) this.Open();
			else if ( action is WidgetTextEditedAction ) this.Status = this.document.IsModified ? "Modified (memory only)" : "Unmodified";
			else if ( action is WidgetDialogCompletedAction { Result.Outcome: WidgetDialogOutcome.Accepted } completed && completed.Result.Value is IReadOnlyList<string> { Count: 1 } paths ) {
				string path = paths[ 0 ]; this.Status = "Reading " + path;
				this.QueueWork( async token => {
					// Size bound keeps this small demonstration responsive and memory bounded.
					if ( new FileInfo( path ).Length > 1_048_576 ) throw new IOException( "Sample supports files up to 1 MiB." );
					string text = await File.ReadAllTextAsync( path, token );
					return () => this.ReplaceDocument( text, path );
				} );
			} else base.OnAction( action );
		}
		private void ReportPolicy() => this.Status = $"Enter {( this.newLine == "\n" ? "LF" : this.newLine == "\r\n" ? "CRLF" : "CR" )} | Indent {( this.indentation == "\t" ? "Tab" : $"{this.indentation.Length} spaces" )}";
		private void ReplaceDocument( string text, string path ) {
			this.Host.Detach( this.editor ); this.view.Dispose();
			this.document = new WidgetTextDocument( [ text.AsMemory() ] ); this.view = new WidgetTextViewState( this.document ); this.editor = new WidgetTextEditor( "editor", this.view );
			this.editor.NewLine = this.newLine; this.editor.Indentation = this.indentation;
			this.Host.Attach( this.editor ); this.Arrange(); this.Host.Focus( this.editor ); this.Status = path + " — unmodified, memory only";
		}
		public override void Dispose() { this.view.Dispose(); base.Dispose(); }
	}
}
