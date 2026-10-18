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

public sealed record WidgetDialogAction( string Id, string Label, WidgetDialogOutcome Outcome ) {
	public char? Mnemonic { get; init; }
	public bool IsDefault { get; init; }
	public bool IsCancel { get; init; }
	public bool RequiresValidation { get; init; }
	public bool IsEnabled { get; init; } = true;
	public object? Value { get; init; }
}
