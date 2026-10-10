/*
	Icod.Widgets.Editor.Sample
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Editor.Sample;
using Icod.DCurses;
using Icod.Widgets;

internal static class Program {
	private static async Task<int> Main( string[] args ) {
		if ( !args.Contains( "--smoke", StringComparer.Ordinal ) ) { Console.WriteLine( "Editor sample: applications own file opening. Use --smoke." ); return 0; }
		string directory = Path.Combine( Path.GetTempPath(), $"icod-editor-{Guid.NewGuid():N}" ); Directory.CreateDirectory( directory ); string path = Path.Combine( directory, "note.txt" ); await File.WriteAllTextAsync( path, "hello widgets" );
		try {
			LocalWidgetFileSystemProvider provider = new(); WidgetFilePicker picker = new( "open", provider, directory, false, [ new WidgetFileFilter( "text", [ ".txt" ] ), WidgetFileFilter.AllFiles ] ); using WidgetHost host = new( new CursesScreen( 60, 12 ), WidgetTheme.Default ); host.ShowModal( picker );
			WidgetBrowserRequest browse = picker.RequestDirectory( directory ); picker.ApplyLoadResult( await WidgetBrowserLoader.LoadAsync( provider, browse, CancellationToken.None ) ); picker.ToggleFile( path ); WidgetPickerValidationRequest validation = picker.RequestAccept(); picker.ApplyValidationResult( await WidgetPickerValidationLoader.ValidateAsync( provider, validation, CancellationToken.None ) );
			WidgetDialogCompletedAction action = host.DrainActions().OfType<WidgetDialogCompletedAction>().Single(); string selected = AssertOne( action.Result.Value ); string text = await File.ReadAllTextAsync( selected );
			if ( "hello widgets" != text ) return 1; Console.WriteLine( "PASS picker returned a path; application opened the file" ); return 0;
		} finally { Directory.Delete( directory, true ); }
	}
	private static string AssertOne( object? value ) => value is IReadOnlyList<string> paths && 1 == paths.Count ? paths[ 0 ] : throw new InvalidOperationException( "Expected one selected path." );
}
