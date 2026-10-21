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

/// <summary>Provides keyboard-operated bounded popup and submenu navigation.</summary>
public partial class WidgetPopupMenu : Widget {
	private sealed class Level( IReadOnlyList<WidgetMenuItem> items ) { public IReadOnlyList<WidgetMenuItem> Items { get; } = items; public int Cursor { get; set; } }
	private readonly WidgetMenuItem[] rootDefinitions;
	private WidgetMenuItem[] rootItems;
	private readonly List<Level> levels = [];
	private int pressedIndex = -1;
	private int pressedLevel = -1;
	private int hoveredLevel = -1;
	private int pressedHeading = -1;
	private int hoveredIndex = -1;
	private int hoveredHeading = -1;
	private bool hasScreenAnchor;
	private int screenAnchorRow;
	private int screenAnchorColumn;
	private bool IsMenuBar => this is WidgetMenuBar;

	public WidgetPopupMenu( string id, IReadOnlyList<WidgetMenuItem> items ) : base( id ) {
		ArgumentNullException.ThrowIfNull( items ); if ( 0 == items.Count ) throw new ArgumentException( "A menu requires at least one item.", nameof( items ) );
		this.rootDefinitions = items.ToArray(); this.rootItems = this.rootDefinitions;
		if ( MaximumDepth( this.rootItems ) > WidgetLimits.MaximumSubmenuDepth ) throw new ArgumentException( $"A menu cannot exceed {WidgetLimits.MaximumSubmenuDepth} levels.", nameof( items ) );
		HashSet<string> itemIds = new( StringComparer.Ordinal ); int itemCount = 0;
		foreach ( WidgetMenuItem item in Flatten( this.rootItems ) ) {
			if ( item is null || !itemIds.Add( item.Id ) ) throw new ArgumentException( "Menu item IDs must be unique across the menu tree.", nameof( items ) );
			if ( ++itemCount > WidgetLimits.MaximumItems ) throw new ArgumentOutOfRangeException( nameof( items ) );
		}
		ValidateMnemonics( this.rootItems, nameof( items ) );
		HashSet<( WidgetKey Key, WidgetInputModifiers Modifiers, char? Character, int? FunctionKeyNumber )> shortcuts = [];
		foreach ( WidgetMenuItem item in Flatten( this.rootItems ) ) if ( item.Shortcut is { } shortcut ) {
			if ( shortcut.FunctionKeyNumber is < 0 or > 63 || shortcut.FunctionKeyNumber.HasValue && WidgetKey.Function != shortcut.Key )
				throw new ArgumentException( "A numbered menu shortcut must identify a function key from 0 through 63.", nameof( items ) );
			if ( !shortcuts.Add( ( shortcut.Key, shortcut.Modifiers, shortcut.Character.HasValue ? char.ToUpperInvariant( shortcut.Character.Value ) : null, shortcut.FunctionKeyNumber ) ) )
				throw new ArgumentException( "Menu shortcuts must be unique.", nameof( items ) );
		}
	}

	public override bool IsFocusable => true;
	public bool IsOpen { get; private set; }
	public int Depth => this.levels.Count;
	public WidgetMenuItem? CurrentItem => 0 == this.levels.Count ? null : this.levels[ ^1 ].Items[ this.levels[ ^1 ].Cursor ];
	public CursesRectangle RenderBounds { get; private set; }
	internal IReadOnlyList<WidgetMenuItem> RootItems => this.rootItems;
	internal Action<WidgetMenuItem>? CommandSink { get; set; }

