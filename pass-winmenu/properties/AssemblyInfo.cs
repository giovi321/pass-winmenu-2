using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Runtime.CompilerServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Pass Winmenu 2")]
[assembly: AssemblyDescription("An easy-to-use Windows interface for pass, with Windows Hello unlock. Based on pass-winmenu by Johan Geluk.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("giovi321")]
[assembly: AssemblyProduct("Pass Winmenu 2")]
[assembly: AssemblyCopyright("Copyright © 2026 giovi321. Based on pass-winmenu by Johan Geluk.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The SDK would normally generate this from SupportedOSPlatformVersion, but
// GenerateAssemblyInfo is disabled; without it the CA1416 analyzer treats every
// WinForms/WPF call as reachable on non-Windows platforms.
[assembly: SupportedOSPlatform("windows10.0.17763.0")]

//In order to begin building localizable applications, set 
//<UICulture>CultureYouAreCodingWith</UICulture> in your .csproj file
//inside a <PropertyGroup>.  For example, if you are using US english
//in your source files, set the <UICulture> to en-US.  Then uncomment
//the NeutralResourceLanguage attribute below.  Update the "en-US" in
//the line below to match the UICulture setting in the project file.

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly: ThemeInfo(
	ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
									 //(used if a resource is not found in the page, 
									 // or application resource dictionaries)
	ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
											  //(used if a resource is not found in the page, 
											  // app, or any theme specific resource dictionaries)
)]


// AssemblyVersion, AssemblyFileVersion and AssemblyInformationalVersion are generated
// by the WriteVersionAttributes target in pass-winmenu.csproj, from the same
// PassWinmenuVersion property that produces the embedded version.txt. Do not add them
// here as well: duplicating them is what let the reported version drift from the file
// version. Bump PassWinmenuVersion in the csproj instead.

// Required for tests
[assembly: InternalsVisibleTo("pass-winmenu-tests")]
[assembly: InternalsVisibleTo("pw")]
// Required for mocking internal interfaces
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
