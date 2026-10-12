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
	public WidgetCaretIntent? GetCaretIntent() {
		this.ThrowIfDisposed(); if ( this.popup is not null ) return null; Widget? focused = this.FocusedWidget;
		if ( focused is null || !focused.IsEffectivelyEnabled || !focused.IsEffectivelyVisible ) return null;
		WidgetCaretIntent? intent = focused.GetHostCaretIntent();
		if ( intent is null || !intent.IsVisible || !this.Screen.Bounds.Contains( intent.Row, intent.Column ) ) return null;
		return intent with { FocusGeneration = this.focusGeneration };
	}
	internal CursesWindow GetPresentationWindow( Widget widget ) => widget is WidgetDialog dialog && this.dialogPanels.TryGetValue( dialog, out CursesPanel? panel ) ? panel.ContentWindow : this.popup?.Owner == widget ? this.popup.Panel.ContentWindow : this.Screen.StandardWindow;
	private void RenderPanel( Widget widget, CursesPanel panel ) {
		CursesRectangle bounds = ( widget is WidgetDropDown drop ? drop.PopupBounds : widget.Bounds ).Intersect( this.Screen.Bounds );
		if ( bounds.IsEmpty || !widget.IsEffectivelyVisible ) { panel.Hide(); return; }
		panel.SetBounds( bounds ); panel.Show();
		// Render through the existing public control seam, then copy only the overlay rectangle.
		CursesScreen staging = new( this.Screen.Columns, this.Screen.Rows, this.Screen.TextWidthProvider );
		staging.StandardWindow.WrapMode = CursesWrapMode.Clip;
		widget.OnRender( staging, this.Theme, ReferenceEquals( widget, this.FocusedWidget ) );
		if ( widget is WidgetDialog dialog ) foreach ( Widget child in dialog.DescendantsAndSelf().Skip( 1 ) )
			if ( child.IsEffectivelyVisible ) child.OnRender( staging, this.Theme, ReferenceEquals( child, this.FocusedWidget ) );
		staging.StandardWindow.CopyRectangleTo( this.GetPresentationWindow( widget ), bounds.Row, bounds.Column, bounds.Rows, bounds.Columns, 0, 0 );
	}
}
