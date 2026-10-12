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

/// <summary>Displays an ordered tab snapshot and emits caller-mediated activation and close requests.</summary>
public sealed class WidgetTabStrip( string id ) : Widget( id ) {
	private sealed record Segment( int Index, int Column, int Columns, int CloseColumn );
	private WidgetTab[] tabs = [];
	private Segment[] segments = [];
	private string[] visibleKeys = [];
	private int overflowColumn = -1;
	public override bool IsFocusable => true;
	public override bool AcceptsTab => true;
	public IReadOnlyList<WidgetTab> Tabs => Array.AsReadOnly( this.tabs );
	public string? ActiveKey { get; private set; }
	public IReadOnlyList<string> VisibleKeys => Array.AsReadOnly( this.visibleKeys );

	/// <summary>Replaces the snapshot atomically. Null active key preserves a surviving active key.</summary>
	public void SetTabs( IReadOnlyList<WidgetTab> tabs, string? activeKey = null ) {
		ArgumentNullException.ThrowIfNull( tabs );
		if ( tabs.Count > WidgetLimits.MaximumTabs ) throw new ArgumentOutOfRangeException( nameof( tabs ) );
		WidgetTab[] snapshot = new WidgetTab[ tabs.Count ]; HashSet<string> keys = new( StringComparer.Ordinal );
		for ( int index = 0; index < tabs.Count; index++ ) {
			WidgetTab tab = tabs[ index ] ?? throw new ArgumentException( "A tab cannot be null.", nameof( tabs ) );
			if ( string.IsNullOrWhiteSpace( tab.Key ) || tab.Caption is null || !keys.Add( tab.Key ) )
				throw new ArgumentException( "Tabs require distinct nonempty keys and nonnull captions.", nameof( tabs ) );
			snapshot[ index ] = tab;
		}
		if ( activeKey is not null && !snapshot.Any( tab => tab.IsEnabled && StringComparer.Ordinal.Equals( tab.Key, activeKey ) ) )
			throw new ArgumentException( "The active key must identify an enabled tab.", nameof( activeKey ) );
		int oldIndex = Array.FindIndex( this.tabs, tab => StringComparer.Ordinal.Equals( tab.Key, this.ActiveKey ) );
		string? selected = activeKey;
		if ( selected is null && this.ActiveKey is not null ) selected = snapshot.FirstOrDefault( tab => tab.IsEnabled && StringComparer.Ordinal.Equals( tab.Key, this.ActiveKey ) )?.Key;
		if ( selected is null && snapshot.Length > 0 ) {
			int start = Math.Min( Math.Max( 0, oldIndex ), snapshot.Length - 1 );
			selected = snapshot.Skip( start ).FirstOrDefault( tab => tab.IsEnabled )?.Key
				?? snapshot.Take( start ).Reverse().FirstOrDefault( tab => tab.IsEnabled )?.Key;
		}
		this.tabs = snapshot; this.ActiveKey = selected; this.Layout();
	}

