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
	private readonly WidgetScrollBarInteraction interaction = new();
	private long extent;
	private long viewport;

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
		this.extent = extent; this.viewport = viewport; this.Offset = Math.Min( offset, this.MaximumOffset ); this.interaction.Cancel();
	}

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is { } pointer ) return this.OnPointer( pointer, host.CurrentTime );
		if ( WidgetInputKind.Key != input.Kind ) return false;
		WidgetScrollCommandKind? command = input.KeyCode switch {
			WidgetKey.Home => WidgetScrollCommandKind.Start,
			WidgetKey.End => WidgetScrollCommandKind.End,
			WidgetKey.PageUp => WidgetScrollCommandKind.PageDecrement,
			WidgetKey.PageDown => WidgetScrollCommandKind.PageIncrement,
			WidgetKey.Left or WidgetKey.Up => WidgetScrollCommandKind.UnitDecrement,
			WidgetKey.Right or WidgetKey.Down => WidgetScrollCommandKind.UnitIncrement,
			_ => null
		};
		return command.HasValue && this.ApplyCommand( new WidgetScrollCommand( command.Value ) );
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return; int length = this.TrackLength;
		CursesStyle track = this.ScrollStyle( theme, WidgetScrollBarPointerTarget.None, WidgetStyleRole.ScrollTrack );
		screen.StandardWindow.FillRectangle( area.Row, area.Column, area.Rows, area.Columns, CursesCell.Blank( track ) );
		WidgetScrollBarGeometry geometry = WidgetScrollBarGeometry.Create( length, this.extent, this.viewport, this.Offset );
		if ( 2 > length ) return;
		bool vertical = WidgetScrollOrientation.Vertical == this.Orientation;
		this.WriteCell( screen, 0, vertical ? "▲" : "◄",
			this.ScrollStyle( theme, WidgetScrollBarPointerTarget.DecrementButton, WidgetStyleRole.ScrollButton ) );
		for ( int coordinate = geometry.TrackStart; coordinate < geometry.TrackStart + geometry.TrackLength; coordinate++ ) {
			bool insideThumb = coordinate >= geometry.ThumbStart && coordinate < geometry.ThumbStart + geometry.ThumbLength;
			WidgetScrollBarPointerTarget target = geometry.HitTest( coordinate );
			WidgetStyleRole role = insideThumb ? focused ? WidgetStyleRole.ScrollFocusedThumb : WidgetStyleRole.ScrollThumb : WidgetStyleRole.ScrollTrack;
			this.WriteCell( screen, coordinate, vertical ? insideThumb ? "┃" : "│" : insideThumb ? "━" : "─",
				this.ScrollStyle( theme, target, role ) );
		}
		this.WriteCell( screen, length - 1, vertical ? "▼" : "►",
			this.ScrollStyle( theme, WidgetScrollBarPointerTarget.IncrementButton, WidgetStyleRole.ScrollButton ) );
	}

	protected override void OnArrange( CursesRectangle bounds ) => this.interaction.Cancel();
	protected override void OnCaptureCanceled() => this.interaction.Cancel();
	internal override void OnDetached() => this.interaction.Cancel();
	protected override void OnUpdate( TimeSpan timestamp ) {
		for ( int work = 0; work < WidgetScrollBarInteraction.MaximumWorkPerUpdate; work++ ) {
			WidgetScrollBarGeometry geometry = WidgetScrollBarGeometry.Create( this.TrackLength, this.extent, this.viewport, this.Offset );
			if ( !this.interaction.TryRepeat( timestamp, geometry, out WidgetScrollCommand command ) ) break;
			_ = this.ApplyCommand( command );
		}
	}

	private long MaximumOffset => this.extent <= this.viewport ? 0 : this.extent - this.viewport;
	private long Page => Math.Max( 1, this.viewport );
	private int TrackLength => WidgetScrollOrientation.Horizontal == this.Orientation ? this.Bounds.Columns : this.Bounds.Rows;

	private bool OnPointer( CursesMouseEvent pointer, TimeSpan timestamp ) {
		if ( pointer.Action is CursesMouseAction.WheelUp or CursesMouseAction.WheelDown ) {
			if ( WidgetScrollOrientation.Vertical != this.Orientation ) return false;
			return this.ApplyCommand( new WidgetScrollCommand( CursesMouseAction.WheelUp == pointer.Action
				? WidgetScrollCommandKind.UnitDecrement : WidgetScrollCommandKind.UnitIncrement, 3 ) );
		}
		if ( pointer.Action is CursesMouseAction.WheelLeft or CursesMouseAction.WheelRight ) {
			if ( WidgetScrollOrientation.Horizontal != this.Orientation ) return false;
			return this.ApplyCommand( new WidgetScrollCommand( CursesMouseAction.WheelLeft == pointer.Action
				? WidgetScrollCommandKind.UnitDecrement : WidgetScrollCommandKind.UnitIncrement, 3 ) );
		}
		int coordinate = WidgetScrollOrientation.Horizontal == this.Orientation ? pointer.Column - this.Bounds.Column : pointer.Row - this.Bounds.Row;
		WidgetScrollBarGeometry geometry = WidgetScrollBarGeometry.Create( this.TrackLength, this.extent, this.viewport, this.Offset );
		if ( CursesMouseAction.Move == pointer.Action && CursesMouseButton.Primary != pointer.Button ) {
			this.interaction.Hover( geometry, coordinate ); return false;
		}
		if ( CursesMouseButton.Primary != pointer.Button ) return false;
		if ( CursesMouseAction.Press == pointer.Action ) {
			WidgetScrollInteractionResult result = this.interaction.Press( geometry, coordinate,
				0 != ( pointer.Modifiers & CursesKeyModifiers.Shift ), this.Offset, timestamp );
			return this.ApplyResult( result );
		}
		if ( CursesMouseAction.Move == pointer.Action ) return this.ApplyResult( this.interaction.Move( geometry, coordinate ) );
		if ( CursesMouseAction.Release == pointer.Action ) return this.ApplyResult( this.interaction.Release( geometry, coordinate ) );
		return false;
	}

	private bool ApplyResult( WidgetScrollInteractionResult result ) {
		if ( result.Command is { } command ) _ = this.ApplyCommand( command );
		return result.Handled;
	}

	private bool ApplyCommand( WidgetScrollCommand command ) {
		long amount = Math.Max( 1, command.Value );
		long target = command.Kind switch {
			WidgetScrollCommandKind.UnitDecrement => SaturatingAdd( this.Offset, -amount ),
			WidgetScrollCommandKind.UnitIncrement => SaturatingAdd( this.Offset, amount ),
			WidgetScrollCommandKind.PageDecrement => SaturatingAdd( this.Offset, -SaturatingMultiply( this.Page, amount ) ),
			WidgetScrollCommandKind.PageIncrement => SaturatingAdd( this.Offset, SaturatingMultiply( this.Page, amount ) ),
			WidgetScrollCommandKind.Start => 0,
			WidgetScrollCommandKind.End => this.MaximumOffset,
			WidgetScrollCommandKind.Absolute or WidgetScrollCommandKind.ThumbTrack => command.Value,
			_ => throw new ArgumentOutOfRangeException( nameof( command ) )
		};
		return this.SetUserOffset( target );
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

	private static long SaturatingMultiply( long left, long right ) =>
		0 == left || 0 == right ? 0 : left > long.MaxValue / right ? long.MaxValue : left * right;

	private CursesStyle ScrollStyle( WidgetTheme theme, WidgetScrollBarPointerTarget target, WidgetStyleRole role ) {
		if ( !this.IsEffectivelyEnabled ) return this.Style( theme, WidgetStyleRole.Disabled );
		if ( WidgetScrollBarPointerTarget.None != target && target == this.interaction.PressedTarget )
			return this.ScrollPartStyle( theme, WidgetStyleRole.ScrollPressed, WidgetStyleRole.ScrollFocusedThumb );
		if ( WidgetScrollBarPointerTarget.None != target && target == this.interaction.HoverTarget )
			return this.ScrollPartStyle( theme, WidgetStyleRole.ScrollHover, WidgetStyleRole.ScrollFocusedThumb );
		return WidgetStyleRole.ScrollButton == role
			? this.ScrollPartStyle( theme, WidgetStyleRole.ScrollButton, WidgetStyleRole.ScrollTrack )
			: this.Style( theme, role );
	}

	private CursesStyle ScrollPartStyle( WidgetTheme theme, WidgetStyleRole role, WidgetStyleRole fallback ) =>
		this.HasPartStyle( theme, role ) ? this.Style( theme, role ) : this.Style( theme, fallback );

	private void WriteCell( CursesScreen screen, int coordinate, string content, CursesStyle style ) {
		int row = this.Bounds.Row + ( WidgetScrollOrientation.Vertical == this.Orientation ? coordinate : 0 );
		int column = this.Bounds.Column + ( WidgetScrollOrientation.Horizontal == this.Orientation ? coordinate : 0 );
		if ( !screen.Bounds.Contains( row, column ) ) return;
		screen.StandardWindow.Move( row, column ); screen.StandardWindow.Write( content, style );
	}
}
