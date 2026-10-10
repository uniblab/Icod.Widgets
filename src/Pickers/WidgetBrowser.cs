/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
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
