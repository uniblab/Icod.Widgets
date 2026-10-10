/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

/// <summary>Closes its dialog as canceled; closing a parent unwinds descendants.</summary>
public sealed class WidgetModalLease : IDisposable {
	private WidgetHost? host;
	internal WidgetModalLease( WidgetHost host, WidgetDialog dialog ) { this.host = host; this.Dialog = dialog; }
	public WidgetDialog Dialog { get; }
	public bool IsActive => this.host is not null;
	public void Dispose() { Volatile.Read( ref this.host )?.CloseModal( this.Dialog, WidgetDialogOutcome.Cancel ); }
	internal void MarkClosed() => this.host = null;
}
