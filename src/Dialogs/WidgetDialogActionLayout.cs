/*
	Icod.Widgets
	Measures the visible dialog action presentation used by rendering and pointer input.
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

using Icod.DCurses;

internal readonly record struct WidgetDialogActionPresentation(
	int Index,
	WidgetDialogAction Action,
	string Label,
	string RenderedLabel,
	int Column,
	int RenderedWidth
);

internal static class WidgetDialogActionLayout {
	internal static string Label( WidgetDialogAction action ) => $"[{WidgetBrowserView.Sanitize( action.Label )}]";

	internal static IReadOnlyList<WidgetDialogActionPresentation> Measure(
		IReadOnlyList<WidgetDialogAction> actions,
		int firstColumn,
		int rightExclusive,
		int gap,
		ICursesTextWidthProvider widthProvider
	) {
		List<WidgetDialogActionPresentation> result = [];
		int column = firstColumn;
		for ( int index = 0; index < actions.Count && column < rightExclusive; index++ ) {
			WidgetDialogAction action = actions[ index ]; string label = Label( action );
			string rendered = CursesText.TruncateToColumns( label, rightExclusive - column, widthProvider );
			int renderedWidth = CursesText.MeasureColumns( rendered, widthProvider );
			result.Add( new( index, action, label, rendered, column, renderedWidth ) );
			column += CursesText.MeasureColumns( label, widthProvider ) + gap;
		}
		return result;
	}
}
