/*
	Icod.Widgets.Tests
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: GPL-3.0-or-later
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
