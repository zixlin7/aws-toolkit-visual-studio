using System.Runtime.InteropServices;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.CloudFormation;
using Amazon.AWSToolkit.PluginServices.Activators;

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("9a9366c1-0c1f-4bd7-bceb-183e66229ebf")]

[assembly: PluginActivatorType(typeof(CloudFormationActivator))]
