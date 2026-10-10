/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

public sealed record WidgetBrowserRequest( Guid BrowserId, long Generation, string Location ) {
	/// <summary>True for root/drive enumeration instead of a directory read.</summary>
	public bool IsRoots { get; init; }
}

