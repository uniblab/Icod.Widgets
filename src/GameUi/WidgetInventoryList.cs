/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

/// <summary>A domain-neutral multiple-selection inventory presentation.</summary>
public sealed class WidgetInventoryList( string id ) : WidgetList( id, WidgetSelectionMode.Multiple );

