/*
	Icod.Widgets
	Reusable terminal UI controls for .NET.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.Widgets;

/// <summary>Describes an immutable semantic action emitted by a widget.</summary>
public abstract record WidgetAction( string WidgetId );

public sealed record WidgetInvokedAction( string WidgetId, string CommandId ) : WidgetAction( WidgetId );

/// <summary>Requests activation of a caller-owned tab; the application accepts by replacing the tab snapshot.</summary>
public sealed record WidgetTabActivationRequestedAction( string WidgetId, string TabKey ) : WidgetAction( WidgetId );

/// <summary>Requests a close decision without removing the tab or its document.</summary>
public sealed record WidgetTabCloseRequestedAction( string WidgetId, string TabKey ) : WidgetAction( WidgetId );

public sealed record WidgetCheckedInvokedAction( string WidgetId, string CommandId, bool IsChecked ) : WidgetAction( WidgetId );

public sealed record WidgetValueChangedAction<T>( string WidgetId, T Value ) : WidgetAction( WidgetId );

public sealed record WidgetSelectionChangedAction( string WidgetId, IReadOnlyList<string> Keys ) : WidgetAction( WidgetId );

/// <summary>Reports one committed text-document transaction without copying document contents.</summary>
public sealed record WidgetTextEditedAction( string WidgetId, Guid DocumentId, long Revision, long StateId ) : WidgetAction( WidgetId );

/// <summary>Reports one directional editor-selection change.</summary>
public sealed record WidgetTextSelectionChangedAction( string WidgetId, Guid ViewId, long Anchor, long Caret ) : WidgetAction( WidgetId );

/// <summary>Reports a user-originated scrollbar offset change.</summary>
public sealed record WidgetScrollChangedAction( string WidgetId, long Offset ) : WidgetAction( WidgetId );

/// <summary>Requests that the application perform an explicit clipboard operation.</summary>
public sealed record WidgetClipboardRequestedAction( string WidgetId, WidgetClipboardRequest Request ) : WidgetAction( WidgetId );

public sealed record WidgetDialogCompletedAction( string WidgetId, WidgetDialogResult Result ) : WidgetAction( WidgetId );

/// <summary>Requests application-owned validation before a form dialog can complete.</summary>
public sealed record WidgetDialogValidationRequestedAction( string WidgetId, WidgetDialogValidationRequest Request ) : WidgetAction( WidgetId );

public sealed record WidgetWizardNavigationRequestedAction( string WidgetId, int FromPage, int ToPage ) : WidgetAction( WidgetId );


/// <summary>Requests provider I/O to load a picker directory; apply the result on the UI loop.</summary>
public sealed record WidgetBrowserLoadRequestedAction( string WidgetId, WidgetBrowserRequest Request ) : WidgetAction( WidgetId );
/// <summary>Requests provider validation before a picker can complete.</summary>
public sealed record WidgetPickerValidationRequestedAction( string WidgetId, WidgetPickerValidationRequest Request ) : WidgetAction( WidgetId );

/// <summary>Requests application-owned inspection of a proposed save target without writing it.</summary>
public sealed record WidgetSaveTargetInspectionRequestedAction( string WidgetId, WidgetSaveTargetRequest Request ) : WidgetAction( WidgetId );

/// <summary>Requests application-owned search or replacement against a chosen document revision.</summary>
public sealed record WidgetSearchRequestedAction( string WidgetId, WidgetSearchRequest Request ) : WidgetAction( WidgetId );
