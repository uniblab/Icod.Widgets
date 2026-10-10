/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

/// <summary>Performs provider I/O without mutating a widget controller.</summary>
public static class WidgetBrowserLoader {
	public static async ValueTask<WidgetBrowserLoadResult> LoadAsync( IWidgetFileSystemProvider provider, WidgetBrowserRequest request, CancellationToken token ) {
		ArgumentNullException.ThrowIfNull( provider ); ArgumentNullException.ThrowIfNull( request );
		try {
			WidgetDirectorySnapshot snapshot = request.IsRoots
				? new WidgetDirectorySnapshot( "", null, await provider.GetRootsAsync( token ).ConfigureAwait( false ) )
				: await provider.ReadDirectoryAsync( request.Location, token ).ConfigureAwait( false );
			token.ThrowIfCancellationRequested();
			if ( snapshot.Entries.Count > WidgetLimits.MaximumItems ) return WidgetBrowserLoadResult.Failure( request, new WidgetBrowserError( WidgetBrowserErrorKind.TooManyEntries, $"A directory cannot contain more than {WidgetLimits.MaximumItems} entries." ) );
			return WidgetBrowserLoadResult.Success( request, snapshot );
		} catch ( OperationCanceledException ) { throw; }
		catch ( UnauthorizedAccessException exception ) { return Failure( WidgetBrowserErrorKind.AccessDenied, exception ); }
		catch ( DirectoryNotFoundException exception ) { return Failure( WidgetBrowserErrorKind.NotFound, exception ); }
		catch ( FileNotFoundException exception ) { return Failure( WidgetBrowserErrorKind.NotFound, exception ); }
		catch ( PathTooLongException exception ) { return Failure( WidgetBrowserErrorKind.InvalidPath, exception ); }
		catch ( ArgumentException exception ) { return Failure( WidgetBrowserErrorKind.InvalidPath, exception ); }
		catch ( NotSupportedException exception ) { return Failure( WidgetBrowserErrorKind.InvalidPath, exception ); }
		catch ( IOException exception ) { return Failure( WidgetBrowserErrorKind.IoError, exception ); }

		WidgetBrowserLoadResult Failure( WidgetBrowserErrorKind kind, Exception exception ) => WidgetBrowserLoadResult.Failure( request, new WidgetBrowserError( kind, exception.Message ) );
	}
}

