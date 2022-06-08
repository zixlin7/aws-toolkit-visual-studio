using Microsoft.VisualStudio.Shell;

// Aligning Toolkit on a Newtonsoft.Json version newer than what ships with our lowest supported VS Version (VS 2019)
// https://devblogs.microsoft.com/visualstudio/using-newtonsoft-json-in-a-visual-studio-extension/
[assembly: ProvideCodeBase(AssemblyName = "Newtonsoft.Json")]
