/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using Icod.DCurses;

public sealed class WidgetRadioGroup : Widget {
	private readonly string[] keys;
	private int selectedIndex;
	public WidgetRadioGroup( string id, IReadOnlyList<string> keys, string? selectedKey = null ) : base( id ) {
		ArgumentNullException.ThrowIfNull( keys );
		if ( 0 == keys.Count || keys.Count > WidgetLimits.MaximumItems ) throw new ArgumentOutOfRangeException( nameof( keys ) );
		this.keys = keys.ToArray();
		if ( this.keys.Any( string.IsNullOrWhiteSpace ) || this.keys.Distinct( StringComparer.Ordinal ).Count() != this.keys.Length ) throw new ArgumentException( "Radio keys must be nonempty and unique.", nameof( keys ) );
		this.selectedIndex = selectedKey is null ? 0 : Array.IndexOf( this.keys, selectedKey );
		if ( 0 > this.selectedIndex ) throw new ArgumentException( "The selected key is not present.", nameof( selectedKey ) );
	}
	public override bool IsFocusable => true;
	public IReadOnlyList<string> Keys => this.keys;
	public string SelectedKey => this.keys[ this.selectedIndex ];
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key != input.Kind ) return false;
		int next = input.KeyCode switch {
			WidgetKey.Up or WidgetKey.Left => Math.Max( 0, this.selectedIndex - 1 ),
			WidgetKey.Down or WidgetKey.Right => Math.Min( this.keys.Length - 1, this.selectedIndex + 1 ),
			WidgetKey.Home => 0,
			WidgetKey.End => this.keys.Length - 1,
			_ => this.selectedIndex
		};
		if ( next == this.selectedIndex ) return false;
		this.selectedIndex = next;
		host.Enqueue( new WidgetValueChangedAction<string>( this.Id, this.SelectedKey ) );
		return true;
	}
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		string value = CursesText.TruncateToColumns( $"(*) {this.SelectedKey}", area.Columns, screen.TextWidthProvider );
		screen.StandardWindow.Move( area.Row, area.Column ); screen.StandardWindow.Write( value, focused ? theme.Focused : theme.Normal );
	}
}

