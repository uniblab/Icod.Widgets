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

using System.Numerics;
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
		( int thumbStart, int thumbLength ) = this.GetThumb( length );
		CursesStyle track = this.Style( theme, !this.IsEffectivelyEnabled ? WidgetStyleRole.Disabled : WidgetStyleRole.ScrollTrack );
		CursesStyle thumb = this.Style( theme, !this.IsEffectivelyEnabled ? WidgetStyleRole.Disabled : focused ? WidgetStyleRole.ScrollFocusedThumb : WidgetStyleRole.ScrollThumb );
		screen.StandardWindow.FillRectangle( area.Row, area.Column, area.Rows, area.Columns, CursesCell.Blank( track ) );
		for ( int index = 0; index < length; index++ ) {
			int row = WidgetScrollOrientation.Vertical == this.Orientation ? this.Bounds.Row + index : this.Bounds.Row;
			int column = WidgetScrollOrientation.Horizontal == this.Orientation ? this.Bounds.Column + index : this.Bounds.Column;
			if ( !screen.Bounds.Contains( row, column ) ) continue; screen.StandardWindow.Move( row, column );
			bool insideThumb = index >= thumbStart && index < thumbStart + thumbLength;
			screen.StandardWindow.Write( insideThumb ? "█" : "░", insideThumb ? thumb : track );
		}
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
			( int start, int length ) = this.GetThumb( this.TrackLength );
			if ( coordinate < start ) return this.SetUserOffset( SaturatingAdd( this.Offset, -this.Page ) );
			if ( coordinate >= start + length ) return this.SetUserOffset( SaturatingAdd( this.Offset, this.Page ) );
			this.dragging = true; this.dragCoordinate = coordinate; this.dragOffset = this.Offset; return true;
		}
		if ( CursesMouseAction.Move == pointer.Action && this.dragging ) return this.DragTo( coordinate );
		if ( CursesMouseAction.Release == pointer.Action && this.dragging ) { this.dragging = false; return this.DragTo( coordinate ); }
		return false;
	}

	private bool DragTo( int coordinate ) {
		( _, int thumbLength ) = this.GetThumb( this.TrackLength ); int travel = Math.Max( 0, this.TrackLength - thumbLength );
		if ( 0 == travel || 0 == this.MaximumOffset ) return true;
		long delta = ScaleSigned( coordinate - this.dragCoordinate, this.MaximumOffset, travel ); return this.SetUserOffset( SaturatingAdd( this.dragOffset, delta ) );
	}

	private bool SetUserOffset( long value ) {
		long target = Math.Clamp( value, 0, this.MaximumOffset ); if ( target == this.Offset ) return true;
		this.EnsureActionCapacity(); this.Offset = target; this.EmitAction( new WidgetScrollChangedAction( this.Id, target ) ); return true;
	}

	private ( int Start, int Length ) GetThumb( int trackLength ) {
		if ( 0 >= trackLength ) return ( 0, 0 ); if ( 0 == this.extent || this.extent <= this.viewport ) return ( 0, trackLength );
		int length = Math.Max( 1, checked( (int)Scale( this.viewport, trackLength, this.extent ) ) ); length = Math.Min( trackLength, length );
		int travel = trackLength - length; int start = 0 == travel ? 0 : checked( (int)Scale( this.Offset, travel, this.MaximumOffset ) ); return ( start, length );
	}

	private static long Scale( long value, long numerator, long denominator ) {
		if ( 0 == value || 0 == numerator || 0 == denominator ) return 0;
		return (long)( (BigInteger)value * numerator / denominator );
	}

	private static long ScaleSigned( int value, long numerator, int denominator ) {
		if ( 0 == value ) return 0; BigInteger result = (BigInteger)value * numerator / denominator;
		return result > long.MaxValue ? long.MaxValue : result < long.MinValue ? long.MinValue : (long)result;
	}

	private static long SaturatingAdd( long left, long right ) {
		if ( 0 < right && left > long.MaxValue - right ) return long.MaxValue;
		if ( long.MinValue == right || 0 > right && left < -right ) return 0;
		return left + right;
	}
}
