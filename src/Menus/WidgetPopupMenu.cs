/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using Icod.DCurses;

/// <summary>Provides keyboard-operated bounded popup and submenu navigation.</summary>
public class WidgetPopupMenu : Widget {
	private sealed class Level( IReadOnlyList<WidgetMenuItem> items ) { public IReadOnlyList<WidgetMenuItem> Items { get; } = items; public int Cursor { get; set; } }
	private readonly WidgetMenuItem[] rootItems;
	private readonly List<Level> levels = [];

	public WidgetPopupMenu( string id, IReadOnlyList<WidgetMenuItem> items ) : base( id ) {
		ArgumentNullException.ThrowIfNull( items ); if ( 0 == items.Count ) throw new ArgumentException( "A menu requires at least one item.", nameof( items ) );
		this.rootItems = items.ToArray();
		if ( MaximumDepth( this.rootItems ) > WidgetLimits.MaximumSubmenuDepth ) throw new ArgumentException( $"A menu cannot exceed {WidgetLimits.MaximumSubmenuDepth} levels.", nameof( items ) );
		ValidateMnemonics( this.rootItems, nameof( items ) );
		HashSet<( WidgetKey Key, WidgetInputModifiers Modifiers, char? Character )> shortcuts = [];
		foreach ( WidgetMenuItem item in Flatten( this.rootItems ) ) if ( item.Shortcut is { } shortcut &&
			!shortcuts.Add( ( shortcut.Key, shortcut.Modifiers, shortcut.Character.HasValue ? char.ToUpperInvariant( shortcut.Character.Value ) : null ) ) )
			throw new ArgumentException( "Menu shortcuts must be unique.", nameof( items ) );
	}

	public override bool IsFocusable => true;
	public bool IsOpen { get; private set; }
	public int Depth => this.levels.Count;
	public WidgetMenuItem? CurrentItem => 0 == this.levels.Count ? null : this.levels[ ^1 ].Items[ this.levels[ ^1 ].Cursor ];
	public CursesRectangle RenderBounds { get; private set; }

	public void Open() { if ( this.IsOpen ) return; this.Host?.OpenPopup( this, this.Bounds ); this.levels.Clear(); this.levels.Add( new Level( this.rootItems ) ); this.IsOpen = true; }
	public void Close() { this.Host?.ClosePopup( this ); this.levels.Clear(); this.IsOpen = false; this.RenderBounds = default; }

