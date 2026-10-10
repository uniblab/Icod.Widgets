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

using Icod.DCurses;

public sealed record WidgetHelpBinding( string CommandId, string Gesture, string Description );

/// <summary>Displays caller-resolved effective command bindings.</summary>
public sealed class WidgetHelpOverlay : WidgetDialog {
	private readonly WidgetHelpBinding[] bindings;
	public WidgetHelpOverlay( string id, string title, IReadOnlyList<WidgetHelpBinding> bindings ) : base( id, title ) { ArgumentNullException.ThrowIfNull( bindings ); this.bindings = bindings.ToArray(); }
	public IReadOnlyList<WidgetHelpBinding> Bindings => this.bindings;
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) { if ( WidgetInputKind.Key == input.Kind && WidgetKey.Escape == input.KeyCode ) { this.Finish( host, WidgetDialogOutcome.Cancel ); return true; } return false; }
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) { CursesRectangle area = this.RenderFrame( screen, theme ); for ( int row = 0; row < area.Rows && row < this.bindings.Length; row++ ) { WidgetHelpBinding binding = this.bindings[ row ]; WidgetBrowserView.WriteLine( screen, area, row, $"{binding.Gesture}: {binding.Description}", theme.Normal ); } }
}

