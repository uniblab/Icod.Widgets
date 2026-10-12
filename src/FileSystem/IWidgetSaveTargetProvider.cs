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

/// <summary>An optional application-owned capability for observing a proposed save path.</summary>
public interface IWidgetSaveTargetProvider {
	ValueTask<WidgetSaveTargetObservation> InspectTargetAsync( string directory, string fileName, CancellationToken token );
}

/// <summary>A momentary observation, not an authorization to overwrite or create.</summary>
public sealed record WidgetSaveTargetObservation( string TargetPath, bool Exists, bool IsValid, string? Error = null );
