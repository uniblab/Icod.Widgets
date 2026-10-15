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
	private readonly Dictionary<WidgetDialog, CursesPanel> dialogPanels = [];
	private CursesPanel? attachmentPanel;
	private CursesScreen? overlayStaging;
	private CursesPanel? modalBackdrop;
	private CursesCell[] backdropRow = [];
	private void RenderModalBackdrop() {
		WidgetDialog? active = this.ActiveModal;
		if ( active is null || !active.HasBackdropStyle( this.Theme ) || !this.dialogPanels.TryGetValue( active, out CursesPanel? front ) || !front.IsVisible || front.Bounds == this.Screen.Bounds ) { this.modalBackdrop?.Hide(); return; }
		this.modalBackdrop ??= this.Screen.CreatePanel( 0, 0, this.Screen.Rows, this.Screen.Columns );
		CursesPanel backdrop = this.modalBackdrop; backdrop.SetBounds( this.Screen.Bounds );
		CursesWindow window = backdrop.ContentWindow;
		this.Screen.StandardWindow.CopyRectangleTo( window, 0, 0, this.Screen.Rows, this.Screen.Columns, 0, 0 );
		// Include lower host dialogs so nested modals dim their parent as well.
		foreach ( var entry in this.dialogPanels ) {
			if ( entry.Key == active ) break;
			CursesRectangle bounds = entry.Value.Bounds.Intersect( this.Screen.Bounds );
			if ( entry.Value.IsVisible && !bounds.IsEmpty ) entry.Value.ContentWindow.CopyRectangleTo( window, 0, 0, bounds.Rows, bounds.Columns, bounds.Row, bounds.Column );
		}
		CursesStyle style = active.ResolveStyle( this.Theme, WidgetStyleRole.ModalBackdrop );
		if ( this.backdropRow.Length != this.Screen.Columns ) this.backdropRow = new CursesCell[ this.Screen.Columns ];
		window.WrapMode = CursesWrapMode.Clip;
		for ( int row = 0; row < this.Screen.Rows; row++ ) {
			for ( int column = 0; column < this.Screen.Columns; column++ ) this.backdropRow[ column ] = window.GetCell( row, column );
			window.FillRectangle( row, 0, 1, this.Screen.Columns, CursesCell.Blank( style ) );
			for ( int column = 0; column < this.Screen.Columns; column++ ) {
				CursesCell cell = this.backdropRow[ column ]; if ( cell.IsContinuation || cell.IsBlank ) continue;
				window.Move( row, column );
				if ( cell.LineGlyph is { } glyph ) window.WriteCell( CursesCell.Line( glyph, style ) );
				else window.Write( cell.Content, style );
			}
		}
		backdrop.MoveBelow( front ); backdrop.Show();
	}

	public WidgetCaretIntent? GetCaretIntent() {
		this.ThrowIfDisposed(); if ( this.popup is not null ) return null; Widget? focused = this.FocusedWidget;
		if ( focused is null || !focused.IsEffectivelyEnabled || !focused.IsEffectivelyVisible ) return null;
		WidgetCaretIntent? intent = focused.GetHostCaretIntent();
		if ( intent is null || !intent.IsVisible || !this.Screen.Bounds.Contains( intent.Row, intent.Column ) ) return null;
		return intent with { FocusGeneration = this.focusGeneration };
	}
	internal CursesWindow GetPresentationWindow( Widget widget ) => widget is WidgetDialog dialog && this.dialogPanels.TryGetValue( dialog, out CursesPanel? panel ) ? panel.ContentWindow : this.popup?.Owner == widget ? this.popup.Panel.ContentWindow : this.Screen.StandardWindow;
	private void RenderPanel( Widget widget, CursesPanel panel ) {
		CursesRectangle bounds = ( widget is WidgetDropDown drop ? drop.PopupBounds : widget is WidgetPopupMenu menu ? menu.GetPopupBounds( this.Screen ) : widget.Bounds ).Intersect( this.Screen.Bounds );
		if ( bounds.IsEmpty || !widget.IsEffectivelyVisible ) { panel.Hide(); return; }
		panel.SetBounds( bounds ); panel.Show();
		// Render through the existing public control seam, then copy only the overlay rectangle.
		if ( this.overlayStaging is null || this.overlayStaging.Columns != this.Screen.Columns || this.overlayStaging.Rows != this.Screen.Rows )
			this.overlayStaging = new CursesScreen( this.Screen.Columns, this.Screen.Rows, this.Screen.TextWidthProvider );
		CursesScreen staging = this.overlayStaging;
		staging.StandardWindow.WrapMode = CursesWrapMode.Clip;
		if ( widget is not WidgetDialog )
			staging.StandardWindow.FillRectangle( bounds.Row, bounds.Column, bounds.Rows, bounds.Columns, CursesCell.Blank( widget.ResolveStyle( this.Theme, WidgetStyleRole.PopupSurface ) ) );
		widget.OnRender( staging, this.Theme, ReferenceEquals( widget, this.FocusedWidget ) );
		if ( widget is WidgetDialog dialog ) foreach ( Widget child in dialog.DescendantsAndSelf().Skip( 1 ) )
			if ( child.IsEffectivelyVisible ) child.OnRender( staging, this.Theme, ReferenceEquals( child, this.FocusedWidget ) );
		staging.StandardWindow.CopyRectangleTo( this.GetPresentationWindow( widget ), bounds.Row, bounds.Column, bounds.Rows, bounds.Columns, 0, 0 );
	}
}
