/*
	Icod.Widgets.Editor.Sample
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Editor.Sample;

using System.Diagnostics;
using Icod.DCurses;
using Icod.Widgets;

internal static class Program {
	private static async Task<int> Main( string[] args ) => args.Contains( "--smoke", StringComparer.Ordinal ) ? await RunSmokeAsync() : await RunInteractiveAsync();

	private static async Task<int> RunSmokeAsync() {
		string directory = Path.Combine( Path.GetTempPath(), $"icod-widgets-{Guid.NewGuid():N}" );
		Directory.CreateDirectory( directory ); string path = Path.Combine( directory, "sample.txt" ); await File.WriteAllTextAsync( path, "sample" );
		try {
			LocalWidgetFileSystemProvider provider = new(); WidgetFilePicker picker = new( "file", provider, directory, false, [ WidgetFileFilter.AllFiles ] );
			using WidgetHost host = new( new CursesScreen( 80, 20 ), WidgetTheme.Default ); _ = host.ShowModal( picker );
			WidgetBrowserRequest request = picker.RequestDirectory( directory ); picker.ApplyLoadResult( await WidgetBrowserLoader.LoadAsync( provider, request, CancellationToken.None ) ); picker.ToggleFile( path );
			WidgetPickerValidationRequest validation = picker.RequestAccept(); picker.ApplyValidationResult( await WidgetPickerValidationLoader.ValidateAsync( provider, validation, CancellationToken.None ) );
			WidgetDialogCompletedAction completed = ( WidgetDialogCompletedAction )host.DrainActions().Single(); string selected = ( ( IReadOnlyList<string> )completed.Result.Value! ).Single();
			if ( "sample" != await File.ReadAllTextAsync( selected ) ) return 1; Console.WriteLine( "PASS picker returned a path; the application opened it" ); return 0;
		} finally { Directory.Delete( directory, true ); }
	}

	private static async Task<int> RunInteractiveAsync() {
		LocalWidgetFileSystemProvider provider = new(); WidgetFilePicker picker = new( "file", provider, Environment.CurrentDirectory, false, [ WidgetFileFilter.AllFiles ] );
		string? selectedPath = null; string? failure = null;
		await using ( CursesSession session = await CursesSession.OpenAsync() ) {
			using WidgetHost host = new( session, WidgetTheme.Default ); Stopwatch clock = Stopwatch.StartNew(); _ = host.ShowModal( picker ); host.Arrange( session.Screen.Bounds );
			WidgetBrowserRequest initial = picker.RequestDirectory( picker.StartLocation ); picker.ApplyLoadResult( await WidgetBrowserLoader.LoadAsync( provider, initial, CancellationToken.None ) );
			bool running = true;
			while ( running ) {
				session.StandardScreen.Clear(); host.Render(); await session.RefreshAsync();
				CursesEvent terminalEvent = await session.ReadEventAsync();
				if ( terminalEvent.RequiresRepaint ) { _ = session.SynchronizeDimensions(); host.Arrange( session.Screen.Bounds ); }
				if ( terminalEvent.Input is not { } input ) continue;
				if ( IsQuit( input ) ) break;
				WidgetDispatchResult result = host.Process( input, clock.Elapsed );
				foreach ( WidgetAction action in result.Actions ) {
					if ( action is WidgetBrowserLoadRequestedAction load ) picker.ApplyLoadResult( await WidgetBrowserLoader.LoadAsync( provider, load.Request, CancellationToken.None ) );
					else if ( action is WidgetPickerValidationRequestedAction validation ) {
						picker.ApplyValidationResult( await WidgetPickerValidationLoader.ValidateAsync( provider, validation.Request, CancellationToken.None ) );
						foreach ( WidgetAction followUp in host.DrainActions() ) running = HandleCompletion( followUp, ref selectedPath );
					} else running = HandleCompletion( action, ref selectedPath );
				}
			}
		}
		if ( selectedPath is not null ) {
			try { string text = await File.ReadAllTextAsync( selectedPath ); Console.WriteLine( $"Selected: {selectedPath}\nCharacters read by the application: {text.Length}" ); }
			catch ( Exception exception ) when ( exception is IOException or UnauthorizedAccessException ) { failure = exception.Message; }
		}
		if ( failure is not null ) { Console.Error.WriteLine( failure ); return 1; }
		return 0;
	}

	private static bool HandleCompletion( WidgetAction action, ref string? selectedPath ) {
		if ( action is not WidgetDialogCompletedAction completed ) return true;
		if ( WidgetDialogOutcome.Accepted == completed.Result.Outcome && completed.Result.Value is IReadOnlyList<string> paths ) selectedPath = paths.Single();
		return false;
	}

	private static bool IsQuit( CursesInputEvent input ) => input.Character is { Value: 81 or 113 } && 0 != ( input.Modifiers & CursesKeyModifiers.Control );
}
