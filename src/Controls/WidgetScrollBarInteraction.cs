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

internal enum WidgetScrollCommandKind {
	UnitDecrement,
	UnitIncrement,
	PageDecrement,
	PageIncrement,
	Start,
	End,
	Absolute,
	ThumbTrack
}

internal readonly record struct WidgetScrollCommand( WidgetScrollCommandKind Kind, long Value = 1 );

internal readonly record struct WidgetScrollInteractionResult( bool Handled, WidgetScrollCommand? Command = null );

internal sealed class WidgetScrollBarInteraction {
	internal const int MaximumWorkPerUpdate = 64;
	private static readonly TimeSpan InitialRepeatDelay = TimeSpan.FromMilliseconds( 400 );
	private static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds( 50 );
	private bool dragging;
	private int dragCoordinate;
	private long dragOffset;
	private WidgetScrollCommand? repeatCommand;
	private int repeatCoordinate;
	private TimeSpan nextRepeat;

	internal WidgetScrollBarPointerTarget HoverTarget { get; private set; }
	internal WidgetScrollBarPointerTarget PressedTarget { get; private set; }

	internal WidgetScrollInteractionResult Press(
		WidgetScrollBarGeometry geometry,
		int coordinate,
		bool shift,
		long offset,
		TimeSpan timestamp ) {
		this.Cancel();
		WidgetScrollBarPointerTarget target = geometry.HitTest( coordinate );
		this.HoverTarget = target;
		this.PressedTarget = target;
		if ( shift && target is WidgetScrollBarPointerTarget.PageBefore or WidgetScrollBarPointerTarget.PageAfter ) {
			return new WidgetScrollInteractionResult( true,
				new WidgetScrollCommand( WidgetScrollCommandKind.Absolute, geometry.OffsetForCoordinate( coordinate ) ) );
		}
		return target switch {
			WidgetScrollBarPointerTarget.DecrementButton => this.BeginRepeat( WidgetScrollCommandKind.UnitDecrement, coordinate, timestamp ),
			WidgetScrollBarPointerTarget.IncrementButton => this.BeginRepeat( WidgetScrollCommandKind.UnitIncrement, coordinate, timestamp ),
			WidgetScrollBarPointerTarget.PageBefore => this.BeginRepeat( WidgetScrollCommandKind.PageDecrement, coordinate, timestamp ),
			WidgetScrollBarPointerTarget.PageAfter => this.BeginRepeat( WidgetScrollCommandKind.PageIncrement, coordinate, timestamp ),
			WidgetScrollBarPointerTarget.Thumb => this.BeginDrag( coordinate, offset ),
			_ => new WidgetScrollInteractionResult( false )
		};
	}

	internal WidgetScrollInteractionResult Move( WidgetScrollBarGeometry geometry, int coordinate ) {
		this.HoverTarget = geometry.HitTest( coordinate );
		return this.dragging
			? new WidgetScrollInteractionResult( true, new WidgetScrollCommand( WidgetScrollCommandKind.ThumbTrack,
				geometry.OffsetForDrag( this.dragCoordinate, this.dragOffset, coordinate ) ) )
			: new WidgetScrollInteractionResult( false );
	}

	internal WidgetScrollInteractionResult Release( WidgetScrollBarGeometry geometry, int coordinate ) {
		WidgetScrollInteractionResult result = this.dragging
			? new WidgetScrollInteractionResult( true, new WidgetScrollCommand( WidgetScrollCommandKind.ThumbTrack,
				geometry.OffsetForDrag( this.dragCoordinate, this.dragOffset, coordinate ) ) )
			: new WidgetScrollInteractionResult( WidgetScrollBarPointerTarget.None != this.PressedTarget );
		this.Cancel();
		this.HoverTarget = geometry.HitTest( coordinate );
		return result;
	}

	internal void Hover( WidgetScrollBarGeometry geometry, int coordinate ) => this.HoverTarget = geometry.HitTest( coordinate );

	internal bool TryRepeat( TimeSpan timestamp, WidgetScrollBarGeometry geometry, out WidgetScrollCommand command ) {
		if ( this.repeatCommand is not { } pending || timestamp < this.nextRepeat ) { command = default; return false; }
		if ( this.PressedTarget is WidgetScrollBarPointerTarget.PageBefore or WidgetScrollBarPointerTarget.PageAfter
			&& geometry.HitTest( this.repeatCoordinate ) != this.PressedTarget ) {
			this.repeatCommand = null; this.PressedTarget = WidgetScrollBarPointerTarget.None; command = default; return false;
		}
		this.nextRepeat += RepeatInterval;
		command = pending;
		return true;
	}

	internal void Cancel() {
		this.dragging = false;
		this.repeatCommand = null;
		this.HoverTarget = WidgetScrollBarPointerTarget.None;
		this.PressedTarget = WidgetScrollBarPointerTarget.None;
	}

	private WidgetScrollInteractionResult BeginRepeat( WidgetScrollCommandKind kind, int coordinate, TimeSpan timestamp ) {
		WidgetScrollCommand command = new( kind );
		this.repeatCommand = command;
		this.repeatCoordinate = coordinate;
		this.nextRepeat = timestamp + InitialRepeatDelay;
		return new WidgetScrollInteractionResult( true, command );
	}

	private WidgetScrollInteractionResult BeginDrag( int coordinate, long offset ) {
		this.dragging = true;
		this.dragCoordinate = coordinate;
		this.dragOffset = offset;
		return new WidgetScrollInteractionResult( true );
	}
}
