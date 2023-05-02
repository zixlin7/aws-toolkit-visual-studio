using System.Runtime.InteropServices;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.ECS;
using Amazon.AWSToolkit.PluginServices.Activators;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a09c427c-dbf7-49c8-9590-e99ad60dece2")]

[assembly: PluginActivatorType(typeof(ECSActivator))]

