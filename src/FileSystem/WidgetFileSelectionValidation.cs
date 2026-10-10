/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

public enum WidgetFileSelectionKind { Directory, File }

public sealed record WidgetFileSelectionValidation( bool IsValid, IReadOnlyList<string> FailedPaths ) {
	public static WidgetFileSelectionValidation Valid { get; } = new( true, Array.Empty<string>() );
	public static WidgetFileSelectionValidation Invalid( IReadOnlyList<string> failedPaths ) => new( false, failedPaths.ToArray() );
}

