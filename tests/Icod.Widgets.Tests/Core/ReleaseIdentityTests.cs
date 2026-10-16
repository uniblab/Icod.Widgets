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

using System.Xml.Linq;
using Xunit;

public sealed class ReleaseIdentityTests {
	[Fact]
	public void CandidateAndPackageConsumerUseVersion161() {
		string root = FindRepositoryRoot();
		XDocument production = XDocument.Load(
			Path.Combine( root, "Icod.Widgets.csproj" )
		);
		XDocument consumer = XDocument.Load( Path.Combine(
			root,
			"tests",
			"PackageConsumer",
			"PackageConsumer.csproj"
		) );

		Assert.Equal( "1.6.1", GetSingleValue( production, "Version" ) );
		Assert.Equal( "1.6.1", GetSingleValue( production, "PackageVersion" ) );
		Assert.Equal( "1.0.0.0", GetSingleValue( production, "AssemblyVersion" ) );
		XElement reference = Assert.Single(
			consumer.Descendants(),
			static current => "PackageReference" == current.Name.LocalName
		);
		Assert.Equal( "Icod.Widgets", reference.Attribute( "Include" )?.Value );
		Assert.Equal( "1.6.1", reference.Attribute( "Version" )?.Value );
	}

	[Fact]
	public void CandidateHasCuratedDependencyCorrectionNotes() {
		string notesPath = Path.Combine(
			FindRepositoryRoot(),
			"docs",
			"releases",
			"1.6.1.md"
		);
		Assert.True( File.Exists( notesPath ), $"Missing curated notes: {notesPath}" );

		string notes = File.ReadAllText( notesPath );
		Assert.Contains( "Icod.DCurses 2.5.1", notes, StringComparison.Ordinal );
		Assert.Contains( "AssemblyVersion", notes, StringComparison.Ordinal );
		Assert.Contains( "no public API changes", notes, StringComparison.OrdinalIgnoreCase );
	}

	private static string GetSingleValue(
		XDocument project,
		string localName
	) {
		return project.Descendants()
			.Single( current => localName == current.Name.LocalName )
			.Value;
	}

	private static string FindRepositoryRoot() {
		DirectoryInfo? current = new( AppContext.BaseDirectory );
		while ( current is not null ) {
			if ( File.Exists( Path.Combine( current.FullName, "Icod.Widgets.sln" ) ) ) {
				return current.FullName;
			}
			current = current.Parent;
		}
		throw new InvalidOperationException( "Repository root not found." );
	}
}
