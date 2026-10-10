/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
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

