/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;
using Icod.DCurses;

public sealed partial class WidgetHost {
	private readonly Dictionary<WidgetDialog, CursesPanel> dialogPanels = [];
	private CursesPanel? attachmentPanel;
	internal CursesWindow GetPresentationWindow( Widget widget ) => widget is WidgetDialog dialog && this.dialogPanels.TryGetValue( dialog, out CursesPanel? panel ) ? panel.ContentWindow : this.popup?.Owner == widget ? this.popup.Panel.ContentWindow : this.Screen.StandardWindow;
	private void RenderPanel( Widget widget, CursesPanel panel ) {
		CursesRectangle bounds = ( widget is WidgetDropDown drop ? drop.PopupBounds : widget.Bounds ).Intersect( this.Screen.Bounds );
		if ( bounds.IsEmpty || !widget.IsEffectivelyVisible ) { panel.Hide(); return; }
		panel.SetBounds( bounds ); panel.Show();
		// Render through the existing public control seam, then copy only the overlay rectangle.
		CursesScreen staging = new( this.Screen.Columns, this.Screen.Rows, this.Screen.TextWidthProvider );
		staging.StandardWindow.WrapMode = CursesWrapMode.Clip;
		widget.OnRender( staging, this.Theme, ReferenceEquals( widget, this.FocusedWidget ) );
		staging.StandardWindow.CopyRectangleTo( this.GetPresentationWindow( widget ), bounds.Row, bounds.Column, bounds.Rows, bounds.Columns, 0, 0 );
	}
}
