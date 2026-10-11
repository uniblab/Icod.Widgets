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

/// <summary>Clipboard request/completion portion of <see cref="WidgetHost"/>.</summary>
public sealed partial class WidgetHost {
	private sealed record PendingClipboard( Widget Source, IWidgetClipboardTarget Target, WidgetClipboardRequest Request );
	private readonly Dictionary<Guid, PendingClipboard> pendingClipboard = [];
	private readonly Dictionary<Widget, Guid> pendingClipboardByWidget = [];
	private readonly Dictionary<Widget, long> attachmentGenerations = [];
	private long nextAttachmentGeneration;
	private long focusGeneration;

	public bool ApplyClipboardResult( WidgetClipboardResult result ) {
		ArgumentNullException.ThrowIfNull( result ); this.ThrowIfDisposed();
		if ( !Enum.IsDefined( result.Status ) ) throw new ArgumentOutOfRangeException( nameof( result ) );
		if ( !this.pendingClipboard.Remove( result.RequestId, out PendingClipboard? pending ) ) return false;
		this.pendingClipboardByWidget.Remove( pending.Source );
		if ( !ReferenceEquals( pending.Source.Host, this ) || !ReferenceEquals( this.FocusedWidget, pending.Source ) ||
			!this.attachmentGenerations.TryGetValue( pending.Source, out long attachment ) || attachment != pending.Request.AttachmentGeneration ||
			this.focusGeneration != pending.Request.FocusGeneration || !pending.Target.IsClipboardRequestCurrent( pending.Request ) ) return false;
		return pending.Target.ApplyClipboardResult( pending.Request, result, this.CurrentTime );
	}

	internal WidgetClipboardRequest RegisterClipboardRequest( Widget source, WidgetClipboardRequest request ) {
		ArgumentNullException.ThrowIfNull( source ); ArgumentNullException.ThrowIfNull( request ); this.ThrowIfDisposed();
		if ( source is not IWidgetClipboardTarget target || !ReferenceEquals( source.Host, this ) || !ReferenceEquals( this.FocusedWidget, source ) )
			throw new InvalidOperationException( "Clipboard requests require the attached focused edit target." );
		if ( Guid.Empty == request.RequestId || this.pendingClipboard.ContainsKey( request.RequestId ) ) throw new InvalidOperationException( "The clipboard request identity is not unique." );
		WidgetClipboardRequest guarded = request with { AttachmentGeneration = this.attachmentGenerations[ source ], FocusGeneration = this.focusGeneration };
		this.InvalidateClipboard( source ); PendingClipboard pending = new( source, target, guarded );
		this.pendingClipboard.Add( guarded.RequestId, pending ); this.pendingClipboardByWidget.Add( source, guarded.RequestId ); return guarded;
	}

	private void RegisterAttachmentGeneration( Widget widget ) => this.attachmentGenerations.Add( widget, checked( ++this.nextAttachmentGeneration ) );
	private void RemoveAttachmentGeneration( Widget widget ) { this.InvalidateClipboard( widget ); this.attachmentGenerations.Remove( widget ); }
	private void ClipboardFocusChanged( Widget? previous ) {
		this.focusGeneration = checked( this.focusGeneration + 1 ); if ( previous is not null ) this.InvalidateClipboard( previous );
	}
	private void InvalidateClipboard( Widget widget ) {
		if ( this.pendingClipboardByWidget.Remove( widget, out Guid requestId ) ) this.pendingClipboard.Remove( requestId );
	}
	private void ClearClipboardRequests() { this.pendingClipboard.Clear(); this.pendingClipboardByWidget.Clear(); this.attachmentGenerations.Clear(); }
}
