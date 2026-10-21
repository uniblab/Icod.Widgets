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

using System.Text.Json;
using System.Xml.Linq;
using Xunit;

public sealed class ReleaseIdentityTests {
	[Fact]
	public void CandidateAndPackageConsumerUseVersion1100() {
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

		Assert.Equal( "1.10.0", GetSingleValue( production, "Version" ) );
		Assert.Equal( "1.10.0", GetSingleValue( production, "PackageVersion" ) );
		Assert.Equal( "1.0.0.0", GetSingleValue( production, "AssemblyVersion" ) );
		XElement consumerReference = Assert.Single(
			consumer.Descendants(),
			static current => "PackageReference" == current.Name.LocalName
		);
		Assert.Equal( "Icod.Widgets", consumerReference.Attribute( "Include" )?.Value );
		Assert.Equal( "1.10.0", consumerReference.Attribute( "Version" )?.Value );
		XElement dependency = Assert.Single( production.Descendants(), static current =>
			"PackageReference" == current.Name.LocalName && "Icod.DCurses" == current.Attribute( "Include" )?.Value );
		Assert.Equal( "2.5.2", dependency.Attribute( "Version" )?.Value );
	}

	[Fact]
	public void CandidateHasCuratedTwoAxisScrollbarNotes() {
		string notesPath = Path.Combine(
			FindRepositoryRoot(),
			"docs",
			"releases",
			"1.10.0.md"
		);
		Assert.True( File.Exists( notesPath ), $"Missing curated notes: {notesPath}" );

		string notes = File.ReadAllText( notesPath );
		Assert.Contains( "scrollbar", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "auto-hide", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "horizontal", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "Shift+click", notes, StringComparison.Ordinal );
		Assert.Contains( "wheel", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "clamp", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "pointer", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "▲", notes, StringComparison.Ordinal );
		Assert.Contains( "◄", notes, StringComparison.Ordinal );
		Assert.Contains( "ScrollTrack", notes, StringComparison.Ordinal );
		Assert.Contains( "OpenAt", notes, StringComparison.Ordinal );
		Assert.Contains( "AssemblyVersion", notes, StringComparison.Ordinal );
	}

	[Fact]
	public void CuratedNotesIncludeVerificationAndInstall() {
		string root = FindRepositoryRoot();
		string notes = File.ReadAllText( Path.Combine( root, "docs", "releases", "1.10.0.md" ) );

		Assert.Contains( "## Verification", notes, StringComparison.Ordinal );
		Assert.Contains( "509 net10.0 tests", notes, StringComparison.Ordinal );
		Assert.Contains( "Windows", notes, StringComparison.Ordinal );
		Assert.Contains( "Linux", notes, StringComparison.Ordinal );
		Assert.Contains( "macOS", notes, StringComparison.Ordinal );
		Assert.Contains( "dotnet add package Icod.Widgets --version 1.10.0", notes, StringComparison.Ordinal );
	}

	[Fact]
	public void GenericDialogImplementationPlanIsMarkedComplete() {
		string plan = File.ReadAllText( Path.Combine(
			FindRepositoryRoot(),
			"docs",
			"superpowers",
			"plans",
			"2026-10-18-generic-dialog-actions.md"
		) );

		Assert.Contains( "**Status:** Complete", plan, StringComparison.Ordinal );
		Assert.DoesNotContain( "- [ ]", plan, StringComparison.Ordinal );
	}

