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

namespace Icod.Widgets.Tests.Core;

using Icod.DCurses;
using Xunit;

public sealed class DependencyBoundaryTests {
	[Fact]
	public void CandidateHostUsesOnlyPublicDcursesApi() {
		CursesScreen screen = new( 80, 25 );
		using WidgetHost host = new( screen, WidgetTheme.Default );

		Assert.Same( screen, host.Screen );
		Assert.Equal( 0, host.AttachedCount );
	}

	[Fact]
	public void LibraryDoesNotCompileGplSources() {
		string[] resourceNames = typeof( WidgetHost ).Assembly.GetManifestResourceNames();

		Assert.DoesNotContain( resourceNames, name =>
			name.Contains( ".Tests.", StringComparison.Ordinal )
			|| name.Contains( ".Sample.", StringComparison.Ordinal ) );
	}
}
