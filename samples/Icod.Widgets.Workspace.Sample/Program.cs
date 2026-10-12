/*
	Icod.Widgets.Workspace.Sample
	In-memory tabbed document workspace sample for Icod.Widgets.
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

namespace Icod.Widgets.Workspace.Sample;

using Icod.DCurses;
using Icod.Widgets.Samples;

internal static class Program {
	private static async Task<int> Main( string[] args ) {
		if ( !args.Contains( "--smoke", StringComparer.Ordinal ) ) return await WidgetSampleApplication.RunAsync( screen => new Workspace( screen ) );
		using Workspace app = new( new CursesScreen( 64, 12 ) ); app.Arrange();
		if ( app.DocumentKeys.Count != 3 || app.Tabs.ActiveKey != "welcome" ) return 1;
		app.Switch( "notes" ); app.Input( WidgetInput.TextInput( new System.Text.Rune( '!' ) ) );
		if ( !app.IsModified( "notes" ) ) return 2;
		app.AskClose( "notes" ); app.Input( WidgetInput.Key( WidgetKey.Escape ) );
		if ( !app.DocumentKeys.Contains( "notes" ) ) return 3;
		app.AskClose( "notes" ); app.Input( WidgetInput.Key( WidgetKey.Left ) ); app.Input( WidgetInput.Key( WidgetKey.Enter ) );
		if ( app.DocumentKeys.Contains( "notes" ) || app.DocumentKeys.Count != 2 ) return 4;
		app.Switch( "welcome" ); app.OpenSearch(); app.Input( WidgetInput.Paste( "Welcome" ) );
		WidgetSearchRequest request = app.Search!.Submit( WidgetSearchIntent.Find ); app.ApplySearch( request );
		if ( app.ActiveView.Caret != 7 ) return 5;
		app.Input( WidgetInput.Key( WidgetKey.Escape ) ); app.OpenSearch(); app.Input( WidgetInput.Paste( "e" ) );
		request = app.Search!.Submit( WidgetSearchIntent.Find ); app.ApplySearch( request ); app.Input( WidgetInput.Key( WidgetKey.Escape ) );
		if ( app.ActiveView.Caret != 2 ) return 9;
		app.Input( WidgetInput.FunctionKey( 3 ) ); if ( app.ActiveView.Caret != 7 ) return 10;
		app.Input( WidgetInput.FunctionKey( 6 ) ); if ( !ReferenceEquals( app.Host.FocusedWidget, app.Tabs ) ) return 11;
		app.Input( WidgetInput.FunctionKey( 6 ) ); if ( app.Host.FocusedWidget?.Id != "workspace.status" ) return 12;
		app.Input( WidgetInput.FunctionKey( 6, WidgetInputModifiers.Shift ) ); if ( !ReferenceEquals( app.Host.FocusedWidget, app.Tabs ) ) return 13;
		app.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new( 'v' ) ) );
		app.Input( WidgetInput.Key( WidgetKey.Character, character: new( 'f' ) ) );
		if ( app.Status != "Go to File requested" ) return 6;
		app.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 'f' ) ) );
		if ( app.Search is not { IsOpen: true } ) return 7;
		app.Input( WidgetInput.Key( WidgetKey.Escape ) ); app.OpenSaveTarget(); await app.PollAsync( true );
		WidgetSaveTargetRequest save = app.SavePicker!.RequestAccept();
		app.SavePicker.ApplyInspectionResult( new( save, new( "/memory/draft.txt", false, true ) ) ); await app.PollAsync();
		if ( app.WriteCount != 0 ) return 8;
		foreach ( string key in app.DocumentKeys.ToArray() ) app.AskClose( key );
		app.Input( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new( 'f' ) ) );
		if ( app.DocumentKeys.Count != 0 || app.Status != "No document is open" ) return 14;
		app.Draw(); Console.WriteLine( "PASS tab switching, cancel/don't-save, F6 focus, Find Next, empty workspace, scoped mnemonic, search, save observation, status; no file writes" ); return 0;
	}

	private sealed class Workspace : WidgetSampleApplication {
		private sealed record Entry( WidgetTextDocument Document, WidgetTextViewState View, WidgetTextEditor Editor );
		private readonly Dictionary<string, Entry> documents = new( StringComparer.Ordinal );
		private readonly List<string> order = [];
		private readonly MemoryProvider provider = new();
		private readonly WidgetMenuBar menu;
		private readonly WidgetContentHost content = new( "workspace.content" );
		private readonly WidgetStatusBar statusBar = new( "workspace.status" );
		private string? closingKey;
		private string? activeKey = "welcome";
		private string? lastQuery;
		private WidgetSearchOptions lastSearchOptions = new();
		private int nextDialogId;
		protected override string Heading => "Icod.Widgets document workspace — memory only";
		protected override string Keys => "Alt+V,F Go to File | Ctrl+F Find | F3 next | Ctrl+Tab switch | F6 focus | Del close";
		internal WidgetTabStrip Tabs { get; } = new( "workspace.tabs" );
		internal IReadOnlyList<string> DocumentKeys => this.order;
		internal WidgetTextViewState ActiveView => this.documents[ this.activeKey! ].View;
		internal WidgetSearchForm? Search { get; private set; }
		internal WidgetSaveTargetPicker? SavePicker { get; private set; }
		internal int WriteCount => this.provider.WriteCount;
		internal bool IsModified( string key ) => this.documents[ key ].Document.IsModified;
		internal Workspace( CursesScreen screen ) : base( screen ) {
			this.menu = new WidgetMenuBar( "workspace.menu", [ WidgetMenuItem.Submenu( "view", "View", [
				WidgetMenuItem.Command( "go.file", "Go to File...", "view.file", 'f' ),
				WidgetMenuItem.Command( "go.line", "Go to Line...", "view.line", 'l' ),
				WidgetMenuItem.Command( "find", "Find...", "edit.find", 'n', shortcut: new WidgetMenuShortcut( WidgetKey.Character, WidgetInputModifiers.Control, 'f' ) ),
				WidgetMenuItem.Command( "find.next", "Find Next", "edit.find.next", shortcut: new WidgetMenuShortcut( WidgetKey.Function ) { FunctionKeyNumber = 3 } ),
				WidgetMenuItem.Command( "save.as", "Save As...", "file.saveas", 's' )
			], 'v' ) ] );
			this.Host.Attach( this.menu ); this.Host.Attach( this.Tabs ); this.Host.Attach( this.content ); this.Host.Attach( this.statusBar );
			this.AddDocument( "welcome", "Welcome to the workspace.\n" ); this.AddDocument( "notes", "Notes\n" ); this.AddDocument( "scratch", "Scratch\n" ); this.Switch( "welcome" );
			this.RefreshCommands();
		}
		private void AddDocument( string key, string text ) {
			WidgetTextDocument document = new( [ text.AsMemory() ] ); WidgetTextViewState view = new( document );
			this.documents.Add( key, new Entry( document, view, new WidgetTextEditor( "editor." + key, view ) ) ); this.order.Add( key ); this.RefreshTabs();
		}
		private void RefreshTabs() => this.Tabs.SetTabs( this.order.Select( key => new WidgetTab( key, key, this.documents[ key ].Document.IsModified ) ).ToArray(), this.activeKey is not null && this.order.Contains( this.activeKey ) ? this.activeKey : null );
		private void RefreshCommands() {
			bool available = this.activeKey is not null && this.documents.ContainsKey( this.activeKey );
			this.menu.SetPresentation( [
				new( "go.line", "Go to Line...", available ), new( "find", "Find...", available ),
				new( "find.next", "Find Next", available && !string.IsNullOrEmpty( this.lastQuery ) ), new( "save.as", "Save As...", available )
			] );
			this.statusBar.SetFields( [
				new WidgetStatusField( "find", "Find", 100 ) { CommandId = "edit.find", IsEnabled = available },
				new WidgetStatusField( "save", "Save As", 50 ) { CommandId = "file.saveas", IsEnabled = available }
			] );
		}
		internal void Switch( string key ) {
			if ( !this.documents.TryGetValue( key, out Entry? entry ) ) return;
			this.activeKey = key; this.content.SetContent( entry.Editor ); this.RefreshTabs(); this.RefreshCommands(); this.Host.Focus( entry.Editor ); this.Status = $"Active: {key}";
		}
		internal void AskClose( string key ) {
			if ( !this.documents.ContainsKey( key ) ) return;
			if ( !this.documents[ key ].Document.IsModified ) { this.RemoveDocument( key ); return; }
			this.closingKey = key; this.Show( WidgetFormDialog.SaveDontSaveCancel( "close." + ++this.nextDialogId, "Close " + key, "Save changes?" ) );
		}
		private void RemoveDocument( string key ) {
			Entry entry = this.documents[ key ]; string? replacement = this.order.FirstOrDefault( candidate => candidate != key );
			if ( this.activeKey == key ) this.content.SetContent( replacement is null ? null : this.documents[ replacement ].Editor );
			this.order.Remove( key ); this.documents.Remove( key ); entry.View.Dispose();
			if ( this.activeKey == key ) this.activeKey = replacement;
			this.RefreshTabs(); this.RefreshCommands(); this.Status = replacement is null ? "No document is open" : $"Closed {key} without writing";
		}
		internal void OpenSearch() {
			if ( this.activeKey is null || !this.documents.TryGetValue( this.activeKey, out Entry? entry ) ) { this.Status = "No document is open"; return; }
			WidgetSearchForm dialog = new( "find." + ++this.nextDialogId, "Find", false );
			dialog.SetContext( entry.Document.Id, entry.View.Id, entry.Document.Revision ); this.Search = dialog; this.Show( dialog );
		}
		internal void ApplySearch( WidgetSearchRequest request ) {
			if ( this.Search is null || this.activeKey is null || !this.documents.TryGetValue( this.activeKey, out Entry? entry ) ||
				request.DocumentId != entry.Document.Id || request.ViewId != entry.View.Id || request.Revision != entry.Document.Revision ) return;
			int index = this.Find( entry, request.Query, request.Options, 0 );
			WidgetSearchResult result = new( request, index >= 0, index >= 0 ? index : null, index >= 0 ? request.Query.Length : 0 );
			if ( this.Search.ApplyResult( result ) && index >= 0 ) {
				entry.View.SetSelection( index, index + request.Query.Length ); this.lastQuery = request.Query; this.lastSearchOptions = request.Options;
				this.RefreshCommands(); this.Status = $"Found at {index}";
			}
		}
		private void FindNext() {
			if ( this.activeKey is null || !this.documents.TryGetValue( this.activeKey, out Entry? entry ) ) { this.Status = "No document is open"; return; }
			if ( string.IsNullOrEmpty( this.lastQuery ) ) { this.Status = "Find a value first"; return; }
			int start = checked( (int)Math.Min( entry.View.Caret, entry.Document.Length ) ); int index = this.Find( entry, this.lastQuery, this.lastSearchOptions, start );
			if ( index < 0 && start > 0 ) index = this.Find( entry, this.lastQuery, this.lastSearchOptions, 0 );
			if ( index < 0 ) { this.Status = "Not found"; return; }
			entry.View.SetSelection( index, index + this.lastQuery.Length ); this.Status = $"Found at {index}";
		}
		private int Find( Entry entry, string query, WidgetSearchOptions options, int start ) {
			string text = entry.Document.Read( 0, checked( (int)entry.Document.Length ) );
			return text.IndexOf( query, start, options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase );
		}
		internal void OpenSaveTarget() {
			if ( this.activeKey is null || !this.documents.ContainsKey( this.activeKey ) ) { this.Status = "No document is open"; return; }
			this.SavePicker = new WidgetSaveTargetPicker( "save." + ++this.nextDialogId, this.provider, this.provider, "/memory", "draft.txt" ); this.Show( this.SavePicker );
		}
		internal override void Arrange() {
			CursesScreen screen = this.Host.Screen; int width = screen.Columns;
			this.menu.Arrange( new CursesRectangle( Math.Min( 2, screen.Rows ), 0, Math.Min( 1, Math.Max( 0, screen.Rows - 2 ) ), width ) );
			this.Tabs.Arrange( new CursesRectangle( Math.Min( 3, screen.Rows ), 0, Math.Min( 1, Math.Max( 0, screen.Rows - 3 ) ), width ) );
			this.content.Arrange( new CursesRectangle( Math.Min( 4, screen.Rows ), 0, Math.Max( 0, screen.Rows - 7 ), width ) );
			this.statusBar.Arrange( new CursesRectangle( Math.Max( 0, screen.Rows - 3 ), 0, Math.Min( 1, screen.Rows ), width ) );
			if ( this.Dialog is { IsOpen: true } dialog ) dialog.Arrange( screen.Bounds );
		}
		protected override bool OnShortcut( WidgetInput input ) {
			if ( input.Kind == WidgetInputKind.Key && input.KeyCode == WidgetKey.Function && input.FunctionKeyNumber == 6 ) { this.MoveWorkspaceFocus( 0 != ( input.Modifiers & WidgetInputModifiers.Shift ) ? -1 : 1 ); return true; }
			if ( input.Kind != WidgetInputKind.Key || input.KeyCode != WidgetKey.Tab || 0 == ( input.Modifiers & WidgetInputModifiers.Control ) ) return false;
			int index = this.activeKey is null ? -1 : this.order.IndexOf( this.activeKey ); if ( index < 0 || this.order.Count == 0 ) return true;
			this.Switch( this.order[ ( index + ( 0 != ( input.Modifiers & WidgetInputModifiers.Shift ) ? this.order.Count - 1 : 1 ) ) % this.order.Count ] ); return true;
		}
		private void MoveWorkspaceFocus( int direction ) {
			List<Widget> targets = [];
			if ( this.activeKey is not null && this.documents.TryGetValue( this.activeKey, out Entry? entry ) ) targets.Add( entry.Editor );
			targets.Add( this.Tabs ); if ( this.statusBar.IsFocusable ) targets.Add( this.statusBar );
			if ( targets.Count == 0 ) return;
			int current = targets.FindIndex( widget => ReferenceEquals( widget, this.Host.FocusedWidget ) );
			int next = current < 0 ? 0 : ( current + direction + targets.Count ) % targets.Count; this.Host.Focus( targets[ next ] );
		}
		protected override void OnAction( WidgetAction action ) {
			switch ( action ) {
				case WidgetTabActivationRequestedAction tab: this.Switch( tab.TabKey ); break;
				case WidgetTabCloseRequestedAction close: this.AskClose( close.TabKey ); break;
				case WidgetTextEditedAction: this.RefreshTabs(); break;
				case WidgetInvokedAction { CommandId: "view.file" }: this.Status = "Go to File requested"; break;
				case WidgetInvokedAction { CommandId: "view.line" } when this.activeKey is not null && this.documents.TryGetValue( this.activeKey, out Entry? current ): {
					WidgetGoToLineDialog go = new( "line." + ++this.nextDialogId, "Go to line" );
					go.SetContext( current.Document.Id, current.View.Id, current.Document.Revision ); this.Show( go ); break;
				}
				case WidgetInvokedAction { CommandId: "edit.find" }: this.OpenSearch(); break;
				case WidgetInvokedAction { CommandId: "edit.find.next" }: this.FindNext(); break;
				case WidgetInvokedAction { CommandId: "file.saveas" }: this.OpenSaveTarget(); break;
				case WidgetSearchRequestedAction requested: this.ApplySearch( requested.Request ); break;
				case WidgetSaveTargetInspectionRequestedAction inspected: {
					WidgetSaveTargetPicker? picker = this.SavePicker; if ( picker?.TargetProvider is null ) break;
					this.QueueWork( async token => { WidgetSaveTargetObservation observation = await picker.TargetProvider.InspectTargetAsync( inspected.Request.Directory, inspected.Request.FileName, token );
						return () => picker.ApplyInspectionResult( new WidgetSaveTargetInspectionResult( inspected.Request, observation ) ); } ); break;
				}
				case WidgetDialogCompletedAction completed when this.closingKey is not null: {
					string key = this.closingKey; this.closingKey = null;
					if ( completed.Result.Outcome == WidgetDialogOutcome.DontSave ) this.RemoveDocument( key );
					else this.Status = completed.Result.Outcome == WidgetDialogOutcome.Save ? "Save requires application-owned I/O; close denied" : "Close canceled";
					break;
				}
				case WidgetDialogCompletedAction { Result.Value: WidgetSaveTargetSelection selection }: this.Status = $"Observed {selection.TargetPath}; no write performed"; break;
				case WidgetDialogCompletedAction { Result.Value: WidgetGoToLineRequest line } when this.activeKey is not null && this.documents.TryGetValue( this.activeKey, out Entry? current ): {
					if ( line.DocumentId == current.Document.Id && line.ViewId == current.View.Id && line.Revision == current.Document.Revision && line.LineNumber <= current.Document.LineCount ) {
						current.View.SetSelection( current.Document.GetLine( line.LineNumber - 1 ).Start, current.Document.GetLine( line.LineNumber - 1 ).Start ); }
					break;
				}
				default: base.OnAction( action ); break;
			}
		}
		public override void Dispose() { foreach ( Entry entry in this.documents.Values ) entry.View.Dispose(); base.Dispose(); }
	}

	private sealed class MemoryProvider : IWidgetFileSystemProvider, IWidgetSaveTargetProvider {
		public int WriteCount => 0;
		public StringComparer PathComparer => StringComparer.Ordinal;
		public ValueTask<IReadOnlyList<WidgetFileSystemEntry>> GetRootsAsync( CancellationToken token ) => ValueTask.FromResult<IReadOnlyList<WidgetFileSystemEntry>>( [] );
		public ValueTask<WidgetDirectorySnapshot> ReadDirectoryAsync( string location, CancellationToken token ) => ValueTask.FromResult( new WidgetDirectorySnapshot( location, null, [] ) );
		public ValueTask<WidgetFileSelectionValidation> ValidateSelectionAsync( IReadOnlyList<string> paths, WidgetFileSelectionKind kind, CancellationToken token ) => ValueTask.FromResult( WidgetFileSelectionValidation.Valid );
		public ValueTask<WidgetSaveTargetObservation> InspectTargetAsync( string directory, string fileName, CancellationToken token ) => ValueTask.FromResult( new WidgetSaveTargetObservation( directory.TrimEnd( '/' ) + "/" + fileName, false, true ) );
	}
}
