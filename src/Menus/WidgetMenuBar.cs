/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

/// <summary>A menu-bar presentation sharing popup menu semantics.</summary>
public sealed class WidgetMenuBar( string id, IReadOnlyList<WidgetMenuItem> items ) : WidgetPopupMenu( id, items ) {
	internal bool TryGlobalInput( WidgetInput input, WidgetHost host ) => this.TryRootMnemonic( input, host );
}
