/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/

namespace Icod.Widgets;

/// <summary>Describes an immutable semantic action emitted by a widget.</summary>
public abstract record WidgetAction( string WidgetId );

public sealed record WidgetInvokedAction( string WidgetId, string CommandId ) : WidgetAction( WidgetId );

public sealed record WidgetCheckedInvokedAction( string WidgetId, string CommandId, bool IsChecked ) : WidgetAction( WidgetId );

public sealed record WidgetValueChangedAction<T>( string WidgetId, T Value ) : WidgetAction( WidgetId );

public sealed record WidgetSelectionChangedAction( string WidgetId, IReadOnlyList<string> Keys ) : WidgetAction( WidgetId );

public sealed record WidgetDialogCompletedAction( string WidgetId, WidgetDialogResult Result ) : WidgetAction( WidgetId );

public sealed record WidgetWizardNavigationRequestedAction( string WidgetId, int FromPage, int ToPage ) : WidgetAction( WidgetId );

