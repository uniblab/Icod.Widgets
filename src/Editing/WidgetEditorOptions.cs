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

/// <summary>Bounds editor projection and configures its text policy.</summary>
public sealed record WidgetEditorOptions {
	public const int MaximumSupportedProjectionSourceUnits = 1_048_576;
	public const int MaximumSupportedClusterScan = 65_536;
	public const int MaximumSupportedCachedRows = 4_096;
	public const int MaximumSupportedCachedSourceUnits = 16_777_216;
	public const int MaximumSupportedWorkPerUpdate = 1_024;
	public const int MaximumSupportedPendingPasteUnits = WidgetTextDocumentOptions.MaximumSupportedSingleEditLength;
	public const int MaximumSupportedClipboardBytes = 16_777_216;

	public CursesTextWrapMode WrapMode { get; init; } = CursesTextWrapMode.NoWrap;
	public int TabWidth { get; init; } = 4;
	public int MaximumProjectionSourceUnits { get; init; } = 65_536;
	public int MaximumClusterScan { get; init; } = 4_096;
	public int MaximumCachedRows { get; init; } = 512;
	public int MaximumCachedSourceUnits { get; init; } = 1_048_576;
	public int MaximumWorkPerUpdate { get; init; } = 64;
	public int MaximumPendingPasteUnits { get; init; } = 16_777_216;
	public int MaximumClipboardBytes { get; init; } = 65_536;
	public string NewLine { get; init; } = "\n";
	public string Indentation { get; init; } = "\t";
	public bool AutoIndent { get; init; } = true;
	public bool AllowTypingCoalescence { get; init; } = true;
	/// <summary>Gets whether every visible row of the caret's logical line uses the themed current-line style.</summary>
	public bool HighlightCurrentLine { get; init; }
	/// <summary>Gets whether the editor reserves a fixed left gutter for one-based logical line numbers.</summary>
	public bool ShowLineNumbers { get; init; }

	internal void Validate() {
		if ( !Enum.IsDefined( this.WrapMode ) ) throw new ArgumentOutOfRangeException( nameof( this.WrapMode ) );
		if ( this.TabWidth is < 1 or > 256 ) throw new ArgumentOutOfRangeException( nameof( this.TabWidth ) );
		if ( this.MaximumProjectionSourceUnits is < 16 or > MaximumSupportedProjectionSourceUnits ) throw new ArgumentOutOfRangeException( nameof( this.MaximumProjectionSourceUnits ) );
		if ( this.MaximumClusterScan is < 16 or > MaximumSupportedClusterScan ) throw new ArgumentOutOfRangeException( nameof( this.MaximumClusterScan ) );
		if ( this.MaximumCachedRows is < 1 or > MaximumSupportedCachedRows ) throw new ArgumentOutOfRangeException( nameof( this.MaximumCachedRows ) );
		if ( this.MaximumCachedSourceUnits is < 16 or > MaximumSupportedCachedSourceUnits ) throw new ArgumentOutOfRangeException( nameof( this.MaximumCachedSourceUnits ) );
		if ( this.MaximumWorkPerUpdate is < 1 or > MaximumSupportedWorkPerUpdate ) throw new ArgumentOutOfRangeException( nameof( this.MaximumWorkPerUpdate ) );
		if ( this.MaximumPendingPasteUnits is < 0 or > MaximumSupportedPendingPasteUnits ) throw new ArgumentOutOfRangeException( nameof( this.MaximumPendingPasteUnits ) );
		if ( this.MaximumClipboardBytes is < 0 or > MaximumSupportedClipboardBytes ) throw new ArgumentOutOfRangeException( nameof( this.MaximumClipboardBytes ) );
		if ( this.NewLine is not "\n" and not "\r" and not "\r\n" ) throw new ArgumentException( "The inserted newline must be LF, CR, or CRLF.", nameof( this.NewLine ) );
		if ( this.Indentation is null || this.Indentation.Any( static value => value is not ' ' and not '\t' ) ) throw new ArgumentException( "Indentation may contain only spaces and tabs.", nameof( this.Indentation ) );
	}
}
