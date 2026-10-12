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

public enum WidgetSearchDirection { Forward, Backward }
public enum WidgetSearchIntent { Find, ReplaceOne, ReplaceAll }
public sealed record WidgetSearchOptions( bool MatchCase = false, bool WholeWord = false, bool UseRegularExpression = false, WidgetSearchDirection Direction = WidgetSearchDirection.Forward );
public sealed record WidgetSearchRequest( Guid RequestId, Guid FormId, long Generation, Guid DocumentId, Guid ViewId, long Revision,
	WidgetSearchIntent Intent, string Query, string? Replacement, WidgetSearchOptions Options );
public sealed record WidgetSearchResult( WidgetSearchRequest Request, bool Found, long? MatchStart = null, long MatchLength = 0, string? Error = null );
