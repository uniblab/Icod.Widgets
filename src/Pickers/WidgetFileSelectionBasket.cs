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

/// <summary>Maintains ordered, comparer-deduplicated file choices.</summary>
public sealed class WidgetFileSelectionBasket {
	private readonly StringComparer comparer;
	private readonly List<string> paths = [];
	internal event Action? Changed;
	public WidgetFileSelectionBasket( StringComparer comparer ) => this.comparer = comparer ?? throw new ArgumentNullException( nameof( comparer ) );
	public IReadOnlyList<string> Paths => this.paths.ToArray();
	public bool Toggle( string path ) { ArgumentException.ThrowIfNullOrWhiteSpace( path ); int index = this.paths.FindIndex( item => this.comparer.Equals( item, path ) ); if ( 0 <= index ) { this.paths.RemoveAt( index ); this.Changed?.Invoke(); return false; } this.paths.Add( path ); this.Changed?.Invoke(); return true; }
	public bool Remove( string path ) { int index = this.paths.FindIndex( item => this.comparer.Equals( item, path ) ); if ( 0 > index ) return false; this.paths.RemoveAt( index ); this.Changed?.Invoke(); return true; }
	public void Clear() { if ( 0 == this.paths.Count ) return; this.paths.Clear(); this.Changed?.Invoke(); }
}

