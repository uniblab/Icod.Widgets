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

/// <summary>Identifies an application edit command offered to the focused widget.</summary>
public enum WidgetEditCommand { Copy, Cut, Paste, SelectAll, Undo, Redo }

/// <summary>Reports whether a focused edit command applied.</summary>
public enum WidgetCommandResult { NotApplicable, Disabled, Executed }

/// <summary>Allows a focused widget to participate in application edit commands.</summary>
public interface IWidgetEditTarget {
	bool CanExecuteEditCommand( WidgetEditCommand command );
	WidgetCommandResult ExecuteEditCommand( WidgetEditCommand command, TimeSpan timestamp );
}
