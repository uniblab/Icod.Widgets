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

/// <summary>Bounds storage, transactions, history, and retained snapshots for a text document.</summary>
public sealed record WidgetTextDocumentOptions {
	public const long MaximumSupportedDocumentLength = 1_073_741_823;
	public const int MaximumSupportedSingleEditLength = 268_435_456;
	public const int MaximumSupportedHistoryGroups = 65_536;
	public const long MaximumSupportedHistoryRetainedUnits = 536_870_912;
	public const int MaximumSupportedSnapshots = 128;
	public const int MaximumSupportedTextElementScan = 65_536;

	public long MaximumDocumentLength { get; init; } = 268_435_456;
	public int MaximumSingleEditLength { get; init; } = 16_777_216;
	public int MaximumHistoryGroups { get; init; } = 4_096;
	public long MaximumHistoryRetainedUnits { get; init; } = 67_108_864;
	public int MaximumSnapshots { get; init; } = 16;
	public int MaximumTextElementScan { get; init; } = 4_096;

	internal void Validate() {
		if ( this.MaximumDocumentLength is < 0 or > MaximumSupportedDocumentLength ) throw new ArgumentOutOfRangeException( nameof( this.MaximumDocumentLength ) );
		if ( this.MaximumSingleEditLength is < 0 or > MaximumSupportedSingleEditLength ) throw new ArgumentOutOfRangeException( nameof( this.MaximumSingleEditLength ) );
		if ( this.MaximumHistoryGroups is < 0 or > MaximumSupportedHistoryGroups ) throw new ArgumentOutOfRangeException( nameof( this.MaximumHistoryGroups ) );
		if ( this.MaximumHistoryRetainedUnits is < 0 or > MaximumSupportedHistoryRetainedUnits ) throw new ArgumentOutOfRangeException( nameof( this.MaximumHistoryRetainedUnits ) );
		if ( this.MaximumSnapshots is < 1 or > MaximumSupportedSnapshots ) throw new ArgumentOutOfRangeException( nameof( this.MaximumSnapshots ) );
		if ( this.MaximumTextElementScan is < 16 or > MaximumSupportedTextElementScan ) throw new ArgumentOutOfRangeException( nameof( this.MaximumTextElementScan ) );
	}
}