	[Fact]
	public void PublicApi1100AddsScrollbarAndAnchoredPopupSurface() {
		string root = FindRepositoryRoot(); string fingerprintPath = Path.Combine( root, "docs", "Public-API-Fingerprint-1.10.json" );
		Assert.True( File.Exists( fingerprintPath ), $"Missing API fingerprint: {fingerprintPath}" );
		Assert.True( File.Exists( Path.Combine( root, "packaging", "Get-PublicApiFingerprint.ps1" ) ) );
		using JsonDocument fingerprint = JsonDocument.Parse( File.ReadAllText( fingerprintPath ) );
		Assert.Equal( "1.10.0", fingerprint.RootElement.GetProperty( "packageVersion" ).GetString() );
		Assert.Equal( "1.0.0.0", fingerprint.RootElement.GetProperty( "assemblyVersion" ).GetString() );
		using JsonDocument previous = JsonDocument.Parse( File.ReadAllText( Path.Combine( root, "docs", "Public-API-Fingerprint-1.9.json" ) ) );
		Dictionary<string, string[]> oldTypes = ReadTypes( previous );
		Dictionary<string, string[]> newTypes = ReadTypes( fingerprint );
		Assert.Equal( oldTypes.Keys.Order(), newTypes.Keys.Order() );
		List<string> additions = [];
		foreach ( (string name, string[] oldMembers) in oldTypes ) {
			Assert.Empty( oldMembers.Except( newTypes[ name ], StringComparer.Ordinal ) );
			additions.AddRange( newTypes[ name ].Except( oldMembers, StringComparer.Ordinal ).Select( member => name + " :: " + member ) );
		}
		Assert.Equal( [
			"Icod.Widgets.WidgetEditorOptions :: Method: Boolean get_ShowHorizontalScrollBar()",
			"Icod.Widgets.WidgetEditorOptions :: Method: Void set_ShowHorizontalScrollBar(Boolean)",
			"Icod.Widgets.WidgetEditorOptions :: Property: Boolean ShowHorizontalScrollBar",
			"Icod.Widgets.WidgetPopupMenu :: Method: Void OpenAt(Int32, Int32)",
			"Icod.Widgets.WidgetStyleRole :: Field: Icod.Widgets.WidgetStyleRole ScrollButton",
			"Icod.Widgets.WidgetStyleRole :: Field: Icod.Widgets.WidgetStyleRole ScrollCorner",
			"Icod.Widgets.WidgetStyleRole :: Field: Icod.Widgets.WidgetStyleRole ScrollHover",
			"Icod.Widgets.WidgetStyleRole :: Field: Icod.Widgets.WidgetStyleRole ScrollPressed"
		], additions.Order( StringComparer.Ordinal ) );
	}

	[Fact]
	public void PublicApiFingerprintsUseCrossPlatformLfCheckouts() {
		string attributesPath = Path.Combine( FindRepositoryRoot(), ".gitattributes" );
		Assert.True( File.Exists( attributesPath ), $"Missing Git attributes: {attributesPath}" );
		Assert.Contains(
			"docs/Public-API-Fingerprint-*.json text eol=lf",
			File.ReadAllLines( attributesPath ),
			StringComparer.Ordinal
		);
	}

	[Fact]
	public void ReadmeDocumentsEditorScrollbarWithoutReleaseHistory() {
		string readme = File.ReadAllText( Path.Combine( FindRepositoryRoot(), "README.md" ) );
		Assert.Contains( "ShowVerticalScrollBar", readme, StringComparison.Ordinal );
		Assert.Contains( "ShowHorizontalScrollBar", readme, StringComparison.Ordinal );
		Assert.Contains( "Ctrl+Up", readme, StringComparison.Ordinal );
		Assert.Contains( "ScrollTrack", readme, StringComparison.Ordinal );
		Assert.DoesNotContain( "## Release history", readme, StringComparison.OrdinalIgnoreCase );
		Assert.DoesNotContain( "Icod.Widgets 1.10.0", readme, StringComparison.Ordinal );
	}

	[Fact]
	public void SamplesDocumentWindowsAndEditorScrollbarChecks() {
		string samples = File.ReadAllText( Path.Combine(
			FindRepositoryRoot(),
			"samples",
			"README.md"
		) );

		Assert.Contains( "Windows live verification", samples, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "Alt+F", samples, StringComparison.Ordinal );
		Assert.Contains( "Ctrl+Z", samples, StringComparison.Ordinal );
		Assert.Contains( "click OK", samples, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "borders", samples, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "auto-hide", samples, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "Ctrl+Up", samples, StringComparison.Ordinal );
		Assert.Contains( "wheel", samples, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "arrow", samples, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "track", samples, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "thumb", samples, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "caret", samples, StringComparison.OrdinalIgnoreCase );
	}

	private static Dictionary<string, string[]> ReadTypes( JsonDocument fingerprint ) => fingerprint.RootElement
		.GetProperty( "types" )
		.EnumerateArray()
		.ToDictionary(
			type => type.GetProperty( "name" ).GetString()!,
			type => type.GetProperty( "members" ).EnumerateArray().Select( member => member.GetString()! ).ToArray(),
			StringComparer.Ordinal
		);

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
