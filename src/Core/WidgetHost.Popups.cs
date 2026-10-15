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

public sealed partial class WidgetHost {
	private sealed record PopupFrame( Widget Owner, Widget? Previous, CursesInteractionScope Scope, CursesInteractionScopeLease Lease, CursesInteractionRegion Region, CursesPanel Panel ) { public List<CursesPanel> SubmenuPanels { get; } = []; }
	private PopupFrame? popup;
	internal void OpenPopup( Widget owner, CursesRectangle bounds ) {
		this.ThrowIfDisposed();
		if ( this.popup is not null ) this.DismissPopup();
		Widget? previous = this.FocusedWidget;
		CursesInteractionScope scope = this.router.RegisterScope( new CursesInteractionScopeOptions { Parent = this.modalFrames.LastOrDefault()?.Scope } );
		CursesInteractionRegion? region = null; CursesRectangle clipped = bounds.Intersect( this.Screen.Bounds );
		CursesPanel panel = this.Screen.CreatePanel( clipped.Row, clipped.Column, Math.Max( 1, clipped.Rows ), Math.Max( 1, clipped.Columns ) ); if ( clipped.IsEmpty ) panel.Hide();
		try {
			region = this.router.RegisterRegion( new CursesInteractionRegionOptions( new CursesRectangle( 0, 0, clipped.Rows, clipped.Columns ) ) { Panel = panel, Scope = scope, IsFocusable = true, IsEnabled = !clipped.IsEmpty } );
			CursesInteractionScopeLease lease = this.router.ActivateScope( scope );
			this.widgetsByRegion.Add( region, owner ); this.popup = new( owner, previous, scope, lease, region, panel ); this.CancelPointerCapture();
			this.router.Focus( region ); this.ClipboardFocusChanged( previous );
		} catch { region?.Dispose(); panel.Dispose(); scope.Dispose(); throw; }
	}
	internal void UpdatePopup( Widget owner, CursesRectangle bounds ) { if ( this.popup?.Owner == owner ) { CursesRectangle clipped = bounds.Intersect( this.Screen.Bounds ); if ( clipped.IsEmpty ) this.popup.Panel.Hide(); else { this.popup.Panel.SetBounds( clipped ); this.popup.Panel.Show(); } this.popup.Region.SetBounds( new CursesRectangle( 0, 0, clipped.Rows, clipped.Columns ) ); this.popup.Region.IsEnabled = !clipped.IsEmpty; } }
	internal void ClosePopup( Widget owner ) {
		if ( this.popup is not { } frame || frame.Owner != owner ) return;
		this.popup = null; this.CancelPointerCapture(); frame.Lease.Dispose(); this.widgetsByRegion.Remove( frame.Region ); frame.Region.Dispose(); foreach ( CursesPanel panel in frame.SubmenuPanels ) panel.Dispose(); frame.Panel.Dispose(); frame.Scope.Dispose();
		if ( frame.Previous is { } previous && ReferenceEquals( previous.Host, this ) ) this.Focus( previous );
	}
	private void DismissPopup() { if ( this.popup?.Owner is WidgetDropDown drop ) drop.Dismiss(); else if ( this.popup?.Owner is WidgetPopupMenu menu ) menu.Close(); }
}
