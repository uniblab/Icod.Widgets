/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
*/
namespace Icod.Widgets.Tests.Core;

using Icod.DCurses;
using Xunit;

public sealed class WidgetHostTests {
	[Fact]
	public void ActionsDrainExactlyOnce() {
		using WidgetHost host = CreateHost();
		ProbeWidget button = new( "button", focusable: true );
		host.Attach( button );
		host.Arrange( new CursesRectangle( 0, 0, 4, 20 ) );
		host.Focus( button );

		WidgetDispatchResult result = host.Process(
			WidgetInput.Key( WidgetKey.Enter ),
			TimeSpan.Zero );

		WidgetInvokedAction action = Assert.IsType<WidgetInvokedAction>( Assert.Single( result.Actions ) );
		Assert.Equal( "probe.invoke", action.CommandId );
		Assert.Empty( host.DrainActions() );
	}

	[Fact]
	public void ClockCannotMoveBackwards() {
		using WidgetHost host = CreateHost();
		host.Process( WidgetInput.Key( WidgetKey.Tab ), TimeSpan.FromSeconds( 2 ) );

		Assert.Throws<ArgumentOutOfRangeException>( () => host.Process(
			WidgetInput.Key( WidgetKey.Tab ),
			TimeSpan.FromSeconds( 1 ) ) );
	}

	[Fact]
	public void DetachingFocusedWidgetSelectsEligibleSuccessor() {
		using WidgetHost host = CreateHost();
		WidgetStack stack = new( "root", WidgetOrientation.Vertical );
		ProbeWidget first = new( "first", focusable: true );
		ProbeWidget second = new( "second", focusable: true );
		stack.Add( first );
		stack.Add( second );
		host.Attach( stack );
		host.Arrange( new CursesRectangle( 0, 0, 10, 20 ) );
		host.Focus( first );

		stack.Remove( first );

		Assert.Same( second, host.FocusedWidget );
	}

	[Fact]
	public void WidgetLimitRejectsBeforeAttachment() {
		using WidgetHost host = CreateHost();
		WidgetStack stack = new( "root", WidgetOrientation.Vertical );
		for ( int index = 0; index < WidgetLimits.MaximumWidgetsPerHost; index++ ) {
			stack.Add( new ProbeWidget( $"item-{index}" ) );
		}

		Assert.Throws<InvalidOperationException>( () => host.Attach( stack ) );
		Assert.Equal( 0, host.AttachedCount );
	}

	[Fact]
	public void WidgetLimitAcceptsExactlyMaximum() {
		using WidgetHost host = CreateHost();
		WidgetStack stack = new( "root", WidgetOrientation.Vertical );
		for ( int index = 1; index < WidgetLimits.MaximumWidgetsPerHost; index++ ) {
			stack.Add( new ProbeWidget( $"item-{index}" ) );
		}

		host.Attach( stack );

		Assert.Equal( WidgetLimits.MaximumWidgetsPerHost, host.AttachedCount );
	}

	[Fact]
	public void DuplicateIdRejectsWholeTreeBeforeAttachment() {
		using WidgetHost host = CreateHost();
		WidgetStack stack = new( "root", WidgetOrientation.Vertical );
		stack.Add( new ProbeWidget( "duplicate", focusable: true ) );
		stack.Add( new ProbeWidget( "duplicate", focusable: true ) );

		Assert.Throws<InvalidOperationException>( () => host.Attach( stack ) );
		Assert.Equal( 0, host.AttachedCount );
		Assert.Null( host.FocusedWidget );
	}

	[Fact]
	public void DisposedHostKeepsBorrowedScreenAlive() {
		CursesScreen screen = new( 20, 4 );
		WidgetHost host = new( screen, WidgetTheme.Default );

		host.Dispose();

		Assert.Equal( 20, screen.Columns );
		Assert.Equal( 4, screen.Rows );
	}

	private static WidgetHost CreateHost() => new( new CursesScreen( 80, 25 ), WidgetTheme.Default );

	private sealed class ProbeWidget( string id, bool focusable = false ) : Widget( id ) {
		public override bool IsFocusable => focusable;

		protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
			if ( WidgetKey.Enter != input.KeyCode ) {
				return false;
			}
			host.Enqueue( new WidgetInvokedAction( this.Id, "probe.invoke" ) );
			return true;
		}
	}
}
