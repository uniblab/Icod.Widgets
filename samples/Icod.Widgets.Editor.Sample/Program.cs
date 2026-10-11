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
			await File.WriteAllTextAsync( Path.Combine( directory, "note.txt" ), "hello widgets" );
			using Editor app = new( new CursesScreen( 60, 12 ), directory ); app.Arrange(); app.Open(); await app.PollAsync( true );
			app.Input( WidgetInput.Key( WidgetKey.Enter ) );
			await app.PollAsync( true ); await app.PollAsync( true ); app.Draw();
			if ( app.Text != "hello widgets" || app.Host.ModalDepth != 0 ) return 1;
			app.Open(); await app.PollAsync( true ); app.Input( WidgetInput.Key( WidgetKey.Escape ) );
			if ( app.Text != "hello widgets" ) return 2;
			Console.WriteLine( "PASS keyboard file selection, application-owned file reading, cancellation preserves document" ); return 0;
		} finally { Directory.Delete( directory, true ); }
	}
	private sealed class Editor : WidgetSampleApplication {
		private readonly string start;
		private readonly WidgetButton open = new( "open", "Open file (Ctrl+O)", "file.open" );
		private readonly WidgetList lines = new( "lines", WidgetSelectionMode.Single );
		private int nextId;
		internal string Text { get; private set; } = "";
		protected override string Heading => "Icod.Widgets editor open-file example";
		protected override string Keys => "Ctrl+O opens a file | Arrows/PageUp/PageDown browse document | This example never writes files";
		internal Editor( CursesScreen screen, string start ) : base( screen ) { this.start = start; this.Host.Attach( this.open ); this.Host.Attach( this.lines ); }
		internal override void Arrange() {
			CursesScreen screen = this.Host.Screen;
			this.open.Arrange( new CursesRectangle( Math.Min( 2, screen.Rows ), 0, Math.Min( 1, Math.Max( 0, screen.Rows - 2 ) ), screen.Columns ) );
			this.lines.Arrange( new CursesRectangle( Math.Min( 3, screen.Rows ), 0, Math.Max( 0, screen.Rows - 5 ), screen.Columns ) );
			if ( this.Dialog is { IsOpen: true } dialog ) dialog.Arrange( screen.Bounds );
			if ( this.Host.FocusedWidget is null ) this.Host.Focus( this.open );
		}
		internal void Open() => this.Show( new WidgetFilePicker( "open." + ++this.nextId, new LocalWidgetFileSystemProvider(), this.start, false, [ new WidgetFileFilter( "Text", [ ".txt", ".md", ".cs", ".json" ] ), WidgetFileFilter.AllFiles ] ) );
		protected override bool OnShortcut( WidgetInput input ) { if ( ( input.Modifiers & WidgetInputModifiers.Control ) != 0 && input.Character?.Value is 'o' or 'O' ) { this.Open(); return true; } return false; }
		protected override void OnAction( WidgetAction action ) {
			if ( action is WidgetInvokedAction { CommandId: "file.open" } ) this.Open();
			else if ( action is WidgetDialogCompletedAction { Result.Outcome: WidgetDialogOutcome.Accepted } completed && completed.Result.Value is IReadOnlyList<string> { Count: 1 } paths ) {
				string path = paths[ 0 ]; this.Status = "Reading " + path;
				this.QueueWork( async token => {
					// Size bound keeps this small demonstration responsive and memory bounded.
					if ( new FileInfo( path ).Length > 1_048_576 ) throw new IOException( "Sample supports files up to 1 MiB." );
					string text = await File.ReadAllTextAsync( path, token );
					return () => { this.Text = text; this.lines.SetItems( text.Replace( "\r", "" ).Split( '\n' ).Take( WidgetLimits.MaximumItems ).Select( ( line, index ) => new WidgetListItem( index.ToString(), line.Replace( '\t', ' ' ) ) ).ToArray() ); this.Status = path; this.Host.Focus( this.lines ); };
				} );
			} else base.OnAction( action );
		}
	}
}
