/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using Icod.DCurses;

/// <summary>Provides explicit preview, commit, and cancel selection.</summary>
public sealed class WidgetDropDown : Widget {
	private readonly WidgetListItem[] items;
	private int selectedIndex;
	private int previewIndex;

	public WidgetDropDown( string id, IReadOnlyList<WidgetListItem> items, string selectedKey ) : base( id ) {
		ArgumentNullException.ThrowIfNull( items );
		if ( 0 == items.Count || items.Count > WidgetLimits.MaximumItems ) throw new ArgumentOutOfRangeException( nameof( items ) );
		this.items = items.ToArray();
		if ( this.items.Select( item => item.Key ).Distinct( StringComparer.Ordinal ).Count() != this.items.Length ) throw new ArgumentException( "Drop-down keys must be unique.", nameof( items ) );
		this.selectedIndex = Array.FindIndex( this.items, item => item.IsEnabled && item.Key == selectedKey );
		if ( 0 > this.selectedIndex ) throw new ArgumentException( "The selected key must identify an enabled item.", nameof( selectedKey ) );
		this.previewIndex = this.selectedIndex;
	}

	public override bool IsFocusable => true;
	public IReadOnlyList<WidgetListItem> Items => this.items;
	public string SelectedKey => this.items[ this.selectedIndex ].Key;
	public string PreviewKey => this.items[ this.previewIndex ].Key;
	public bool IsOpen { get; private set; }
	public CursesRectangle PopupBounds { get; private set; }

	public void Open() {
		if ( this.Host is null ) throw new InvalidOperationException( "The drop-down must be attached before opening." );
		this.previewIndex = this.selectedIndex; this.IsOpen = true; this.UpdatePopupBounds();
	}

	public void Dismiss() { this.previewIndex = this.selectedIndex; this.IsOpen = false; this.PopupBounds = default; }

	protected override void OnArrange( CursesRectangle bounds ) { if ( this.IsOpen ) this.UpdatePopupBounds(); }

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key != input.Kind ) return false;
		if ( !this.IsOpen ) {
			if ( input.KeyCode is WidgetKey.Enter or WidgetKey.Space or WidgetKey.Down ) { this.Open(); return true; }
			return false;
		}
		if ( WidgetKey.Escape == input.KeyCode ) { this.Dismiss(); return true; }
		if ( WidgetKey.Enter == input.KeyCode ) {
			bool changed = this.selectedIndex != this.previewIndex; this.selectedIndex = this.previewIndex; this.IsOpen = false; this.PopupBounds = default;
			if ( changed ) host.Enqueue( new WidgetValueChangedAction<string>( this.Id, this.SelectedKey ) );
			return true;
		}
		int direction = input.KeyCode is WidgetKey.Down or WidgetKey.Right ? 1 : input.KeyCode is WidgetKey.Up or WidgetKey.Left ? -1 : 0;
		if ( 0 == direction ) return false;
		for ( int count = 1; count <= this.items.Length; count++ ) {
			int index = ( this.previewIndex + direction * count + this.items.Length ) % this.items.Length;
			if ( this.items[ index ].IsEnabled ) { this.previewIndex = index; return true; }
		}
		return false;
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( !area.IsEmpty ) {
			string value = CursesText.TruncateToColumns( this.items[ this.selectedIndex ].Text + " ▾", area.Columns, screen.TextWidthProvider );
			screen.StandardWindow.Move( area.Row, area.Column ); screen.StandardWindow.Write( value, focused ? theme.Focused : theme.Normal );
		}
		if ( !this.IsOpen || this.PopupBounds.IsEmpty ) return;
		for ( int row = 0; row < this.PopupBounds.Rows; row++ ) {
			WidgetListItem item = this.items[ row ]; string value = CursesText.TruncateToColumns( item.Text, this.PopupBounds.Columns, screen.TextWidthProvider );
			screen.StandardWindow.Move( this.PopupBounds.Row + row, this.PopupBounds.Column );
			screen.StandardWindow.Write( value, !item.IsEnabled ? theme.Disabled : row == this.previewIndex ? theme.Focused : theme.Normal );
		}
	}

	private void UpdatePopupBounds() {
		if ( this.Host is null ) return;
		int below = Math.Max( 0, this.Host.Screen.Rows - this.Bounds.BottomExclusive ); int above = Math.Min( this.Bounds.Row, this.Host.Screen.Rows );
		bool useBelow = below >= above; int available = useBelow ? below : above; int rows = Math.Min( this.items.Length, available );
		int row = useBelow ? this.Bounds.BottomExclusive : this.Bounds.Row - rows;
		int column = Math.Min( this.Bounds.Column, this.Host.Screen.Columns ); int columns = Math.Min( this.Bounds.Columns, Math.Max( 0, this.Host.Screen.Columns - column ) );
		this.PopupBounds = new CursesRectangle( Math.Clamp( row, 0, this.Host.Screen.Rows ), column, rows, columns );
	}
}