	/// <summary>Applies a validated presentation snapshot without changing the immutable menu definitions.</summary>
	public void SetPresentation( IReadOnlyList<WidgetMenuItemPresentation> presentation ) {
		ArgumentNullException.ThrowIfNull( presentation );
		if ( presentation.Count > WidgetLimits.MaximumItems ) throw new ArgumentOutOfRangeException( nameof( presentation ) );
		HashSet<string> known = new( Flatten( this.rootDefinitions ).Select( item => item.Id ), StringComparer.Ordinal );
		Dictionary<string, WidgetMenuItemPresentation> supplied = new( StringComparer.Ordinal );
		foreach ( WidgetMenuItemPresentation item in presentation ) {
			if ( item is null || !known.Contains( item.ItemId ) || item.Text is null || !supplied.TryAdd( item.ItemId, item ) )
				throw new ArgumentException( "Presentation entries require distinct known IDs and nonnull text.", nameof( presentation ) );
		}
		WidgetMenuItem[] updated = Clone( this.rootDefinitions, supplied );
		string?[] selectedIds = this.levels.Select( level => level.Items[ level.Cursor ].Id ).ToArray();
		this.rootItems = updated;
		if ( !this.IsOpen ) return;
		this.levels.Clear(); IReadOnlyList<WidgetMenuItem> current = updated;
		for ( int depth = 0; depth < selectedIds.Length; depth++ ) {
			int selected = current.ToList().FindIndex( item => StringComparer.Ordinal.Equals( item.Id, selectedIds[ depth ] ) );
			if ( selected < 0 ) selected = 0;
			int cursor = FindEligible( current, selected );
			if ( cursor < 0 ) { this.Close(); return; }
			Level level = new( current ) { Cursor = cursor }; this.levels.Add( level );
			if ( depth + 1 == selectedIds.Length ) break;
			WidgetMenuItem parent = current[ cursor ];
			if ( parent.Kind != WidgetMenuItemKind.Submenu || parent.Children.Count == 0 ) break;
			current = parent.Children;
		}
		this.ResetPointer(); this.UpdatePopupBounds();
	}

	private static WidgetMenuItem[] Clone( IReadOnlyList<WidgetMenuItem> items, Dictionary<string, WidgetMenuItemPresentation> supplied ) =>
		items.Select( item => item.WithPresentation( supplied.GetValueOrDefault( item.Id ), Clone( item.Children, supplied ) ) ).ToArray();

	private static int FindEligible( IReadOnlyList<WidgetMenuItem> items, int from ) {
		for ( int i = from; i < items.Count; i++ ) if ( items[ i ].IsEnabled && items[ i ].Kind != WidgetMenuItemKind.Separator ) return i;
		for ( int i = from - 1; i >= 0; i-- ) if ( items[ i ].IsEnabled && items[ i ].Kind != WidgetMenuItemKind.Separator ) return i;
		return -1;
	}

