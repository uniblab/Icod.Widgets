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
	public void CandidateAndPackageConsumerUseVersion180() {
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

		Assert.Equal( "1.8.0", GetSingleValue( production, "Version" ) );
		Assert.Equal( "1.8.0", GetSingleValue( production, "PackageVersion" ) );
		Assert.Equal( "1.0.0.0", GetSingleValue( production, "AssemblyVersion" ) );
		XElement consumerReference = Assert.Single(
			consumer.Descendants(),
			static current => "PackageReference" == current.Name.LocalName
		);
		Assert.Equal( "Icod.Widgets", consumerReference.Attribute( "Include" )?.Value );
		Assert.Equal( "1.8.0", consumerReference.Attribute( "Version" )?.Value );
		XElement dependency = Assert.Single( production.Descendants(), static current =>
			"PackageReference" == current.Name.LocalName && "Icod.DCurses" == current.Attribute( "Include" )?.Value );
		Assert.Equal( "2.5.2", dependency.Attribute( "Version" )?.Value );
	}

	[Fact]
	public void CandidateHasCuratedGenericDialogNotes() {
		string notesPath = Path.Combine(
			FindRepositoryRoot(),
			"docs",
			"releases",
			"1.8.0.md"
		);
		Assert.True( File.Exists( notesPath ), $"Missing curated notes: {notesPath}" );

		string notes = File.ReadAllText( notesPath );
		Assert.Contains( "action array", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "mnemonic", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "default", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "cancel", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "presentation", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "obsolete", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "AssemblyVersion", notes, StringComparison.Ordinal );
	}

	[Fact]
	public void CuratedNotesIncludeVerificationAndInstall() {
		string root = FindRepositoryRoot();
		string notes = File.ReadAllText( Path.Combine( root, "docs", "releases", "1.8.0.md" ) );

		Assert.Contains( "## Verification", notes, StringComparison.Ordinal );
		Assert.Contains( "418 tests", notes, StringComparison.Ordinal );
		Assert.Contains( "Windows", notes, StringComparison.Ordinal );
		Assert.Contains( "Linux", notes, StringComparison.Ordinal );
		Assert.Contains( "macOS", notes, StringComparison.Ordinal );
		Assert.Contains( "dotnet add package Icod.Widgets --version 1.8.0", notes, StringComparison.Ordinal );
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
	public void PublicApi180RecordContainsGenericDialogMembers() {
		string root = FindRepositoryRoot(); string fingerprintPath = Path.Combine( root, "docs", "Public-API-Fingerprint-1.8.json" );
		Assert.True( File.Exists( fingerprintPath ), $"Missing API fingerprint: {fingerprintPath}" );
		Assert.True( File.Exists( Path.Combine( root, "docs", "Public-API-Baseline-1.8.md" ) ) );
		Assert.True( File.Exists( Path.Combine( root, "packaging", "Get-PublicApiFingerprint.ps1" ) ) );
		using JsonDocument fingerprint = JsonDocument.Parse( File.ReadAllText( fingerprintPath ) );
		Assert.Equal( "1.8.0", fingerprint.RootElement.GetProperty( "packageVersion" ).GetString() );
		Assert.Equal( "1.0.0.0", fingerprint.RootElement.GetProperty( "assemblyVersion" ).GetString() );
		JsonElement[] types = fingerprint.RootElement.GetProperty( "types" ).EnumerateArray().ToArray();
		string[] actionMembers = Assert.Single( types, type => "Icod.Widgets.WidgetDialogAction" == type.GetProperty( "name" ).GetString() )
			.GetProperty( "members" ).EnumerateArray().Select( member => member.GetString()! ).ToArray();
		Assert.Contains( "Property: System.Nullable`1[System.Char] Mnemonic", actionMembers );
		string[] messageMembers = Assert.Single( types, type => "Icod.Widgets.WidgetMessageDialog" == type.GetProperty( "name" ).GetString() )
			.GetProperty( "members" ).EnumerateArray().Select( member => member.GetString()! ).ToArray();
		Assert.Contains( "Constructor: Void .ctor(System.String, System.String, System.String, System.Collections.Generic.IReadOnlyList`1[Icod.Widgets.WidgetDialogAction])", messageMembers );
		Assert.Contains( "Property: System.Collections.Generic.IReadOnlyList`1[Icod.Widgets.WidgetDialogAction] Actions", messageMembers );
		Assert.Contains( "Method: Void SetActionEnabled(System.String, Boolean)", messageMembers );
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
	public void ReadmeDocumentsGenericDialogsWithoutReleaseHistory() {
		string readme = File.ReadAllText( Path.Combine( FindRepositoryRoot(), "README.md" ) );
		Assert.Contains( "IReadOnlyList<WidgetDialogAction>", readme, StringComparison.Ordinal );
		Assert.Contains( "Mnemonic", readme, StringComparison.Ordinal );
		Assert.Contains( "IsDefault", readme, StringComparison.Ordinal );
		Assert.Contains( "IsCancel", readme, StringComparison.Ordinal );
		Assert.Contains( "Styles", readme, StringComparison.Ordinal );
		Assert.DoesNotContain( "## Release history", readme, StringComparison.OrdinalIgnoreCase );
		Assert.DoesNotContain( "Icod.Widgets 1.8.0", readme, StringComparison.Ordinal );
	}

	[Fact]
	public void SamplesDocumentWindowsMenuDialogAndBorderChecks() {
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
