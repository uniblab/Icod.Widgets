/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

public enum WidgetDialogOutcome { Yes, No, Ok, Accepted, Cancel }

public sealed record WidgetDialogResult( WidgetDialogOutcome Outcome, object? Value = null );

