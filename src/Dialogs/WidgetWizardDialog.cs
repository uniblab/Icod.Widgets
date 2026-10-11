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
using Icod.DCurses;

/// <summary>Requests application-owned Previous/Next validation and page transitions.</summary>
public sealed class WidgetWizardDialog : WidgetDialog {
	private readonly string[] pages;
	public WidgetWizardDialog( string id, string title, IReadOnlyList<string> pages ) : base( id, title ) {
		ArgumentNullException.ThrowIfNull( pages ); if ( 0 == pages.Count ) throw new ArgumentException( "A wizard requires a page.", nameof( pages ) ); this.pages = pages.ToArray();
	}
	public int PageIndex { get; private set; }
	public string Page => this.pages[ this.PageIndex ];
	public bool CanPrevious => 0 < this.PageIndex;
	public bool CanNext => this.PageIndex + 1 < this.pages.Length;
	public string? Error { get; private set; }
	public void ApplyNavigation( int pageIndex ) { if ( pageIndex < 0 || pageIndex >= this.pages.Length ) throw new ArgumentOutOfRangeException( nameof( pageIndex ) ); this.PageIndex = pageIndex; this.Error = null; }
	public void RejectNavigation( string error ) => this.Error = string.IsNullOrWhiteSpace( error ) ? throw new ArgumentException( "An error is required.", nameof( error ) ) : error;
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key != input.Kind ) return false;
		if ( WidgetKey.Escape == input.KeyCode ) { this.Finish( host, WidgetDialogOutcome.Cancel ); return true; }
		if ( input.KeyCode is WidgetKey.Left && this.CanPrevious ) { host.Enqueue( new WidgetWizardNavigationRequestedAction( this.Id, this.PageIndex, this.PageIndex - 1 ) ); return true; }
		if ( input.KeyCode is WidgetKey.Enter or WidgetKey.Right && this.CanNext ) { host.Enqueue( new WidgetWizardNavigationRequestedAction( this.Id, this.PageIndex, this.PageIndex + 1 ) ); return true; }
		if ( WidgetKey.Enter == input.KeyCode && !this.CanNext ) { this.Finish( host, new WidgetDialogResult( WidgetDialogOutcome.Accepted, this.PageIndex ) ); return true; }
		return false;
	}
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.RenderFrame( screen, theme, this.Title + $" ({this.PageIndex + 1}/{this.pages.Length})" ); if ( area.IsEmpty ) return;
		WidgetBrowserView.WriteLine( screen, area, 0, this.Page, theme.Normal );
		WidgetBrowserView.WriteLine( screen, area, 1, this.Error ?? "", theme.Error );
		int firstFreeRow = this.Error is null ? 1 : 2;
		if ( firstFreeRow <= area.Rows - 1 ) WidgetBrowserView.WriteLine( screen, area, area.Rows - 1, ( this.CanPrevious ? "Left: Previous | " : "" ) + ( this.CanNext ? "Enter/Right: Next | " : "Enter: Finish | " ) + "Esc: Cancel", theme.Focused );
	}
}

