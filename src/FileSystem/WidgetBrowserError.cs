/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

public enum WidgetBrowserErrorKind { NotFound, AccessDenied, InvalidPath, TooManyEntries, IoError }

public sealed record WidgetBrowserError( WidgetBrowserErrorKind Kind, string Message );

