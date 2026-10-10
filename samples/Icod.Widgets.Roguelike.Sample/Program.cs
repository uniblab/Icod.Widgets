/*
	Icod.Widgets.Roguelike.Sample
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Roguelike.Sample;
using Icod.DCurses;
using Icod.Widgets.Samples;

internal static class Program {
	private static async Task<int> Main( string[] args ) {
		if ( !args.Contains( "--smoke", StringComparer.Ordinal ) ) return await WidgetSampleApplication.RunAsync( screen => new RogueUi( screen ) );
		using RogueUi app = new( new CursesScreen( 60, 18 ) ); app.Arrange(); app.Input( WidgetInput.Key( WidgetKey.Enter ) );
		if ( app.Commands != 1 ) return 1;
		app.Help(); app.Input( WidgetInput.Key( WidgetKey.Tab ) ); app.Input( WidgetInput.Key( WidgetKey.Escape ) );
		if ( app.Host.ModalDepth != 0 || app.Host.FocusedWidget != app.Actions ) return 2;
		app.Draw(); Console.WriteLine( "PASS inventory/actions, messages/status, modal help restores focus, command delivered once" ); return 0;
	}
	private sealed class RogueUi : WidgetSampleApplication {
		internal WidgetActionList Actions { get; } = new( "actions" );
		private readonly WidgetInventoryList inventory = new( "inventory" );
		private readonly WidgetMessageHistory messages = new( "messages" );
		private readonly WidgetStatusBar status = new( "status" );
		private int nextId;
		internal int Commands { get; private set; }
		protected override string Heading => "Icod.Widgets roguelike UI example — text presentation";
		protected override string Keys => "i inventory | a actions | h help | Arrows navigate | Enter invokes action | Space toggles inventory";
		internal RogueUi( CursesScreen screen ) : base( screen ) {
			this.Actions.SetItems( [ new( "drink", "Drink potion" ), new( "drop", "Drop item" ), new( "inspect", "Inspect" ) ] );
			this.inventory.SetItems( [ new( "sword", "Sword 界" ), new( "potion", "Healing potion" ), new( "armour", "Armour" ) ] );
			this.messages.Append( "These controls use application data; no game rules are included." );
			this.status.SetFields( [ new( "hp", "HP 12/20", 10 ), new( "depth", "Depth 3", 5 ) ] );
			this.Host.Attach( this.Actions ); this.Host.Attach( this.inventory ); this.Host.Attach( this.messages ); this.Host.Attach( this.status );
		}
		internal override void Arrange() {
			CursesScreen screen = this.Host.Screen; int row = Math.Min( 2, screen.Rows );
			foreach ( Widget widget in new Widget[] { this.Actions, this.inventory, this.messages, this.status } ) { int count = Math.Min( Math.Max( 0, screen.Rows - 2 - row ), widget == this.status ? 1 : Math.Max( 1, ( screen.Rows - 5 ) / 3 ) ); widget.Arrange( new( row, 0, count, screen.Columns ) ); row += count; }
			if ( this.Dialog is { IsOpen: true } dialog ) dialog.Arrange( screen.Bounds );
			if ( this.Host.FocusedWidget is null ) this.Host.Focus( this.Actions );
		}
		internal void Help() => this.Show( new WidgetHelpOverlay( "help." + ++this.nextId, "Application bindings (Esc closes)", [ new( "inventory", "i", "Focus inventory" ), new( "actions", "a", "Focus actions" ), new( "help", "h", "Show help" ), new( "quit", "Q / Esc / Ctrl+Q", "Exit sample" ) ] ) );
		protected override bool OnShortcut( WidgetInput input ) {
			if ( input.Character?.Value is 'h' or 'H' ) { this.Help(); return true; }
			if ( input.Character?.Value is 'i' or 'I' ) { this.Host.Focus( this.inventory ); return true; }
			if ( input.Character?.Value is 'a' or 'A' ) { this.Host.Focus( this.Actions ); return true; }
			return false;
		}
		protected override void OnAction( WidgetAction action ) { if ( action is WidgetInvokedAction command ) { this.Commands++; this.messages.Append( "Application received command: " + command.CommandId ); this.Status = $"Commands received: {this.Commands}"; } else if ( action is WidgetSelectionChangedAction selection ) this.Status = "Inventory selection: " + string.Join( ", ", selection.Keys ); else base.OnAction( action ); }
	}
}
