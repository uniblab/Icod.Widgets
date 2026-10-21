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

public enum WidgetScrollOrientation { Horizontal, Vertical }

/// <summary>Describes one editor or scrollbar range and whether its extent is exact.</summary>
public readonly record struct WidgetScrollRange( long Extent, long Viewport, long Offset, bool IsExtentExact );

/// <summary>Provides a keyboard- and pointer-operable scrollbar over a nonnegative long range.</summary>
public sealed class WidgetScrollBar : Widget {
	private long extent;
	private long viewport;
	private bool dragging;
	private int dragCoordinate;
	private long dragOffset;

	public WidgetScrollBar( string id, WidgetScrollOrientation orientation ) : base( id ) {
		if ( !Enum.IsDefined( orientation ) ) throw new ArgumentOutOfRangeException( nameof( orientation ) ); this.Orientation = orientation;
	}

	public override bool IsFocusable => true;
	public WidgetScrollOrientation Orientation { get; }
	public long Offset { get; private set; }

	public void SetRange( long extent, long viewport, long offset ) {
		if ( 0 > extent ) throw new ArgumentOutOfRangeException( nameof( extent ) );
		if ( 0 > viewport ) throw new ArgumentOutOfRangeException( nameof( viewport ) );
		if ( 0 > offset ) throw new ArgumentOutOfRangeException( nameof( offset ) );
		this.extent = extent; this.viewport = viewport; this.Offset = Math.Min( offset, this.MaximumOffset ); this.dragging = false;
	}

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is { } pointer ) return this.OnPointer( pointer );
		if ( WidgetInputKind.Key != input.Kind ) return false;
		long? target = input.KeyCode switch {
			WidgetKey.Home => 0,
			WidgetKey.End => this.MaximumOffset,
			WidgetKey.PageUp => SaturatingAdd( this.Offset, -this.Page ),
			WidgetKey.PageDown => SaturatingAdd( this.Offset, this.Page ),
			WidgetKey.Left when WidgetScrollOrientation.Horizontal == this.Orientation => SaturatingAdd( this.Offset, -1 ),
			WidgetKey.Right when WidgetScrollOrientation.Horizontal == this.Orientation => SaturatingAdd( this.Offset, 1 ),
			WidgetKey.Up when WidgetScrollOrientation.Vertical == this.Orientation => SaturatingAdd( this.Offset, -1 ),
			WidgetKey.Down when WidgetScrollOrientation.Vertical == this.Orientation => SaturatingAdd( this.Offset, 1 ),
			_ => null
		};
		return target.HasValue && this.SetUserOffset( target.Value );
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return; int length = this.TrackLength;
		CursesStyle track = this.Style( theme, !this.IsEffectivelyEnabled ? WidgetStyleRole.Disabled : WidgetStyleRole.ScrollTrack );
		CursesStyle thumb = this.Style( theme, !this.IsEffectivelyEnabled ? WidgetStyleRole.Disabled : focused ? WidgetStyleRole.ScrollFocusedThumb : WidgetStyleRole.ScrollThumb );
		screen.StandardWindow.FillRectangle( area.Row, area.Column, area.Rows, area.Columns, CursesCell.Blank( track ) );
		WidgetScrollBarGeometry geometry = WidgetScrollBarGeometry.Create( length, this.extent, this.viewport, this.Offset );
		if ( 2 > length ) return;
		bool vertical = WidgetScrollOrientation.Vertical == this.Orientation;
		this.WriteCell( screen, 0, vertical ? "▲" : "◄", track );
		for ( int coordinate = geometry.TrackStart; coordinate < geometry.TrackStart + geometry.TrackLength; coordinate++ ) {
			bool insideThumb = coordinate >= geometry.ThumbStart && coordinate < geometry.ThumbStart + geometry.ThumbLength;
			this.WriteCell( screen, coordinate, vertical ? insideThumb ? "┃" : "│" : insideThumb ? "━" : "─", insideThumb ? thumb : track );
		}
		this.WriteCell( screen, length - 1, vertical ? "▼" : "►", track );
	}

	protected override void OnCaptureCanceled() => this.dragging = false;

	private long MaximumOffset => this.extent <= this.viewport ? 0 : this.extent - this.viewport;
	private long Page => Math.Max( 1, this.viewport );
	private int TrackLength => WidgetScrollOrientation.Horizontal == this.Orientation ? this.Bounds.Columns : this.Bounds.Rows;

	private bool OnPointer( CursesMouseEvent pointer ) {
		if ( pointer.Action is CursesMouseAction.WheelUp or CursesMouseAction.WheelDown ) {
			if ( WidgetScrollOrientation.Vertical != this.Orientation ) return false;
			return this.SetUserOffset( SaturatingAdd( this.Offset, CursesMouseAction.WheelUp == pointer.Action ? -3 : 3 ) );
		}
		if ( pointer.Action is CursesMouseAction.WheelLeft or CursesMouseAction.WheelRight ) {
			if ( WidgetScrollOrientation.Horizontal != this.Orientation ) return false;
			return this.SetUserOffset( SaturatingAdd( this.Offset, CursesMouseAction.WheelLeft == pointer.Action ? -3 : 3 ) );
		}
		if ( CursesMouseButton.Primary != pointer.Button ) return false;
		int coordinate = WidgetScrollOrientation.Horizontal == this.Orientation ? pointer.Column - this.Bounds.Column : pointer.Row - this.Bounds.Row;
		if ( CursesMouseAction.Press == pointer.Action ) {
			WidgetScrollBarGeometry geometry = WidgetScrollBarGeometry.Create( this.TrackLength, this.extent, this.viewport, this.Offset );
			switch ( geometry.HitTest( coordinate ) ) {
				case WidgetScrollBarPointerTarget.DecrementButton: return this.SetUserOffset( SaturatingAdd( this.Offset, -1 ) );
				case WidgetScrollBarPointerTarget.IncrementButton: return this.SetUserOffset( SaturatingAdd( this.Offset, 1 ) );
				case WidgetScrollBarPointerTarget.PageBefore: return this.SetUserOffset( SaturatingAdd( this.Offset, -this.Page ) );
				case WidgetScrollBarPointerTarget.PageAfter: return this.SetUserOffset( SaturatingAdd( this.Offset, this.Page ) );
				case WidgetScrollBarPointerTarget.Thumb:
					this.dragging = true; this.dragCoordinate = coordinate; this.dragOffset = this.Offset; return true;
				default: return false;
			}
		}
		if ( CursesMouseAction.Move == pointer.Action && this.dragging ) return this.DragTo( coordinate );
		if ( CursesMouseAction.Release == pointer.Action && this.dragging ) { this.dragging = false; return this.DragTo( coordinate ); }
		return false;
	}

	private bool DragTo( int coordinate ) {
		WidgetScrollBarGeometry geometry = WidgetScrollBarGeometry.Create( this.TrackLength, this.extent, this.viewport, this.dragOffset );
		return this.SetUserOffset( geometry.OffsetForDrag( this.dragCoordinate, this.dragOffset, coordinate ) );
	}

	private bool SetUserOffset( long value ) {
		long target = Math.Clamp( value, 0, this.MaximumOffset ); if ( target == this.Offset ) return true;
		this.EnsureActionCapacity(); this.Offset = target; this.EmitAction( new WidgetScrollChangedAction( this.Id, target ) ); return true;
	}

	private static long SaturatingAdd( long left, long right ) {
		if ( 0 < right && left > long.MaxValue - right ) return long.MaxValue;
		if ( long.MinValue == right || 0 > right && left < -right ) return 0;
		return left + right;
	}

	private void WriteCell( CursesScreen screen, int coordinate, string content, CursesStyle style ) {
		int row = this.Bounds.Row + ( WidgetScrollOrientation.Vertical == this.Orientation ? coordinate : 0 );
		int column = this.Bounds.Column + ( WidgetScrollOrientation.Horizontal == this.Orientation ? coordinate : 0 );
		if ( !screen.Bounds.Contains( row, column ) ) return;
		screen.StandardWindow.Move( row, column ); screen.StandardWindow.Write( content, style );
	}
}
