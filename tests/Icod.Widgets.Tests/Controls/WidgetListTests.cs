/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Tests.Controls;

using System.Text;
using Icod.DCurses;
using Xunit;

public sealed class WidgetListTests {
	[Fact]
	public void MoveCursorPreservesMultipleSelection() {
		WidgetList list = CreateList( WidgetSelectionMode.Multiple ); using WidgetHost host = Focus( list );
		host.Process( WidgetInput.Key( WidgetKey.Space ), TimeSpan.Zero );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Equal( [ "a" ], list.SelectedKeys ); Assert.Equal( "b", list.CursorKey );
	}

	[Fact]
	public void DisabledItemsCannotSelect() {
		WidgetList list = new( "list", WidgetSelectionMode.Multiple );
		list.SetItems( [ new( "a", "A" ), new( "b", "B", false ) ] ); using WidgetHost host = Focus( list );
		host.Process( WidgetInput.Key( WidgetKey.Down ), TimeSpan.Zero );
		host.Process( WidgetInput.Key( WidgetKey.Space ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.DoesNotContain( "b", list.SelectedKeys );
	}

	[Fact]
	public void RangeUsesAnchor() {
		WidgetList list = CreateList( WidgetSelectionMode.Multiple ); using WidgetHost host = Focus( list );
		host.Process( WidgetInput.Key( WidgetKey.Space ), TimeSpan.Zero );
		host.Process( WidgetInput.Key( WidgetKey.Down, WidgetInputModifiers.Shift ), TimeSpan.FromMilliseconds( 1 ) );
		Assert.Equal( [ "a", "b" ], list.SelectedKeys );
	}

	[Fact]
	public void ReplacementPreservesEnabledKeys() {
		WidgetList list = CreateList( WidgetSelectionMode.Multiple ); using WidgetHost host = Focus( list );
		host.Process( WidgetInput.Key( WidgetKey.Space ), TimeSpan.Zero );
		list.SetItems( [ new( "c", "C" ), new( "a", "A" ), new( "b", "B", false ) ] );
		Assert.Equal( [ "a" ], list.SelectedKeys ); Assert.Equal( "a", list.CursorKey );
	}

	[Fact]
	public void DuplicateKeysRejectBeforeMutation() {
		WidgetList list = CreateList( WidgetSelectionMode.Single );
		Assert.Throws<ArgumentException>( () => list.SetItems( [ new( "x", "X" ), new( "x", "Again" ) ] ) );
		Assert.Equal( 3, list.Items.Count );
	}

	[Fact]
	public void SearchExpiresAt750Milliseconds() {
		WidgetList list = new( "list", WidgetSelectionMode.Single );
		list.SetItems( [ new( "a", "Alpha" ), new( "b", "Beta" ), new( "g", "Gamma" ) ] ); using WidgetHost host = Focus( list );
		host.Process( WidgetInput.TextInput( new Rune( 'b' ) ), TimeSpan.Zero );
		host.Process( WidgetInput.TextInput( new Rune( 'g' ) ), TimeSpan.FromMilliseconds( 751 ) );
		Assert.Equal( "g", list.CursorKey );
	}

	[Fact]
	public void ControlASelectsEveryEnabledItem() {
		WidgetList list = new( "list", WidgetSelectionMode.Multiple );
		list.SetItems( [ new( "a", "Alpha" ), new( "b", "Beta", false ), new( "c", "Charlie" ) ] );
		using WidgetHost host = Focus( list );
		WidgetDispatchResult result = host.Process(
			WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Control, new Rune( 'a' ) ),
			TimeSpan.Zero );
		Assert.True( result.Handled );
		Assert.Equal( [ "a", "c" ], list.SelectedKeys );
	}

	[Fact]
	public void ReplacementMovesMissingCursorToNearestEnabledItem() {
		WidgetList list = new( "list", WidgetSelectionMode.Single );
		list.SetItems( [ new( "a", "A" ), new( "b", "B" ), new( "c", "C" ) ] );
		using WidgetHost host = Focus( list );
		host.Process( WidgetInput.Key( WidgetKey.End ), TimeSpan.Zero );
		list.SetItems( [ new( "a", "A" ), new( "b", "B" ) ] );
		Assert.Equal( "b", list.CursorKey );
	}

	[Fact]
	public void VisibleRenderingDoesNotVisitWholeList() {
		WidgetList list = new( "list", WidgetSelectionMode.Single );
		list.SetItems( Enumerable.Range( 0, WidgetLimits.MaximumItems ).Select( i => new WidgetListItem( i.ToString(), $"Item {i}" ) ).ToArray() );
		using WidgetHost host = Focus( list, 5 ); host.Render();
		Assert.InRange( list.LastRenderVisitedCount, 1, 5 );
		Assert.Throws<ArgumentOutOfRangeException>( () => list.SetItems( Enumerable.Range( 0, WidgetLimits.MaximumItems + 1 ).Select( i => new WidgetListItem( i.ToString(), "x" ) ).ToArray() ) );
		Assert.Equal( WidgetLimits.MaximumItems, list.Items.Count );
	}

	private static WidgetList CreateList( WidgetSelectionMode mode ) { WidgetList list = new( "list", mode ); list.SetItems( [ new( "a", "A" ), new( "b", "B" ), new( "c", "C" ) ] ); return list; }
	private static WidgetHost Focus( WidgetList list, int rows = 3 ) { WidgetHost host = new( new CursesScreen( 40, rows ), WidgetTheme.Default ); host.Attach( list ); host.Arrange( host.Screen.Bounds ); Assert.True( host.Focus( list ) ); return host; }
}
