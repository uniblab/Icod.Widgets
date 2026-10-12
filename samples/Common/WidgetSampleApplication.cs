/*
	Icod.Widgets.Gallery.Sample / Icod.Widgets.Editor.Sample / Icod.Widgets.Roguelike.Sample
	Shared application loop for the Icod.Widgets samples.
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

namespace Icod.Widgets.Samples;
using System.Diagnostics;
using Icod.DCurses;

// Application-owned event loop, I/O, cancellation, and session lifetime.
internal abstract class WidgetSampleApplication : IDisposable {
	private readonly CancellationTokenSource stop = new();
	private readonly List<Task<Action>> jobs = [];
	private readonly Stopwatch clock = Stopwatch.StartNew();
	private WidgetSampleClipboardAdapter? clipboard;
	protected WidgetSampleApplication( CursesScreen screen ) => this.Host = new( screen, WidgetTheme.Default );
	internal WidgetHost Host { get; }
	internal bool Running { get; private set; } = true;
	internal string Status { get; set; } = "Ready";
	protected TimeSpan CurrentTime => this.clock.Elapsed;
	protected abstract string Heading { get; }
	protected abstract string Keys { get; }
	protected WidgetDialog? Dialog { get; private set; }
	internal virtual void Arrange() => this.Host.Arrange( new CursesRectangle( Math.Min( 2, this.Host.Screen.Rows ), 0, Math.Max( 0, this.Host.Screen.Rows - 4 ), this.Host.Screen.Columns ) );
	internal void Draw() {
		this.Host.Screen.StandardWindow.Clear();
		Write( 0, this.Heading ); Write( 1, this.Keys );
		if ( this.Host.ModalDepth == 0 ) { Write( this.Host.Screen.Rows - 2, this.Status ); Write( this.Host.Screen.Rows - 1, "Tab/Shift+Tab focus | Q or Esc exit | Ctrl+Q exit from any control" ); }
		this.Host.Render();
	}
	protected void Write( int row, string text ) {
		CursesScreen screen = this.Host.Screen; if ( row < 0 || row >= screen.Rows || screen.Columns == 0 ) return;
		screen.StandardWindow.Move( row, 0 ); screen.StandardWindow.Write( CursesText.TruncateToColumns( text.Replace( '\n', ' ' ).Replace( '\r', ' ' ), screen.Columns, screen.TextWidthProvider ) );
	}
	internal void Input( WidgetInput input ) {
		bool controlQ = ( input.Modifiers & WidgetInputModifiers.Control ) != 0 && input.Character?.Value is 'q' or 'Q';
		bool quit = this.Host.ModalDepth == 0 && this.Host.FocusedWidget is not IWidgetEditTarget && ( input.KeyCode == WidgetKey.Escape || input.Character?.Value is 'q' or 'Q' );
		if ( input.Kind == WidgetInputKind.EndOfInput || controlQ || quit ) { this.Running = false; return; }
		if ( this.Host.ModalDepth == 0 && this.OnShortcut( input ) ) return;
		this.Actions( this.Host.Process( input, this.clock.Elapsed ).Actions );
	}
	protected virtual bool OnShortcut( WidgetInput input ) => false;
	protected virtual void OnAction( WidgetAction action ) { this.Status = action.ToString() ?? "Action"; }
	private void Actions( IReadOnlyList<WidgetAction> actions ) {
		foreach ( WidgetAction action in actions ) {
			if ( action is WidgetBrowserLoadRequestedAction browse ) this.Load( browse.Request );
			else if ( action is WidgetPickerValidationRequestedAction validation ) this.Validate( validation.Request );
			else if ( action is WidgetClipboardRequestedAction clipboard ) this.Clipboard( clipboard.Request );
			else { this.OnAction( action ); if ( action is WidgetDialogCompletedAction && this.Dialog?.Id == action.WidgetId ) this.Dialog = null; }
		}
	}
	private void Clipboard( WidgetClipboardRequest request ) {
		WidgetSampleClipboardAdapter adapter = this.clipboard ?? throw new InvalidOperationException( "The live clipboard adapter is unavailable." );
		this.QueueWork( async token => {
			WidgetClipboardResult result = await adapter.ExecuteAsync( request, token );
			return () => { bool applied = this.Host.ApplyClipboardResult( result ); this.Status = applied ? $"Clipboard {request.Operation}: {result.Status}" : $"Clipboard {request.Operation}: stale result ignored"; };
		} );
	}
	protected void Show( WidgetDialog dialog ) {
		this.Dialog = dialog; this.Host.ShowModal( dialog );
		if ( dialog is WidgetFilePicker file ) this.Load( file.RequestDirectory( file.StartLocation ) );
		else if ( dialog is WidgetFolderPicker folder ) this.Load( folder.RequestDirectory( folder.StartLocation ) );
	}
	private IWidgetFileSystemProvider Provider => this.Dialog switch { WidgetFilePicker file => file.Provider, WidgetFolderPicker folder => folder.Provider, _ => throw new InvalidOperationException( "No picker." ) };
	protected void QueueWork( Func<CancellationToken, Task<Action>> work ) => this.jobs.Add( Task.Run( () => work( this.stop.Token ), this.stop.Token ) );
	private void Load( WidgetBrowserRequest request ) {
		WidgetDialog? dialog = this.Dialog; IWidgetFileSystemProvider provider = this.Provider;
		this.jobs.Add( Task.Run( async () => {
			WidgetBrowserLoadResult result = await WidgetBrowserLoader.LoadAsync( provider, request, this.stop.Token );
			return (Action)( () => { if ( dialog is WidgetFilePicker file ) file.ApplyLoadResult( result ); else if ( dialog is WidgetFolderPicker folder ) folder.ApplyLoadResult( result ); } );
		}, this.stop.Token ) );
	}
	private void Validate( WidgetPickerValidationRequest request ) {
		WidgetDialog? dialog = this.Dialog; IWidgetFileSystemProvider provider = this.Provider;
		this.jobs.Add( Task.Run( async () => {
			WidgetPickerValidationResult result = await WidgetPickerValidationLoader.ValidateAsync( provider, request, this.stop.Token );
			return (Action)( () => { if ( dialog is WidgetFilePicker file ) file.ApplyValidationResult( result ); else if ( dialog is WidgetFolderPicker folder ) folder.ApplyValidationResult( result ); } );
		}, this.stop.Token ) );
	}
	internal async Task<bool> PollAsync( bool wait = false ) {
		bool changed = false;
		foreach ( Task<Action> job in this.jobs.ToArray() ) {
			if ( !wait && !job.IsCompleted ) continue;
			this.jobs.Remove( job ); changed = true;
			try { ( await job )(); }
			catch ( OperationCanceledException ) when ( this.stop.IsCancellationRequested ) { }
			catch ( Exception error ) when ( error is IOException or UnauthorizedAccessException or ArgumentException ) { this.Status = error.Message; }
		}
		IReadOnlyList<WidgetAction> actions = this.Host.DrainActions(); this.Actions( actions ); return changed || actions.Count > 0;
	}
	internal static async Task<int> RunAsync( Func<CursesScreen, WidgetSampleApplication> create ) {
		await using CursesSession session = await CursesSession.OpenAsync( new CursesSessionOptions { InputMode = CursesInputMode.Raw } );
		session.StandardScreen.WrapMode = CursesWrapMode.Clip;
		using WidgetSampleApplication app = create( session.Screen );
		await using WidgetSampleCaretCoordinator caret = new( session );
		app.clipboard = new WidgetSampleClipboardAdapter( session );
		List<CursesInputProtocolLease> leases = [];
		try {
			foreach ( CursesInputProtocolOptions options in new[] { new CursesInputProtocolOptions { MouseTrackingMode = CursesMouseTrackingMode.ButtonEvents }, new CursesInputProtocolOptions { BracketedPaste = true }, new CursesInputProtocolOptions { KeyboardReportingMode = CursesKeyboardReportingMode.Disambiguated } } ) {
				var result = await session.AcquireInputProtocolsAsync( options ); if ( result.IsAvailable ) leases.Add( result.GetRequiredValue() );
			}
			app.Arrange();
			bool redraw = true;
			while ( app.Running ) {
				redraw |= await app.PollAsync(); if ( redraw ) { app.Draw(); await session.RefreshAsync(); await caret.ApplyAsync( app.Host.GetCaretIntent(), app.stop.Token ); redraw = false; }
				CursesEvent current = await session.ReadEventAsync( TimeSpan.FromMilliseconds( 50 ) );
				if ( current.Kind == CursesEventKind.Input && current.Input is { } input ) { app.Input( WidgetInput.From( input ) ); redraw = true; }
				else if ( current.Kind == CursesEventKind.Lifecycle && current.Lifecycle is { } lifecycle ) {
					if ( lifecycle.Kind is CursesLifecycleEventKind.Interrupt or CursesLifecycleEventKind.Termination ) break;
					if ( lifecycle.Kind is CursesLifecycleEventKind.Resize or CursesLifecycleEventKind.Resumed ) { session.SynchronizeDimensions(); app.Arrange(); session.Invalidate(); redraw = true; }
				}
			}
		} finally { try { session.StandardScreen.Clear(); await session.RefreshAsync(); } finally { for ( int index = leases.Count - 1; index >= 0; index-- ) await leases[ index ].DisposeAsync(); } }
		return 0;
	}
	public virtual void Dispose() { this.stop.Cancel(); this.Host.Dispose(); /* Pending workers observe cancellation; they never mutate the host. */ }
}
