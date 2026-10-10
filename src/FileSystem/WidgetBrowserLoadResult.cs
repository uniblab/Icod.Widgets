/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

public sealed record WidgetBrowserLoadResult( WidgetBrowserRequest Request, WidgetDirectorySnapshot? Snapshot, WidgetBrowserError? Error ) {
	public static WidgetBrowserLoadResult Success( WidgetBrowserRequest request, WidgetDirectorySnapshot snapshot ) => new( request, snapshot, null );
	public static WidgetBrowserLoadResult Failure( WidgetBrowserRequest request, WidgetBrowserError error ) => new( request, null, error );
}

