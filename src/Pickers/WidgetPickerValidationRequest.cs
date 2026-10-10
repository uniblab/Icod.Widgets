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

