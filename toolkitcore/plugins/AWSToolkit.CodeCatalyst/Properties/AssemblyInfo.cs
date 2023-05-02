using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Amazon.AWSToolkit;

using Amazon.AWSToolkit.CodeCatalyst;
using Amazon.AWSToolkit.PluginServices.Activators;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("5465c047-5a2e-424f-9961-3ca4dec2c2cc")]

[assembly: InternalsVisibleTo("AWSToolkit.Tests, PublicKey=" + ToolkitGlobalConstants.StrongNamePublicKey)]
[assembly: InternalsVisibleTo("AwsToolkit.Vs.v16.Tests, PublicKey=" + ToolkitGlobalConstants.StrongNamePublicKey)]
[assembly: InternalsVisibleTo("AwsToolkit.Vs.v17.Tests, PublicKey=" + ToolkitGlobalConstants.StrongNamePublicKey)]

[assembly: PluginActivatorType(typeof(CodeCatalystPluginActivator))]