	protected override void OnArrange( CursesRectangle bounds ) { if ( this.IsOpen ) this.Host?.UpdatePopup( this, bounds ); }

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( !this.IsOpen ) { if ( WidgetInputKind.Key == input.Kind && input.KeyCode is WidgetKey.Enter or WidgetKey.Space or WidgetKey.Down ) { this.Open(); return true; } return false; }
		if ( WidgetInputKind.Key == input.Kind && this.TryShortcut( input, host ) ) return true;
		if ( this.TryCurrentMnemonic( input, host ) ) return true;
		if ( WidgetInputKind.Key != input.Kind ) return false;
		if ( input.KeyCode is WidgetKey.Down or WidgetKey.Up ) return this.Move( WidgetKey.Down == input.KeyCode ? 1 : -1 );
		if ( WidgetKey.Right == input.KeyCode ) return this.OpenSubmenu();
		if ( WidgetKey.Left == input.KeyCode || WidgetKey.Escape == input.KeyCode ) {
			if ( 1 < this.levels.Count ) this.levels.RemoveAt( this.levels.Count - 1 ); else this.Close(); return true;
		}
		if ( input.KeyCode is WidgetKey.Enter or WidgetKey.Space ) return this.Activate( host );
		return false;
	}

	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		if ( !this.IsOpen || 0 == this.levels.Count ) { this.RenderBounds = default;
			CursesRectangle closedArea = this.Bounds.Intersect( screen.Bounds ); WidgetBrowserView.WriteLine( screen, closedArea, 0, string.Join( " | ", this.rootItems.Where( item => item.Kind != WidgetMenuItemKind.Separator ).Select( item => item.Text ) ), focused ? theme.Focused : theme.Normal ); return; }
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); bool border = area.Rows >= 3 && area.Columns >= 3; int inset = border ? 1 : 0;
		this.RenderBounds = new CursesRectangle( area.Row, area.Column, Math.Min( area.Rows, this.levels[ ^1 ].Items.Count + inset * 2 ), area.Columns );
		int contentRows = this.RenderBounds.Rows - inset * 2; int contentColumns = this.RenderBounds.Columns - inset * 2;
		if ( border ) this.RenderPopupBorder( screen, theme );
		for ( int row = 0; row < contentRows; row++ ) {
			int index = Math.Clamp( this.levels[ ^1 ].Cursor - contentRows + 1, 0, Math.Max( 0, this.levels[ ^1 ].Items.Count - contentRows ) ) + row;
			WidgetMenuItem item = this.levels[ ^1 ].Items[ index ];
			string prefix = WidgetMenuItemKind.CheckableCommand == item.Kind ? ( item.IsChecked ? "[x] " : "[ ] " ) : string.Empty;
			string suffix = WidgetMenuItemKind.Submenu == item.Kind ? " >" : string.Empty;
			string value = WidgetMenuItemKind.Separator == item.Kind ? new string( '─', contentColumns ) : ComposeRow( item, prefix + item.Text + suffix, contentColumns, screen );
			CursesStyle style = !item.IsEnabled ? theme.Disabled : index == this.levels[ ^1 ].Cursor && focused ? theme.Focused : theme.Normal;
			screen.StandardWindow.Move( this.RenderBounds.Row + row + inset, this.RenderBounds.Column + inset ); screen.StandardWindow.Write( value, style );
		}
	}

	private void RenderPopupBorder( CursesScreen screen, WidgetTheme theme ) {
		int right = this.RenderBounds.Column + this.RenderBounds.Columns - 1; int bottom = this.RenderBounds.BottomExclusive - 1;
		string horizontal = new( '─', this.RenderBounds.Columns - 2 );
		screen.StandardWindow.Move( this.RenderBounds.Row, this.RenderBounds.Column ); screen.StandardWindow.Write( "┌" + horizontal + "┐", theme.Border );
		for ( int row = this.RenderBounds.Row + 1; row < bottom; row++ ) { screen.StandardWindow.Move( row, this.RenderBounds.Column ); screen.StandardWindow.Write( "│", theme.Border ); screen.StandardWindow.Move( row, right ); screen.StandardWindow.Write( "│", theme.Border ); }
		screen.StandardWindow.Move( bottom, this.RenderBounds.Column ); screen.StandardWindow.Write( "└" + horizontal + "┘", theme.Border );
	}

	private bool Move( int direction ) {
		Level level = this.levels[ ^1 ];
		for ( int count = 1; count <= level.Items.Count; count++ ) {
			int index = ( level.Cursor + direction * count + level.Items.Count ) % level.Items.Count;
			WidgetMenuItem item = level.Items[ index ]; if ( item.IsEnabled && WidgetMenuItemKind.Separator != item.Kind ) { level.Cursor = index; return true; }
		}
		return false;
	}

	private bool OpenSubmenu() {
		WidgetMenuItem? item = this.CurrentItem; if ( item is null || !item.IsEnabled || WidgetMenuItemKind.Submenu != item.Kind || 0 == item.Children.Count ) return false;
		this.levels.Add( new Level( item.Children ) ); return true;
	}

	private bool Activate( WidgetHost host ) {
		WidgetMenuItem? item = this.CurrentItem; if ( item is null || !item.IsEnabled || WidgetMenuItemKind.Separator == item.Kind ) return false;
		if ( WidgetMenuItemKind.Submenu == item.Kind ) return this.OpenSubmenu();
		if ( WidgetMenuItemKind.CheckableCommand == item.Kind ) { host.Enqueue( new WidgetCheckedInvokedAction( this.Id, item.CommandId!, !item.IsChecked ) ); item.IsChecked = !item.IsChecked; }
		else host.Enqueue( new WidgetInvokedAction( this.Id, item.CommandId! ) );
		this.Close(); return true;
	}

	protected bool TryRootMnemonic( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key != input.Kind || WidgetKey.Character != input.KeyCode || input.Character is not { Value: <= char.MaxValue } character ||
			0 == ( input.Modifiers & WidgetInputModifiers.Alt ) || 0 != ( input.Modifiers & WidgetInputModifiers.Control ) ) return false;
		int index = Array.FindIndex( this.rootItems, item => item.IsEnabled && item.Mnemonic.HasValue && char.ToUpperInvariant( item.Mnemonic.Value ) == char.ToUpperInvariant( (char)character.Value ) );
		if ( 0 > index ) return false;
		if ( !this.IsOpen ) this.Open();
		this.levels[ 0 ].Cursor = index;
		while ( 1 < this.levels.Count ) this.levels.RemoveAt( this.levels.Count - 1 );
		return this.Activate( host );
	}

	private bool TryCurrentMnemonic( WidgetInput input, WidgetHost host ) {
		if ( WidgetKey.Character != input.KeyCode || input.Character is not { Value: <= char.MaxValue } character ||
			0 != ( input.Modifiers & ( WidgetInputModifiers.Control | WidgetInputModifiers.Alt ) ) ) return false;
		Level level = this.levels[ ^1 ];
		int index = level.Items.ToList().FindIndex( item => item.IsEnabled && item.Mnemonic.HasValue && char.ToUpperInvariant( item.Mnemonic.Value ) == char.ToUpperInvariant( (char)character.Value ) );
		if ( 0 > index ) return false;
		level.Cursor = index;
		return this.Activate( host );
	}

	protected bool TryShortcut( WidgetInput input, WidgetHost host ) {
		foreach ( WidgetMenuItem item in FlattenEnabled( this.rootItems ) ) {
			if ( item.Shortcut is not { } shortcut || shortcut.Key != input.KeyCode || shortcut.Modifiers != input.Modifiers || !ShortcutCharacterMatches( shortcut, input ) ) continue;
			if ( item.Kind is WidgetMenuItemKind.Separator or WidgetMenuItemKind.Submenu ) return false;
			if ( WidgetMenuItemKind.CheckableCommand == item.Kind ) { item.IsChecked = !item.IsChecked; host.Enqueue( new WidgetCheckedInvokedAction( this.Id, item.CommandId!, item.IsChecked ) ); }
			else host.Enqueue( new WidgetInvokedAction( this.Id, item.CommandId! ) );
			this.Close(); return true;
		}
		return false;
	}

	private static bool ShortcutCharacterMatches( WidgetMenuShortcut shortcut, WidgetInput input ) => !shortcut.Character.HasValue ||
		input.Character is { Value: <= char.MaxValue } character && char.ToUpperInvariant( shortcut.Character.Value ) == char.ToUpperInvariant( (char)character.Value );

	private static string ComposeRow( WidgetMenuItem item, string label, int columns, CursesScreen screen ) {
		if ( item.Shortcut is not { } shortcut ) return CursesText.TruncateToColumns( label, columns, screen.TextWidthProvider );
		string gesture = FormatShortcut( shortcut ); int gestureColumns = CursesText.MeasureColumns( gesture, screen.TextWidthProvider );
		if ( columns < gestureColumns + 2 ) return CursesText.TruncateToColumns( label, columns, screen.TextWidthProvider );
		string clippedLabel = CursesText.TruncateToColumns( label, columns - gestureColumns - 2, screen.TextWidthProvider );
		int labelColumns = CursesText.MeasureColumns( clippedLabel, screen.TextWidthProvider );
		return clippedLabel + new string( ' ', columns - labelColumns - gestureColumns ) + gesture;
	}

	private static string FormatShortcut( WidgetMenuShortcut shortcut ) {
		List<string> parts = [];
		if ( 0 != ( shortcut.Modifiers & WidgetInputModifiers.Control ) ) parts.Add( "Ctrl" );
		if ( 0 != ( shortcut.Modifiers & WidgetInputModifiers.Alt ) ) parts.Add( "Alt" );
		if ( 0 != ( shortcut.Modifiers & WidgetInputModifiers.Shift ) ) parts.Add( "Shift" );
		parts.Add( shortcut.Character.HasValue ? char.ToUpperInvariant( shortcut.Character.Value ).ToString() : shortcut.Key.ToString() );
		return string.Join( "+", parts );
	}

	private static IEnumerable<WidgetMenuItem> Flatten( IReadOnlyList<WidgetMenuItem> items ) { foreach ( WidgetMenuItem item in items ) { yield return item; foreach ( WidgetMenuItem child in Flatten( item.Children ) ) yield return child; } }
	private static IEnumerable<WidgetMenuItem> FlattenEnabled( IReadOnlyList<WidgetMenuItem> items ) { foreach ( WidgetMenuItem item in items ) { if ( !item.IsEnabled ) continue; yield return item; foreach ( WidgetMenuItem child in FlattenEnabled( item.Children ) ) yield return child; } }
	private static void ValidateMnemonics( IReadOnlyList<WidgetMenuItem> items, string parameterName ) {
		HashSet<char> mnemonics = [];
		foreach ( WidgetMenuItem item in items ) {
			if ( item.Mnemonic.HasValue && !mnemonics.Add( char.ToUpperInvariant( item.Mnemonic.Value ) ) ) throw new ArgumentException( "Menu mnemonics must be unique within a level.", parameterName );
			if ( 0 < item.Children.Count ) ValidateMnemonics( item.Children, parameterName );
		}
	}
	private static int MaximumDepth( IReadOnlyList<WidgetMenuItem> items ) { int depth = 1; foreach ( WidgetMenuItem item in items ) if ( 0 < item.Children.Count ) depth = Math.Max( depth, 1 + MaximumDepth( item.Children ) ); return depth; }
}

