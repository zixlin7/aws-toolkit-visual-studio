using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("AWS Toolkit for Visual Studio")]
[assembly: AssemblyDescription("")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]
[assembly: NeutralResourcesLanguage("en-US")]

// Have this assembly write to the .pkgdef that it will reside at the root of the VSIX, so
// Visual Studio knows where to load it from.
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\Community.VisualStudio.Toolkit.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\LiveCharts.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\LiveCharts.Wpf.dll")]
