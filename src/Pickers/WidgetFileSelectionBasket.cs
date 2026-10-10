/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
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

