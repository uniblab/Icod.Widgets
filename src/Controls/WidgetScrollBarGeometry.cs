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

internal enum WidgetScrollBarPointerTarget {
	None,
	DecrementButton,
	IncrementButton,
	PageBefore,
	Thumb,
	PageAfter
}

internal readonly record struct WidgetScrollBarGeometry(
	int Length,
	int TrackStart,
	int TrackLength,
	int ThumbStart,
	int ThumbLength,
	long MaximumOffset
) {
	internal static WidgetScrollBarGeometry Create( int length, long extent, long viewport, long offset ) {
		if ( 0 > length ) throw new ArgumentOutOfRangeException( nameof( length ) );
		if ( 0 > extent ) throw new ArgumentOutOfRangeException( nameof( extent ) );
		if ( 0 > viewport ) throw new ArgumentOutOfRangeException( nameof( viewport ) );
		if ( 0 > offset ) throw new ArgumentOutOfRangeException( nameof( offset ) );
		long maximumOffset = extent <= viewport ? 0 : extent - viewport;
		if ( 2 > length ) return new WidgetScrollBarGeometry( length, 0, 0, 0, 0, maximumOffset );
		if ( 2 == length ) return new WidgetScrollBarGeometry( length, 1, 0, 1, 0, maximumOffset );
		int trackLength = length - 2;
		if ( 0 == extent || extent <= viewport ) return new WidgetScrollBarGeometry( length, 1, trackLength, 1, trackLength, maximumOffset );
		int thumbLength = Math.Clamp( checked( (int)Scale( viewport, trackLength, extent ) ), 1, trackLength );
		int travel = trackLength - thumbLength;
		long clampedOffset = Math.Min( offset, maximumOffset );
		int thumbStart = 1 + ( 0 == travel ? 0 : checked( (int)Scale( clampedOffset, travel, maximumOffset ) ) );
		return new WidgetScrollBarGeometry( length, 1, trackLength, thumbStart, thumbLength, maximumOffset );
	}

	internal WidgetScrollBarPointerTarget HitTest( int coordinate ) {
		if ( 2 > this.Length || 0 > coordinate || coordinate >= this.Length ) return WidgetScrollBarPointerTarget.None;
		if ( 0 == coordinate ) return WidgetScrollBarPointerTarget.DecrementButton;
		if ( this.Length - 1 == coordinate ) return WidgetScrollBarPointerTarget.IncrementButton;
		if ( coordinate < this.ThumbStart ) return WidgetScrollBarPointerTarget.PageBefore;
		if ( coordinate < this.ThumbStart + this.ThumbLength ) return WidgetScrollBarPointerTarget.Thumb;
		return WidgetScrollBarPointerTarget.PageAfter;
	}

	internal long OffsetForDrag( int originCoordinate, long originOffset, int coordinate ) {
		int travel = this.TrackLength - this.ThumbLength;
		if ( 0 >= travel || 0 == this.MaximumOffset ) return Math.Clamp( originOffset, 0, this.MaximumOffset );
		long delta = ScaleSigned( coordinate - originCoordinate, this.MaximumOffset, travel );
		return Math.Clamp( SaturatingAdd( originOffset, delta ), 0, this.MaximumOffset );
	}

	private static long Scale( long value, long numerator, long denominator ) {
		if ( 0 == value || 0 == numerator || 0 == denominator ) return 0;
		return (long)( (BigInteger)value * numerator / denominator );
	}

	private static long ScaleSigned( int value, long numerator, int denominator ) {
		if ( 0 == value ) return 0;
		BigInteger result = (BigInteger)value * numerator / denominator;
		return result > long.MaxValue ? long.MaxValue : result < long.MinValue ? long.MinValue : (long)result;
	}

	private static long SaturatingAdd( long left, long right ) {
		if ( 0 < right && left > long.MaxValue - right ) return long.MaxValue;
		if ( long.MinValue == right || 0 > right && left < -right ) return 0;
		return left + right;
	}
}
