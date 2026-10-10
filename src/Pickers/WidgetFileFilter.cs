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

public sealed class WidgetFileFilter {
	public WidgetFileFilter( string name, IReadOnlyList<string> suffixes ) {
		if ( string.IsNullOrWhiteSpace( name ) ) throw new ArgumentException( "A filter name is required.", nameof( name ) ); ArgumentNullException.ThrowIfNull( suffixes );
		this.Name = name; this.Suffixes = suffixes.ToArray(); if ( this.Suffixes.Any( suffix => string.IsNullOrEmpty( suffix ) ) ) throw new ArgumentException( "Filter suffixes cannot be empty.", nameof( suffixes ) );
	}
	private WidgetFileFilter() { this.Name = "all"; this.Suffixes = []; this.IsAllFiles = true; }
	public string Name { get; }
	public IReadOnlyList<string> Suffixes { get; }
	public bool IsAllFiles { get; }
	public static WidgetFileFilter AllFiles { get; } = new();
	internal bool Matches( string path, StringComparer comparer ) {
		if ( this.IsAllFiles ) return true;
		foreach ( string suffix in this.Suffixes ) if ( path.Length >= suffix.Length && comparer.Equals( path[ ^suffix.Length.. ], suffix ) ) return true;
		return false;
	}
}

