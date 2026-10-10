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
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) { CursesRectangle area = this.RenderFrame( screen, theme ); for ( int row = 0; row < area.Rows && row < this.bindings.Length; row++ ) { WidgetHelpBinding binding = this.bindings[ row ]; WidgetBrowserView.WriteLine( screen, area, row, $"{binding.Gesture}: {binding.Description}", theme.Normal ); } }
}

