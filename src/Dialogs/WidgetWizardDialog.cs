/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

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
		return false;
	}
}

