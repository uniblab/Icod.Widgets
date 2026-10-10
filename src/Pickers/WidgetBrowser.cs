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

/// <summary>Applies only the latest result created for this live browser.</summary>
public sealed class WidgetBrowser : IDisposable {
	private readonly Guid id = Guid.NewGuid();
	private long generation;
	private bool disposed;
	public WidgetDirectorySnapshot? Snapshot { get; private set; }
	public WidgetBrowserError? Error { get; private set; }
	public string? PendingLocation { get; private set; }

	public WidgetBrowserRequest RequestDirectory( string location ) {
		if ( this.disposed ) throw new ObjectDisposedException( nameof( WidgetBrowser ) );
		if ( string.IsNullOrWhiteSpace( location ) ) throw new ArgumentException( "A location is required.", nameof( location ) );
		this.PendingLocation = location; this.Error = null; return new WidgetBrowserRequest( this.id, checked( ++this.generation ), location );
	}

	/// <summary>Requests provider roots, sharing the directory generation guard.</summary>
	public WidgetBrowserRequest RequestRoots() { if ( this.disposed ) throw new ObjectDisposedException( nameof( WidgetBrowser ) ); this.PendingLocation = "Roots / drives"; this.Error = null; return new WidgetBrowserRequest( this.id, checked( ++this.generation ), "" ) { IsRoots = true }; }

	public bool ApplyLoadResult( WidgetBrowserLoadResult result ) {
		ArgumentNullException.ThrowIfNull( result );
		if ( this.disposed || result.Request.BrowserId != this.id || result.Request.Generation != this.generation ) return false;
		this.PendingLocation = null; this.Error = result.Error;
		if ( !result.Request.IsRoots && result.Error is null && result.Snapshot is not null ) this.Snapshot = result.Snapshot;
		return true;
	}

	internal void InvalidatePending() { if ( this.disposed ) return; this.generation = checked( this.generation + 1 ); this.PendingLocation = null; }

	public void Dispose() { this.disposed = true; this.PendingLocation = null; }
}
