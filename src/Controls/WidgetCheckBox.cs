/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using Icod.DCurses;

public sealed class WidgetCheckBox( string id, string text, bool isChecked = false ) : Widget( id ) {
	public override bool IsFocusable => true;
	public string Text { get; set; } = text ?? throw new ArgumentNullException( nameof( text ) );
	public bool IsChecked { get; private set; } = isChecked;
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key != input.Kind || input.KeyCode is not ( WidgetKey.Space or WidgetKey.Enter ) ) return false;
		host.EnsureActionCapacity();
		this.IsChecked = !this.IsChecked;
		host.Enqueue( new WidgetValueChangedAction<bool>( this.Id, this.IsChecked ) );
		return true;
	}
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		string value = CursesText.TruncateToColumns( $"[{( this.IsChecked ? 'x' : ' ' )}] {this.Text}", area.Columns, screen.TextWidthProvider );
		screen.StandardWindow.Move( area.Row, area.Column );
		screen.StandardWindow.Write( value, !this.IsEffectivelyEnabled ? theme.Disabled : focused ? theme.Focused : theme.Normal );
	}
}

