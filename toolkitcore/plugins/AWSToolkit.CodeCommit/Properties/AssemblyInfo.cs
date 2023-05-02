using System.Runtime.InteropServices;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.CodeCommit;
using Amazon.AWSToolkit.PluginServices.Activators;

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("9aa7dd53-7bc4-4971-bc0c-0540a391dc22")]

[assembly: PluginActivatorType(typeof(CodeCommitActivator))]
