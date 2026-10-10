/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using System.Text;
using Icod.DCurses;

/// <summary>Provides bounded, stable-key single or multiple selection.</summary>
public class WidgetList : Widget {
	private WidgetListItem[] items = [];
	private readonly HashSet<string> selected = new( StringComparer.Ordinal );
	private int cursorIndex = -1;
	private int anchorIndex = -1;
	private int scrollOffset;
	private string? pressedKey;
	private string search = string.Empty;
	private TimeSpan? lastSearchTime;

	public WidgetList( string id, WidgetSelectionMode selectionMode ) : base( id ) {
		if ( !Enum.IsDefined( selectionMode ) ) throw new ArgumentOutOfRangeException( nameof( selectionMode ) );
		this.SelectionMode = selectionMode;
	}

	public override bool IsFocusable => true;
	public WidgetSelectionMode SelectionMode { get; }
	public IReadOnlyList<WidgetListItem> Items => this.items;
	public string? CursorKey => this.cursorIndex >= 0 ? this.items[ this.cursorIndex ].Key : null;
	public IReadOnlyList<string> SelectedKeys => this.items.Where( item => this.selected.Contains( item.Key ) ).Select( item => item.Key ).ToArray();
	public int LastRenderVisitedCount { get; private set; }

	public void SetItems( IReadOnlyList<WidgetListItem> items ) {
		ArgumentNullException.ThrowIfNull( items );
		if ( items.Count > WidgetLimits.MaximumItems ) throw new ArgumentOutOfRangeException( nameof( items ) );
		WidgetListItem[] replacement = items.ToArray();
		HashSet<string> keys = new( StringComparer.Ordinal );
		foreach ( WidgetListItem item in replacement ) {
			if ( item is null || string.IsNullOrWhiteSpace( item.Key ) || item.Text is null ) throw new ArgumentException( "List items require a key and text.", nameof( items ) );
			if ( !keys.Add( item.Key ) ) throw new ArgumentException( $"Duplicate list key '{item.Key}'.", nameof( items ) );
		}
		string? oldCursor = this.CursorKey;
		int oldCursorIndex = this.cursorIndex;
		string[] oldSelection = this.SelectedKeys.ToArray();
		HashSet<string> enabled = replacement.Where( item => item.IsEnabled ).Select( item => item.Key ).ToHashSet( StringComparer.Ordinal );
		string[] nextSelection = replacement.Where( item => enabled.Contains( item.Key ) && this.selected.Contains( item.Key ) ).Select( item => item.Key ).ToArray();
		if ( !oldSelection.SequenceEqual( nextSelection ) ) this.Host?.EnsureActionCapacity();
		this.selected.IntersectWith( enabled );
		if ( WidgetSelectionMode.Single == this.SelectionMode && 1 < this.selected.Count ) {
			string? first = replacement.FirstOrDefault( item => this.selected.Contains( item.Key ) )?.Key;
			this.selected.Clear(); if ( first is not null ) this.selected.Add( first );
		}
		this.items = replacement;
		this.cursorIndex = oldCursor is null ? -1 : Array.FindIndex( replacement, item => item.IsEnabled && item.Key == oldCursor );
		if ( 0 > this.cursorIndex ) this.cursorIndex = FindNearestEnabled( replacement, oldCursorIndex );
		this.anchorIndex = this.cursorIndex;
		this.EnsureVisible();
		if ( !oldSelection.SequenceEqual( this.SelectedKeys, StringComparer.Ordinal ) && this.Host is not null ) this.EmitSelection();
	}

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( input.Kind == WidgetInputKind.Pointer && input.Pointer is { Button: CursesMouseButton.Primary } pointer ) {
			int index = this.scrollOffset + pointer.Row - this.Bounds.Row;
			if ( !this.Bounds.Contains( pointer.Row, pointer.Column ) || index < 0 || index >= this.items.Length || !this.items[ index ].IsEnabled ) { this.pressedKey = null; return false; }
			if ( pointer.Action == CursesMouseAction.Press ) { this.cursorIndex = index; this.pressedKey = this.CursorKey; return true; }
			if ( pointer.Action == CursesMouseAction.Release ) { bool matches = this.pressedKey == this.items[ index ].Key; this.pressedKey = null; if ( !matches ) return false; this.cursorIndex = index; return this.SelectionMode == WidgetSelectionMode.Single ? this.CommitCurrent() : this.ToggleCurrent(); }
		}
		if ( WidgetInputKind.Text == input.Kind && input.Character.HasValue ) return this.Search( input.Character.Value, host.CurrentTime );
		if ( WidgetInputKind.Key != input.Kind ) return false;
		if ( WidgetKey.Character == input.KeyCode && input.Character.HasValue && ( input.Modifiers & ( WidgetInputModifiers.Control | WidgetInputModifiers.Alt ) ) == 0 ) return this.Search( input.Character.Value, host.CurrentTime );
		if ( WidgetKey.Space == input.KeyCode && WidgetSelectionMode.Multiple == this.SelectionMode ) return this.ToggleCurrent();
		if ( WidgetKey.Enter == input.KeyCode && WidgetSelectionMode.Single == this.SelectionMode ) return this.CommitCurrent();
		if ( WidgetKey.Character == input.KeyCode && 0 != ( input.Modifiers & WidgetInputModifiers.Control ) && input.Character is { Value: 65 or 97 } && WidgetSelectionMode.Multiple == this.SelectionMode ) {
			host.EnsureActionCapacity(); this.selected.Clear(); foreach ( WidgetListItem item in this.items ) if ( item.IsEnabled ) this.selected.Add( item.Key ); this.EmitSelection(); return true;
		}
		int next = input.KeyCode switch {
			WidgetKey.Up => this.FindEnabled( this.cursorIndex - 1, -1 ),
			WidgetKey.Down => this.FindEnabled( this.cursorIndex + 1, 1 ),
			WidgetKey.Home => this.FindEnabled( 0, 1 ),
			WidgetKey.End => this.FindEnabled( this.items.Length - 1, -1 ),
			WidgetKey.PageUp => this.FindEnabled( Math.Max( 0, this.cursorIndex - Math.Max( 1, this.Bounds.Rows ) ), -1 ),
			WidgetKey.PageDown => this.FindEnabled( Math.Min( this.items.Length - 1, this.cursorIndex + Math.Max( 1, this.Bounds.Rows ) ), 1 ),
			_ => this.cursorIndex
		};
		if ( next < 0 || next == this.cursorIndex ) return false;
		if ( 0 != ( input.Modifiers & WidgetInputModifiers.Shift ) && WidgetSelectionMode.Multiple == this.SelectionMode ) this.SelectRange( next );
		this.cursorIndex = next; if ( 0 == ( input.Modifiers & WidgetInputModifiers.Shift ) ) this.anchorIndex = next;
		this.EnsureVisible(); return true;
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); this.LastRenderVisitedCount = 0; if ( area.IsEmpty ) return;
		for ( int row = 0; row < area.Rows; row++ ) {
			int index = this.scrollOffset + row; if ( index >= this.items.Length ) break;
			WidgetListItem item = this.items[ index ]; this.LastRenderVisitedCount++;
			string marker = this.selected.Contains( item.Key ) ? "* " : "  ";
			string text = CursesText.TruncateToColumns( marker + item.Text, area.Columns, screen.TextWidthProvider );
			CursesStyle style = !this.IsEffectivelyEnabled || !item.IsEnabled ? theme.Disabled : index == this.cursorIndex && focused ? theme.Focused : this.selected.Contains( item.Key ) ? theme.Selected : theme.Normal;
			screen.StandardWindow.Move( area.Row + row, area.Column ); screen.StandardWindow.Write( text, style );
		}
	}

	private bool ToggleCurrent() {
		if ( this.cursorIndex < 0 || !this.items[ this.cursorIndex ].IsEnabled ) return false;
		this.Host?.EnsureActionCapacity();
		string key = this.items[ this.cursorIndex ].Key; if ( !this.selected.Add( key ) ) this.selected.Remove( key );
		this.anchorIndex = this.cursorIndex; this.EmitSelection(); return true;
	}

	private bool CommitCurrent() {
		if ( this.cursorIndex < 0 || !this.items[ this.cursorIndex ].IsEnabled ) return false;
		string key = this.items[ this.cursorIndex ].Key;
		if ( this.selected.Count == 1 && this.selected.Contains( key ) ) return true;
		this.Host?.EnsureActionCapacity();
		this.selected.Clear(); this.selected.Add( key ); this.anchorIndex = this.cursorIndex; this.EmitSelection(); return true;
	}

	private void SelectRange( int next ) {
		this.Host?.EnsureActionCapacity();
		if ( 0 > this.anchorIndex ) this.anchorIndex = this.cursorIndex;
		this.selected.Clear();
		for ( int index = Math.Min( this.anchorIndex, next ); index <= Math.Max( this.anchorIndex, next ); index++ ) if ( this.items[ index ].IsEnabled ) this.selected.Add( this.items[ index ].Key );
		this.EmitSelection();
	}

	private bool Search( Rune character, TimeSpan now ) {
		if ( this.lastSearchTime.HasValue && now - this.lastSearchTime.Value <= TimeSpan.FromMilliseconds( 750 ) ) this.search += character.ToString(); else this.search = character.ToString();
		this.lastSearchTime = now;
		for ( int offset = 1; offset <= this.items.Length; offset++ ) {
			int index = ( Math.Max( this.cursorIndex, -1 ) + offset ) % this.items.Length;
			if ( this.items[ index ].IsEnabled && this.items[ index ].Text.StartsWith( this.search, StringComparison.OrdinalIgnoreCase ) ) {
				this.cursorIndex = index; this.anchorIndex = index; this.EnsureVisible(); return true;
			}
		}
		return false;
	}

	private int FindEnabled( int start, int step ) {
		if ( 0 == this.items.Length ) return -1;
		for ( int index = Math.Clamp( start, 0, this.items.Length - 1 ); index >= 0 && index < this.items.Length; index += step ) if ( this.items[ index ].IsEnabled ) return index;
		return this.cursorIndex;
	}

	internal override void OnPointerCaptureCanceled() => this.pressedKey = null;

	private static int FindNearestEnabled( WidgetListItem[] items, int priorIndex ) {
		if ( items.Length == 0 ) return -1;
		int origin = Math.Clamp( priorIndex, 0, items.Length - 1 );
		for ( int distance = 0; distance < items.Length; distance++ ) {
			int before = origin - distance; if ( before >= 0 && items[ before ].IsEnabled ) return before;
			int after = origin + distance; if ( after < items.Length && items[ after ].IsEnabled ) return after;
		}
		return -1;
	}

	private void EnsureVisible() {
		int rows = Math.Max( 1, this.Bounds.Rows ); if ( this.cursorIndex < this.scrollOffset ) this.scrollOffset = this.cursorIndex;
		else if ( this.cursorIndex >= this.scrollOffset + rows ) this.scrollOffset = this.cursorIndex - rows + 1;
		this.scrollOffset = Math.Max( 0, this.scrollOffset );
	}

	private void EmitSelection() => this.Host?.Enqueue( new WidgetSelectionChangedAction( this.Id, this.SelectedKeys.ToArray() ) );
}
