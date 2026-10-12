/*
	Icod.Widgets.Gallery.Sample / Icod.Widgets.Editor.Sample / Icod.Widgets.Roguelike.Sample
	Application-owned clipboard adapter shared by the Icod.Widgets samples.
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

using System.Text;
using Icod.DCurses;

/// <summary>Applies the sample's explicit terminal-clipboard and optional local-fallback policy.</summary>
internal sealed class WidgetSampleClipboardAdapter( CursesSession session, bool allowLocalFallback = true ) {
	private static readonly UTF8Encoding StrictUtf8 = new( false, true );
	private readonly CursesSession session = session ?? throw new ArgumentNullException( nameof( session ) );
	private string? localText;

	internal async Task<WidgetClipboardResult> ExecuteAsync( WidgetClipboardRequest request, CancellationToken cancellationToken ) {
		try {
			if ( request.Operation is WidgetClipboardOperation.Copy or WidgetClipboardOperation.Cut ) {
				string text = request.Text ?? throw new FormatException( "A clipboard export did not contain text." );
				await this.session.WriteClipboardAsync( CursesClipboardSelection.Clipboard, text, cancellationToken ); this.localText = text;
				return new WidgetClipboardResult( request.RequestId, WidgetClipboardStatus.Success );
			}
			ReadOnlyMemory<byte> bytes = await this.session.ReadClipboardAsync( CursesClipboardSelection.Clipboard, TimeSpan.FromSeconds( 2 ), cancellationToken );
			string pasted = StrictUtf8.GetString( bytes.ToArray() ); this.localText = pasted;
			return new WidgetClipboardResult( request.RequestId, WidgetClipboardStatus.Success, pasted );
		} catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
			return new WidgetClipboardResult( request.RequestId, WidgetClipboardStatus.Canceled );
		} catch ( TimeoutException ) {
			return this.Fallback( request, WidgetClipboardStatus.TimedOut );
		} catch ( NotSupportedException ) {
			return this.Fallback( request, WidgetClipboardStatus.Unsupported );
		} catch ( UnauthorizedAccessException ) {
			return this.Fallback( request, WidgetClipboardStatus.Denied );
		} catch ( ArgumentOutOfRangeException ) {
			return new WidgetClipboardResult( request.RequestId, WidgetClipboardStatus.TooLarge );
		} catch ( Exception error ) when ( error is EncoderFallbackException or DecoderFallbackException or FormatException ) {
			return new WidgetClipboardResult( request.RequestId, WidgetClipboardStatus.InvalidText );
		} catch ( IOException ) {
			return this.Fallback( request, WidgetClipboardStatus.Failed );
		}
	}

	private WidgetClipboardResult Fallback( WidgetClipboardRequest request, WidgetClipboardStatus failure ) {
		if ( !allowLocalFallback ) return new WidgetClipboardResult( request.RequestId, failure );
		if ( request.Operation is WidgetClipboardOperation.Copy or WidgetClipboardOperation.Cut ) {
			this.localText = request.Text; return new WidgetClipboardResult( request.RequestId, WidgetClipboardStatus.Success );
		}
		return this.localText is null ? new WidgetClipboardResult( request.RequestId, failure ) :
			new WidgetClipboardResult( request.RequestId, WidgetClipboardStatus.Success, this.localText );
	}
}