	public void Open() {
		if ( this.IsOpen ) return;
		this.hasScreenAnchor = false;
		this.OpenCore();
	}
	/// <summary>Opens a content-sized root popup at a screen-cell anchor, moving it left or up as needed to remain visible.</summary>
	public void OpenAt( int row, int column ) {
		if ( this.Host is null ) throw new InvalidOperationException( "An anchored popup menu must be attached to a widget host." );
		this.hasScreenAnchor = true; this.screenAnchorRow = row; this.screenAnchorColumn = column;
		if ( this.IsOpen ) { this.ResetPointer(); this.UpdatePopupBounds(); return; }
		this.OpenCore();
	}
	private void OpenCore() {
		this.levels.Clear(); this.levels.Add( new Level( this.rootItems ) ); this.IsOpen = true;
		if ( this.IsMenuBar ) {
			this.levels[ 0 ].Cursor = Math.Max( 0, FindEligible( this.rootItems, 0 ) );
			this.OpenSubmenu();
		}
		try { this.Host?.OpenPopup( this, this.GetPopupBounds( this.Host.Screen ) ); }
		catch { this.levels.Clear(); this.IsOpen = false; throw; }
	}
	public void Close() { this.firstHeading = 0; this.ResetPointer(); this.Host?.ClosePopup( this ); this.levels.Clear(); this.IsOpen = false; this.RenderBounds = default; this.hasScreenAnchor = false; }
	private void ResetPointer() { this.pressedIndex = -1; this.pressedLevel = -1; this.pressedHeading = -1; this.hoveredIndex = -1; this.hoveredLevel = -1; this.hoveredHeading = -1; }
	protected override void OnCaptureCanceled() { this.pressedIndex = -1; this.pressedLevel = -1; this.pressedHeading = -1; }
	protected override void OnArrange( CursesRectangle bounds ) { this.ResetPointer(); this.UpdatePopupBounds(); }
	private void UpdatePopupBounds() { if ( this.IsOpen && this.Host is { } host ) host.UpdatePopup( this, this.GetPopupBounds( host.Screen ) ); }

	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is { } pointer ) return this.OnPointer( pointer, host );
		if ( !this.IsOpen ) {
			if ( WidgetInputKind.Key == input.Kind && input.KeyCode is WidgetKey.Enter or WidgetKey.Space or WidgetKey.Down ) { this.Open(); return true; }
			return false;
		}
		// Root mnemonics remain available while a dropdown is already open.
		if ( this.IsMenuBar && this.TryRootMnemonic( input, host ) || WidgetInputKind.Key == input.Kind && this.TryShortcut( input, host ) || this.TryCurrentMnemonic( input, host ) ) return true;
		if ( WidgetInputKind.Key != input.Kind ) return false;
		this.ResetPointer();
		bool handled;
		if ( input.KeyCode is WidgetKey.Down or WidgetKey.Up ) handled = this.Move( WidgetKey.Down == input.KeyCode ? 1 : -1 );
		else if ( WidgetKey.Right == input.KeyCode ) handled = this.OpenSubmenu() || this.IsMenuBar && this.SwitchRoot( 1 );
		else if ( WidgetKey.Left == input.KeyCode || WidgetKey.Escape == input.KeyCode ) {
			if ( this.levels.Count > ( this.IsMenuBar ? 2 : 1 ) ) this.levels.RemoveAt( this.levels.Count - 1 );
			else if ( this.IsMenuBar && WidgetKey.Left == input.KeyCode ) this.SwitchRoot( -1 );
			else this.Close();
			handled = true;
		} else handled = input.KeyCode is WidgetKey.Enter or WidgetKey.Space && this.Activate( host );
		this.UpdatePopupBounds(); return handled;
	}

	private bool SwitchRoot( int direction ) {
		Level root = this.levels[ 0 ];
		for ( int count = 1; count <= root.Items.Count; count++ ) {
			int index = ( root.Cursor + direction * count + root.Items.Count ) % root.Items.Count;
			if ( !root.Items[ index ].IsEnabled || root.Items[ index ].Kind == WidgetMenuItemKind.Separator ) continue;
			this.SelectRoot( index ); return true;
		}
		return false;
	}

	private void SelectRoot( int index ) {
		this.levels[ 0 ].Cursor = index;
		while ( this.levels.Count > 1 ) this.levels.RemoveAt( this.levels.Count - 1 );
		this.ResetPointer(); this.OpenSubmenu(); this.UpdatePopupBounds();
	}

	internal void UpdateHover( CursesMouseEvent pointer, CursesScreen screen ) {
		this.hoveredHeading = this.IsMenuBar ? this.HitHeading( pointer.Row, pointer.Column, screen ) : -1;
		( this.hoveredLevel, this.hoveredIndex ) = this.IsOpen ? this.HitItem( pointer.Row, pointer.Column, screen ) : ( -1, -1 );
	}

	private bool OnPointer( CursesMouseEvent pointer, WidgetHost host ) {
		this.UpdateHover( pointer, host.Screen );
		if ( pointer.Action == CursesMouseAction.Move ) return this.IsOpen || this.hoveredHeading >= 0;
		if ( CursesMouseButton.Primary != pointer.Button ) return false;
		int heading = this.hoveredHeading;
		if ( this.IsMenuBar && heading >= 0 ) {
			WidgetMenuItem item = this.rootItems[ heading ];
			if ( !item.IsEnabled || item.Kind == WidgetMenuItemKind.Separator ) return true;
			if ( pointer.Action == CursesMouseAction.Press ) {
				if ( this.IsOpen && this.levels[ 0 ].Cursor == heading ) { this.Close(); return true; }
				if ( !this.IsOpen ) this.Open();
				this.SelectRoot( heading ); this.pressedHeading = heading; return true;
			}
			if ( pointer.Action == CursesMouseAction.Release ) {
				bool matching = this.pressedHeading == heading; this.pressedHeading = -1; this.pressedIndex = -1;
				if ( matching && item.Kind != WidgetMenuItemKind.Submenu ) return this.Activate( host );
				return true;
			}
		}
		if ( !this.IsOpen ) {
			if ( pointer.Action == CursesMouseAction.Press && this.Bounds.Contains( pointer.Row, pointer.Column ) && !this.IsMenuBar ) { this.Open(); return true; }
			return false;
		}
		( int depth, int index ) = this.HitItem( pointer.Row, pointer.Column, host.Screen );
		if ( pointer.Action == CursesMouseAction.Press ) { this.pressedIndex = index; this.pressedLevel = depth; this.pressedHeading = -1; return true; }
		if ( pointer.Action == CursesMouseAction.Release ) {
			bool matching = index >= 0 && index == this.pressedIndex && depth == this.pressedLevel; this.pressedIndex = -1; this.pressedLevel = -1; this.pressedHeading = -1;
			if ( matching ) {
				WidgetMenuItem item = this.levels[ depth ].Items[ index ];
				if ( !item.IsEnabled || item.Kind == WidgetMenuItemKind.Separator ) return true;
				while ( this.levels.Count > depth + 1 ) this.levels.RemoveAt( this.levels.Count - 1 );
				this.levels[ depth ].Cursor = index; bool activated = this.Activate( host ); this.UpdatePopupBounds(); return activated;
			}
		}
		return false;
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
		this.ResetPointer(); this.levels.Add( new Level( item.Children ) { Cursor = Math.Max( 0, FindEligible( item.Children, 0 ) ) } ); this.UpdatePopupBounds(); return true;
	}

	private bool Activate( WidgetHost host ) {
		WidgetMenuItem? item = this.CurrentItem; if ( item is null || !item.IsEnabled || WidgetMenuItemKind.Separator == item.Kind ) return false;
		if ( WidgetMenuItemKind.Submenu == item.Kind ) return this.OpenSubmenu();
		bool? checkedValue = WidgetMenuItemKind.CheckableCommand == item.Kind ? !item.IsChecked : null;
		if ( checkedValue.HasValue && !item.IsPresentationControlled ) item.IsChecked = checkedValue.Value;
		this.Close(); this.DispatchCommand( host, item, checkedValue ); return true;
	}

	protected bool TryRootMnemonic( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key != input.Kind || WidgetKey.Character != input.KeyCode || input.Character is not { Value: <= char.MaxValue } character ||
			0 == ( input.Modifiers & WidgetInputModifiers.Alt ) || 0 != ( input.Modifiers & WidgetInputModifiers.Control ) ) return false;
		int index = Array.FindIndex( this.rootItems, item => item.IsEnabled && item.Mnemonic.HasValue && char.ToUpperInvariant( item.Mnemonic.Value ) == char.ToUpperInvariant( (char)character.Value ) );
		if ( 0 > index ) return false;
		if ( !this.IsOpen ) this.Open();
		this.levels[ 0 ].Cursor = index;
		while ( 1 < this.levels.Count ) this.levels.RemoveAt( this.levels.Count - 1 );
		this.ResetPointer(); bool activated = this.Activate( host ); this.UpdatePopupBounds(); return activated;
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
			if ( item.Shortcut is not { } shortcut || shortcut.Key != input.KeyCode || shortcut.Modifiers != input.Modifiers ||
				!ShortcutCharacterMatches( shortcut, input ) || !ShortcutFunctionKeyMatches( shortcut, input ) ) continue;
			if ( item.Kind is WidgetMenuItemKind.Separator or WidgetMenuItemKind.Submenu ) return false;
			bool? checkedValue = WidgetMenuItemKind.CheckableCommand == item.Kind ? !item.IsChecked : null;
			if ( checkedValue.HasValue && !item.IsPresentationControlled ) item.IsChecked = checkedValue.Value;
			this.Close(); this.DispatchCommand( host, item, checkedValue ); return true;
		}
		return false;
	}

	private void DispatchCommand( WidgetHost host, WidgetMenuItem item, bool? checkedValue ) {
		if ( this.CommandSink is { } sink ) sink( item );
		else host.DispatchFocusedEditOrApplication( item, this.Id, checkedValue );
	}

	private static bool ShortcutCharacterMatches( WidgetMenuShortcut shortcut, WidgetInput input ) => !shortcut.Character.HasValue ||
		input.Character is { Value: <= char.MaxValue } character && char.ToUpperInvariant( shortcut.Character.Value ) == char.ToUpperInvariant( (char)character.Value );
	private static bool ShortcutFunctionKeyMatches( WidgetMenuShortcut shortcut, WidgetInput input ) =>
		!shortcut.FunctionKeyNumber.HasValue || shortcut.FunctionKeyNumber == input.FunctionKeyNumber;

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
		parts.Add( shortcut.Character.HasValue ? char.ToUpperInvariant( shortcut.Character.Value ).ToString() :
			shortcut.FunctionKeyNumber.HasValue ? $"F{shortcut.FunctionKeyNumber.Value}" : shortcut.Key.ToString() );
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

