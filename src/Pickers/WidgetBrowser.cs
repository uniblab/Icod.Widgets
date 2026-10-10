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

	public bool ApplyLoadResult( WidgetBrowserLoadResult result ) {
		ArgumentNullException.ThrowIfNull( result );
		if ( this.disposed || result.Request.BrowserId != this.id || result.Request.Generation != this.generation ) return false;
		this.PendingLocation = null; this.Error = result.Error;
		if ( result.Error is null && result.Snapshot is not null ) this.Snapshot = result.Snapshot;
		return true;
	}

	public void Dispose() { this.disposed = true; this.PendingLocation = null; }
}
