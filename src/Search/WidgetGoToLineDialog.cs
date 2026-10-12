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

using System.Globalization;
using Icod.DCurses;

public sealed record WidgetGoToLineRequest( Guid RequestId, Guid DocumentId, Guid ViewId, long Revision, long LineNumber );

/// <summary>Collects a positive one-based line number without moving an editor selection.</summary>
public sealed class WidgetGoToLineDialog : WidgetDialog {
	private Guid documentId;
	private Guid viewId;
	private long revision;
	public WidgetGoToLineDialog( string id, string title ) : base( id, title ) { this.LineInput = new WidgetTextInput( id + ".line" ); this.Add( this.LineInput ); }
	public WidgetTextInput LineInput { get; }
	public string? Error { get; private set; }
	internal override string? InitialFocusId => this.LineInput.Id;
	public void SetContext( Guid documentId, Guid viewId, long revision ) {
		if ( documentId == Guid.Empty || viewId == Guid.Empty || revision < 0 ) throw new ArgumentException( "Go To Line needs valid document/view identities and revision." );
		this.documentId = documentId; this.viewId = viewId; this.revision = revision; this.LineInput.ResetForNewContext(); this.Error = null;
	}
	protected override void OnArrange( CursesRectangle bounds ) {
		CursesRectangle area = this.Host is null ? bounds : this.GetContentBounds( this.Host.Screen );
		this.LineInput.Arrange( new CursesRectangle( area.Row, area.Column, Math.Min( 1, area.Rows ), area.Columns ) );
	}
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key != input.Kind ) return false;
		if ( WidgetKey.Escape == input.KeyCode ) { this.Finish( host, WidgetDialogOutcome.Cancel ); return true; }
		if ( WidgetKey.Enter != input.KeyCode ) return false;
		if ( this.documentId == Guid.Empty || !long.TryParse( this.LineInput.Value, NumberStyles.None, CultureInfo.InvariantCulture, out long line ) || line <= 0 ) {
			this.Error = "Enter a positive one-based line number."; return true;
		}
		this.Error = null; this.Finish( host, new WidgetDialogResult( WidgetDialogOutcome.Accepted,
			new WidgetGoToLineRequest( Guid.NewGuid(), this.documentId, this.viewId, this.revision, line ) ) ); return true;
	}
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.RenderFrame( screen, theme ); if ( area.IsEmpty ) return;
		WidgetBrowserView.WriteLine( screen, area, area.Rows - 1, this.Error ?? "Enter: Go | Esc: Cancel", this.Error is null ? theme.Normal : theme.Error );
	}
}
