using System;
using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.Registration
{
    /// <summary>
    /// Reusable registration class for command line parameters
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    internal sealed class AWSCommandLineRegistrationAttribute : RegistrationAttribute
    {
        public string CommandLineToken { get; set; }
        public int Arguments { get; set; }
        public bool DemandLoad { get; set; }
        public string HelpString { get; set; }

        public override void Register(RegistrationAttribute.RegistrationContext context)
        {
            using (Key appCommandLine = context.CreateKey("AppCommandLine"))
            {
                using (Key awsToolkitPluginsKey = appCommandLine.CreateSubkey(CommandLineToken))
                {
                    awsToolkitPluginsKey.SetValue("Arguments", Arguments.ToString()); // must be REG_SZ form
                    awsToolkitPluginsKey.SetValue("DemandLoad", DemandLoad ? 1 : 0); // REG_DWORD needed here
                    awsToolkitPluginsKey.SetValue("HelpString", string.IsNullOrEmpty(HelpString) ? string.Empty : HelpString);

                    // needs to be in B format
                    awsToolkitPluginsKey.SetValue("Package", "{" + GuidList.guidPackageString + "}");

                    awsToolkitPluginsKey.Close();
                }

                appCommandLine.Close();
            }
        }

        public override void Unregister(RegistrationAttribute.RegistrationContext context)
        {
        }
    }
}
