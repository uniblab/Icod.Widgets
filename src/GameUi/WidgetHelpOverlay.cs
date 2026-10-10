/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
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
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) { base.OnRender( screen, theme, focused ); CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); for ( int row = 1; row < area.Rows && row <= this.bindings.Length; row++ ) { WidgetHelpBinding binding = this.bindings[ row - 1 ]; screen.StandardWindow.Move( area.Row + row, area.Column ); screen.StandardWindow.Write( CursesText.TruncateToColumns( $"{binding.Gesture}: {binding.Description}", area.Columns, screen.TextWidthProvider ), theme.Normal ); } }
}

