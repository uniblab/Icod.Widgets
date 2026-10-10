/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

/// <summary>Provides nonrecursive logical-path browsing through System.IO.</summary>
public sealed class LocalWidgetFileSystemProvider : IWidgetFileSystemProvider {
	public StringComparer PathComparer { get; } = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

	public ValueTask<IReadOnlyList<WidgetFileSystemEntry>> GetRootsAsync( CancellationToken token ) {
		token.ThrowIfCancellationRequested();
		WidgetFileSystemEntry[] roots = OperatingSystem.IsWindows()
			? DriveInfo.GetDrives().Where( drive => drive.IsReady ).Select( drive => new WidgetFileSystemEntry( Path.GetFullPath( drive.RootDirectory.FullName ), drive.Name, true ) ).ToArray()
			: [ new WidgetFileSystemEntry( Path.GetPathRoot( Environment.CurrentDirectory ) ?? "/", "/", true ) ];
		return ValueTask.FromResult<IReadOnlyList<WidgetFileSystemEntry>>( roots );
	}

	public ValueTask<WidgetDirectorySnapshot> ReadDirectoryAsync( string location, CancellationToken token ) {
		token.ThrowIfCancellationRequested(); string full = Path.GetFullPath( location ); DirectoryInfo directory = new( full );
		List<WidgetFileSystemEntry> entries = [];
		foreach ( FileSystemInfo info in directory.EnumerateFileSystemInfos() ) {
			token.ThrowIfCancellationRequested(); bool isDirectory = 0 != ( info.Attributes & FileAttributes.Directory ); bool isLink = info.LinkTarget is not null;
			entries.Add( new WidgetFileSystemEntry( Path.GetFullPath( info.FullName ), info.Name, isDirectory, isLink ) );
			if ( entries.Count > WidgetLimits.MaximumItems ) break;
		}
		WidgetFileSystemEntry[] ordered = entries.OrderByDescending( item => item.IsDirectory ).ThenBy( item => item.DisplayName, this.PathComparer ).ToArray();
		string? parent = Directory.GetParent( full )?.FullName;
		return ValueTask.FromResult( new WidgetDirectorySnapshot( full, parent, ordered ) );
	}

	public ValueTask<WidgetFileSelectionValidation> ValidateSelectionAsync( IReadOnlyList<string> paths, WidgetFileSelectionKind kind, CancellationToken token ) {
		ArgumentNullException.ThrowIfNull( paths ); if ( !Enum.IsDefined( kind ) ) throw new ArgumentOutOfRangeException( nameof( kind ) );
		List<string> failed = [];
		foreach ( string path in paths ) { token.ThrowIfCancellationRequested(); bool valid = WidgetFileSelectionKind.Directory == kind ? Directory.Exists( path ) : File.Exists( path ); if ( !valid ) failed.Add( path ); }
		return ValueTask.FromResult( 0 == failed.Count ? WidgetFileSelectionValidation.Valid : WidgetFileSelectionValidation.Invalid( failed ) );
	}
}

