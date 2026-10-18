#:property TargetFramework=net10.0
#:property EnableTrimAnalyzer=false
#:property EnableAotAnalyzer=false

using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

if ( args.Length != 3 ) {
	Console.Error.WriteLine(
		"Usage: dotnet run --file PublicApiFingerprint.cs -- <assembly> <package-version> <output>"
	);
	return 2;
}

string assemblyPath = Path.GetFullPath( args[ 0 ] );
string packageVersion = args[ 1 ];
string outputPath = Path.GetFullPath( args[ 2 ] );
AssemblyDependencyResolver resolver = new( assemblyPath );
string targetFramework = Path.GetFileName( Path.GetDirectoryName( assemblyPath )! );
Dictionary<string, string> packageVersions = ReadPackageVersions(
	Path.ChangeExtension( assemblyPath, ".deps.json" )
);
AssemblyLoadContext.Default.Resolving += ( context, name ) => {
	string? path = resolver.ResolveAssemblyToPath( name );
	path ??= ResolveNuGetAssembly( name, targetFramework, packageVersions );
	return path is null ? null : context.LoadFromAssemblyPath( path );
};
Assembly assembly = Assembly.LoadFrom( assemblyPath );
BindingFlags declaredPublic = BindingFlags.Public
	| BindingFlags.Instance
	| BindingFlags.Static
	| BindingFlags.DeclaredOnly;

object[] types = assembly.GetExportedTypes()
	.OrderBy( static type => type.FullName, StringComparer.Ordinal )
	.Select( type => new {
		name = type.FullName!,
		kind = GetKind( type ),
		members = GetMembers( type, declaredPublic )
	} )
	.ToArray();

var fingerprint = new {
	packageVersion,
	assemblyVersion = assembly.GetName().Version?.ToString() ?? string.Empty,
	generatedFrom = "public reflection surface",
	types
};
JsonSerializerOptions options = new() {
	TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
	WriteIndented = true
};
string json = JsonSerializer.Serialize( fingerprint, options )
	.Replace( "\r\n", "\n", StringComparison.Ordinal )
	.Replace( '\r', '\n' )
	+ "\n";
Directory.CreateDirectory( Path.GetDirectoryName( outputPath )! );
File.WriteAllText( outputPath, json );
return 0;

static string GetKind( Type type ) {
	if ( type.IsInterface ) {
		return "interface";
	}
	if ( type.IsEnum ) {
		return "enum";
	}
	if ( type.IsAbstract ) {
		return "abstract-class";
	}
	return "class";
}

static string[] GetMembers( Type type, BindingFlags flags ) {
	return type.GetConstructors( flags )
		.Select( static member => "Constructor: " + member )
		.OrderBy( static member => member, StringComparer.Ordinal )
		.Concat( type.GetMethods( flags )
			.Select( static member => "Method: " + member )
			.OrderBy( static member => member, StringComparer.Ordinal ) )
		.Concat( type.GetProperties( flags )
			.Select( static member => "Property: " + member )
			.OrderBy( static member => member, StringComparer.Ordinal ) )
		.Concat( type.GetFields( flags )
			.Select( static member => "Field: " + member )
			.OrderBy( static member => member, StringComparer.Ordinal ) )
		.Concat( type.GetEvents( flags )
			.Select( static member => "Event: " + member )
			.OrderBy( static member => member, StringComparer.Ordinal ) )
		.ToArray();
}

static Dictionary<string, string> ReadPackageVersions( string depsPath ) {
	Dictionary<string, string> packages = new( StringComparer.OrdinalIgnoreCase );
	using JsonDocument document = JsonDocument.Parse( File.ReadAllText( depsPath ) );
	foreach ( JsonProperty library in document.RootElement.GetProperty( "libraries" ).EnumerateObject() ) {
		int separator = library.Name.LastIndexOf( '/' );
		if ( 0 < separator ) {
			packages[ library.Name[ ..separator ] ] = library.Name[ ( separator + 1 ).. ];
		}
	}
	return packages;
}

static string? ResolveNuGetAssembly(
	AssemblyName name,
	string targetFramework,
	IReadOnlyDictionary<string, string> packageVersions
) {
	if ( name.Name is null || !packageVersions.TryGetValue( name.Name, out string? version ) ) {
		return null;
	}
	string packagesRoot = Environment.GetEnvironmentVariable( "NUGET_PACKAGES" )
		?? Path.Combine(
			Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
			".nuget",
			"packages"
		);
	string packageRoot = Path.Combine( packagesRoot, name.Name.ToLowerInvariant(), version );
	if ( !Directory.Exists( packageRoot ) ) {
		return null;
	}
	string expected = Path.Combine(
		packageRoot,
		"lib",
		targetFramework,
		name.Name + ".dll"
	);
	if ( File.Exists( expected ) ) {
		return expected;
	}
	return Directory.GetFiles( packageRoot, name.Name + ".dll", SearchOption.AllDirectories )
		.OrderBy( static path => path, StringComparer.Ordinal )
		.FirstOrDefault();
}
