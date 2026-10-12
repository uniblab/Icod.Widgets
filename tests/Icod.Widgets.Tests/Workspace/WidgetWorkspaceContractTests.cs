/*
	Icod.Widgets.Tests
	Automated tests for the Icod.Widgets library.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.Widgets.Tests.Workspace;

using System;
using Icod.DCurses;
using Xunit;

public sealed class WidgetWorkspaceContractTests {
	[Fact]
	public void BaselinePreservesAssemblyAndBoundsTabs() {
		Assert.Equal( 1024, WidgetLimits.MaximumTabs );
		Assert.Equal( new Version( 1, 0, 0, 0 ), typeof( WidgetHost ).Assembly.GetName().Version );
	}

	[Fact]
	public void PublicCustomContainerCanArrangeAndEmit() {
		using WidgetHost host = new( new CursesScreen( 20, 3 ), WidgetTheme.Default );
		ActionContainer control = new( "outer" );
		WidgetButton child = new( "inner", "Run", "inner.run" );
		control.Add( child ); host.Attach( control ); control.Arrange( host.Screen.Bounds );
		Assert.Equal( control.Bounds, child.Bounds );
		Assert.True( host.Focus( control ) );
		WidgetDispatchResult result = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.Zero );
		Assert.Equal( "outer.run", Assert.IsType<WidgetInvokedAction>( Assert.Single( result.Actions ) ).CommandId );
	}

	private sealed class ActionContainer( string id ) : WidgetContainer( id ) {
		public override bool IsFocusable => true;
		protected override void OnArrange( CursesRectangle bounds ) {
			foreach ( Widget child in this.Children ) child.Arrange( bounds );
		}
		protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
			if ( input.KeyCode != WidgetKey.Enter ) return false;
			this.EnsureActionCapacity(); this.EmitAction( new WidgetInvokedAction( this.Id, "outer.run" ) ); return true;
		}
	}
}
