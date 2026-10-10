/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

public sealed record WidgetPickerValidationRequest( Guid PickerId, long Generation, IReadOnlyList<string> Paths, WidgetFileSelectionKind Kind );

public sealed record WidgetPickerValidationResult( WidgetPickerValidationRequest Request, WidgetFileSelectionValidation Validation ) {
	public static WidgetPickerValidationResult Success( WidgetPickerValidationRequest request ) => new( request, WidgetFileSelectionValidation.Valid );
	public static WidgetPickerValidationResult Failure( WidgetPickerValidationRequest request, IReadOnlyList<string> failedPaths ) => new( request, WidgetFileSelectionValidation.Invalid( failedPaths ) );
}

public static class WidgetPickerValidationLoader {
	public static async ValueTask<WidgetPickerValidationResult> ValidateAsync( IWidgetFileSystemProvider provider, WidgetPickerValidationRequest request, CancellationToken token ) {
		ArgumentNullException.ThrowIfNull( provider ); ArgumentNullException.ThrowIfNull( request );
		WidgetFileSelectionValidation validation = await provider.ValidateSelectionAsync( request.Paths, request.Kind, token ).ConfigureAwait( false );
		return new WidgetPickerValidationResult( request, validation );
	}
}

