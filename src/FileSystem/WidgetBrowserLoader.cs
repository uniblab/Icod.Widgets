/*
	Icod.Widgets
	Reusable terminal UI controls for .NET.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
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

