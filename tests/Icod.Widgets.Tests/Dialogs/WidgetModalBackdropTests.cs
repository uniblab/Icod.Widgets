/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Tests.Dialogs;
using Icod.DCurses;
using Xunit;

public sealed class WidgetModalBackdropTests {
	[Fact]
	public void ThemedBackdropPreservesUnderlyingCellsAndDisappearsWithModal() {
		CursesScreen screen = new( 30, 10 );
		CursesStyle original = new( CursesColor.Indexed( 7 ), CursesColor.Indexed( 4 ) );
		CursesStyle dim = new( CursesColor.Indexed( 8 ), CursesColor.Indexed( 0 ) );
		screen.StandardWindow.Write( "界 background", original );
		using WidgetHost host = new( screen, WidgetTheme.Default with { Parts = WidgetStyles.Empty.With( WidgetStyleRole.ModalBackdrop, dim ) } );
		WidgetMessageDialog dialog = WidgetMessageDialog.Ok( "about", "About", "Menu sample" );
		using WidgetModalLease lease = host.ShowModal( dialog ); dialog.Arrange( new( 2, 4, 5, 20 ) ); host.Render();
		CursesPanel[] visible = Panels( screen ).Where( panel => panel.IsVisible ).ToArray();
		Assert.Equal( 2, visible.Length ); Assert.Equal( screen.Bounds, visible[ 0 ].Bounds );
		Assert.Equal( "界", visible[ 0 ].ContentWindow.GetCell( 0, 0 ).Content );
		Assert.Equal( dim, visible[ 0 ].ContentWindow.GetCell( 0, 0 ).Style );
		Assert.True( visible[ 0 ].ContentWindow.GetCell( 0, 1 ).IsContinuation );
		Assert.Equal( original, screen.StandardWindow.GetCell( 0, 0 ).Style );
		host.SetTheme( WidgetTheme.Default ); host.Render();
		Assert.Single( Panels( screen ), panel => panel.IsVisible );
		lease.Dispose(); host.Render(); Assert.DoesNotContain( Panels( screen ), panel => panel.IsVisible );
		Assert.Equal( original, screen.StandardWindow.GetCell( 0, 0 ).Style );
	}

	[Fact]
	public void NestedModalBackdropIncludesParentAndReturnsToIt() {
		CursesScreen screen = new( 30, 10 );
		CursesStyle dim = CursesStyle.Default.WithAttributes( CursesTextAttributes.Dim );
		using WidgetHost host = new( screen, WidgetTheme.Default with { Parts = WidgetStyles.Empty.With( WidgetStyleRole.ModalBackdrop, dim ) } );
		WidgetMessageDialog parent = WidgetMessageDialog.Ok( "parent", "Parent", "Parent text" );
		using WidgetModalLease first = host.ShowModal( parent ); parent.Arrange( new( 1, 1, 7, 25 ) );
		WidgetMessageDialog child = WidgetMessageDialog.Ok( "child", "Child", "Child text" );
		using WidgetModalLease second = host.ShowModal( child ); child.Arrange( new( 3, 5, 5, 20 ) ); host.Render();
		CursesPanel[] visible = Panels( screen ).Where( panel => panel.IsVisible ).ToArray();
		Assert.Equal( 3, visible.Length ); Assert.Equal( screen.Bounds, visible[ 1 ].Bounds );
		Assert.Equal( "┌", visible[ 1 ].ContentWindow.GetCell( 1, 1 ).Content );
		Assert.Equal( dim, visible[ 1 ].ContentWindow.GetCell( 1, 1 ).Style );
		second.Dispose(); host.Render(); Assert.Same( parent, host.FocusedWidget );
		Assert.Equal( parent.Bounds, Panels( screen ).Last( panel => panel.IsVisible ).Bounds );
	}

	// Inspect DCurses' actual composition order without requiring a live terminal.
	private static CursesPanel[] Panels( CursesScreen screen ) => (CursesPanel[])typeof( CursesScreen )
		.GetMethod( "SnapshotPanelsBottomToTop", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic )!.Invoke( screen, null )!;
}
