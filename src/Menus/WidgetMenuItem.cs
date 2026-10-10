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

public enum WidgetMenuItemKind { Command, CheckableCommand, Separator, Submenu }

public readonly record struct WidgetMenuShortcut( WidgetKey Key, WidgetInputModifiers Modifiers = WidgetInputModifiers.None, char? Character = null );

/// <summary>Represents a command, checkable command, separator, or submenu.</summary>
public sealed class WidgetMenuItem {
	private WidgetMenuItem( string id, string text, WidgetMenuItemKind kind, string? commandId, bool isEnabled, bool isChecked, WidgetMenuShortcut? shortcut, char? mnemonic, IReadOnlyList<WidgetMenuItem>? children ) {
		if ( string.IsNullOrWhiteSpace( id ) ) throw new ArgumentException( "An item id is required.", nameof( id ) );
		this.Id = id; this.Text = text ?? throw new ArgumentNullException( nameof( text ) ); this.Kind = kind; this.CommandId = commandId; this.IsEnabled = isEnabled; this.IsChecked = isChecked; this.Shortcut = shortcut; this.Mnemonic = mnemonic; this.Children = children?.ToArray() ?? [];
	}
	public string Id { get; }
	public string Text { get; }
	public WidgetMenuItemKind Kind { get; }
	public string? CommandId { get; }
	public bool IsEnabled { get; }
	public bool IsChecked { get; internal set; }
	public WidgetMenuShortcut? Shortcut { get; }
	public char? Mnemonic { get; }
	public IReadOnlyList<WidgetMenuItem> Children { get; }

	public static WidgetMenuItem Command( string id, string text, string commandId, bool isEnabled = true, WidgetMenuShortcut? shortcut = null ) => new( id, text, WidgetMenuItemKind.Command, commandId, isEnabled, false, shortcut, null, null );
	public static WidgetMenuItem Command( string id, string text, string commandId, char mnemonic, bool isEnabled = true, WidgetMenuShortcut? shortcut = null ) => new( id, text, WidgetMenuItemKind.Command, commandId, isEnabled, false, shortcut, mnemonic, null );
	public static WidgetMenuItem Checkable( string id, string text, string commandId, bool isChecked = false, bool isEnabled = true, WidgetMenuShortcut? shortcut = null ) => new( id, text, WidgetMenuItemKind.CheckableCommand, commandId, isEnabled, isChecked, shortcut, null, null );
	public static WidgetMenuItem Checkable( string id, string text, string commandId, char mnemonic, bool isChecked = false, bool isEnabled = true, WidgetMenuShortcut? shortcut = null ) => new( id, text, WidgetMenuItemKind.CheckableCommand, commandId, isEnabled, isChecked, shortcut, mnemonic, null );
	public static WidgetMenuItem Separator( string id ) => new( id, string.Empty, WidgetMenuItemKind.Separator, null, false, false, null, null, null );
	public static WidgetMenuItem Submenu( string id, string text, IReadOnlyList<WidgetMenuItem> children, bool isEnabled = true ) => new( id, text, WidgetMenuItemKind.Submenu, null, isEnabled, false, null, null, children ?? throw new ArgumentNullException( nameof( children ) ) );
	public static WidgetMenuItem Submenu( string id, string text, IReadOnlyList<WidgetMenuItem> children, char mnemonic, bool isEnabled = true ) => new( id, text, WidgetMenuItemKind.Submenu, null, isEnabled, false, null, mnemonic, children ?? throw new ArgumentNullException( nameof( children ) ) );
}