	protected override void OnArrange( CursesRectangle bounds ) => this.Layout();

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is { Button: CursesMouseButton.Primary, Action: CursesMouseAction.Press } pointer ) {
			if ( pointer.Row != this.Bounds.Row ) return false;
			int column = pointer.Column - this.Bounds.Column;
			if ( column == this.overflowColumn ) return this.RequestNeighbor( 1 );
			foreach ( Segment segment in this.segments ) {
				if ( column < segment.Column || column >= segment.Column + segment.Columns ) continue;
				WidgetTab tab = this.tabs[ segment.Index ]; if ( !tab.IsEnabled ) return true;
				if ( column == segment.CloseColumn && tab.IsClosable ) { this.RequestClose( tab ); return true; }
				this.RequestActivation( tab ); return true;
			}
			return false;
		}
		if ( WidgetInputKind.Key != input.Kind ) return false;
		if ( WidgetKey.Tab == input.KeyCode && 0 == ( input.Modifiers & WidgetInputModifiers.Control ) )
			return host.MoveFocus( 0 == ( input.Modifiers & WidgetInputModifiers.Shift ) ? CursesFocusDirection.Forward : CursesFocusDirection.Backward );
		if ( WidgetKey.Tab == input.KeyCode && 0 != ( input.Modifiers & WidgetInputModifiers.Control ) )
			return this.RequestNeighbor( 0 == ( input.Modifiers & WidgetInputModifiers.Shift ) ? 1 : -1 );
		if ( input.KeyCode is WidgetKey.Left or WidgetKey.Right ) return this.RequestNeighbor( WidgetKey.Right == input.KeyCode ? 1 : -1 );
		if ( input.KeyCode is WidgetKey.Home or WidgetKey.End ) {
			WidgetTab? tab = WidgetKey.Home == input.KeyCode ? this.tabs.FirstOrDefault( tab => tab.IsEnabled ) : this.tabs.LastOrDefault( tab => tab.IsEnabled );
			if ( tab is not null ) this.RequestActivation( tab ); return tab is not null;
		}
		WidgetTab? active = this.tabs.FirstOrDefault( tab => StringComparer.Ordinal.Equals( tab.Key, this.ActiveKey ) );
		if ( active is null ) return false;
		if ( input.KeyCode is WidgetKey.Enter or WidgetKey.Space ) { this.RequestActivation( active ); return true; }
		if ( WidgetKey.Delete == input.KeyCode && active.IsClosable ) { this.RequestClose( active ); return true; }
		return false;
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		screen.StandardWindow.FillRectangle( area.Row, area.Column, 1, area.Columns, CursesCell.Blank( theme.Normal ) );
		foreach ( Segment segment in this.segments ) {
			WidgetTab tab = this.tabs[ segment.Index ]; int column = this.Bounds.Column + segment.Column;
			if ( column < area.Column || column >= area.Column + area.Columns ) continue;
			string label = $" {tab.Caption}{( tab.IsModified ? " ●" : "" )}{( tab.IsClosable ? " ×" : "" )} ";
			string clipped = CursesText.TruncateToColumns( label, Math.Min( segment.Columns, area.Column + area.Columns - column ), screen.TextWidthProvider );
			screen.StandardWindow.Move( area.Row, column );
			screen.StandardWindow.Write( clipped, !tab.IsEnabled ? theme.Disabled : StringComparer.Ordinal.Equals( tab.Key, this.ActiveKey ) && focused ? theme.Focused : theme.Normal );
		}
		if ( this.overflowColumn >= 0 && this.Bounds.Column + this.overflowColumn < area.Column + area.Columns ) {
			screen.StandardWindow.Move( area.Row, this.Bounds.Column + this.overflowColumn ); screen.StandardWindow.Write( "›", theme.Normal );
		}
	}

	private bool RequestNeighbor( int direction ) {
		if ( this.tabs.Length == 0 ) return false;
		int index = Array.FindIndex( this.tabs, tab => StringComparer.Ordinal.Equals( tab.Key, this.ActiveKey ) );
		for ( int distance = 1; distance <= this.tabs.Length; distance++ ) {
			int next = ( index + direction * distance % this.tabs.Length + this.tabs.Length ) % this.tabs.Length;
			if ( this.tabs[ next ].IsEnabled ) { this.RequestActivation( this.tabs[ next ] ); return true; }
		}
		return false;
	}

	private void RequestActivation( WidgetTab tab ) { this.EnsureActionCapacity(); this.EmitAction( new WidgetTabActivationRequestedAction( this.Id, tab.Key ) ); }
	private void RequestClose( WidgetTab tab ) { this.EnsureActionCapacity(); this.EmitAction( new WidgetTabCloseRequestedAction( this.Id, tab.Key ) ); }

	private void Layout() {
		this.segments = []; this.visibleKeys = []; this.overflowColumn = -1;
		int columns = Math.Max( 0, this.Bounds.Columns ); if ( columns == 0 || this.tabs.Length == 0 ) return;
		ICursesTextWidthProvider width = this.Host?.Screen.TextWidthProvider ?? new CursesScreen( 1, 1 ).TextWidthProvider;
		int[] lengths = this.tabs.Select( tab => Math.Max( 1, CursesText.MeasureColumns( $" {tab.Caption}{( tab.IsModified ? " ●" : "" )}{( tab.IsClosable ? " ×" : "" )} ", width ) ) ).ToArray();
		int active = Array.FindIndex( this.tabs, tab => StringComparer.Ordinal.Equals( tab.Key, this.ActiveKey ) );
		if ( active < 0 ) active = 0;
		int start = active, end = active, used = Math.Min( columns, lengths[ active ] );
		while ( start > 0 && used + lengths[ start - 1 ] <= columns ) used += lengths[ --start ];
		while ( end + 1 < this.tabs.Length && used + lengths[ end + 1 ] <= columns ) used += lengths[ ++end ];
		bool overflow = start > 0 || end + 1 < this.tabs.Length;
		int available = overflow && columns > 1 ? columns - 1 : columns;
		while ( start < active && used > available ) used -= lengths[ start++ ];
		while ( end > active && used > available ) used -= lengths[ end-- ];
		List<Segment> result = []; int at = 0;
		for ( int index = start; index <= end && at < available; index++ ) {
			int length = Math.Min( lengths[ index ], available - at );
			result.Add( new Segment( index, at, length, this.tabs[ index ].IsClosable && length == lengths[ index ] ? at + length - 2 : -1 ) ); at += length;
		}
		this.segments = result.ToArray(); this.visibleKeys = result.Select( segment => this.tabs[ segment.Index ].Key ).ToArray();
		if ( overflow && columns > 1 ) this.overflowColumn = columns - 1;
	}
}
