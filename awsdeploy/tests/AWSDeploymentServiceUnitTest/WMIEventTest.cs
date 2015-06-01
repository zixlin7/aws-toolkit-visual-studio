using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Win32;

using AWSDeploymentHostManagerApp;
using AWSDeploymentService;

namespace MagicHarpServiceUnitTest
{
    // This test requires that the installultil gets run on the MagicHarpServiceUnitTest.dll so that the WMI namespace, etc. gets registered.
    // "c:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe $(TargetPath)"
    // The project should have a post-build step that does this.

    [TestClass]
    public class WMIEventTest
    {
        //[TestMethod]
        public void TestQueryConfigObject()
        {
            WMIProvider.ConfigServiceInstance.Publish();

            WMIProvider.ConfigServiceInstance.Instance.ConfigurationComplete = true;

            WMIProvider.EC2_ConfigServiceEvent.Publish(
                WMIProvider.EC2_ConfigServiceEventType.ConfigurationComplete,
                WMIProvider.ConfigServiceInstance.Instance.ID,
                    "Some Message"
            );

            //Write registry entry with current WMI instance ID.
            RegistryKey serviceKey = Registry.LocalMachine.CreateSubKey(
                @"SYSTEM\CurrentControlSet\services\Ec2Config\Parameters");
            serviceKey.SetValue("WmiIdentifier", WMIProvider.ConfigServiceInstance.Instance.ID, RegistryValueKind.String);

            Assert.IsTrue(EC2ConfigUtil.CheckInstanceReady());

            WMIProvider.ConfigServiceInstance.Instance.ConfigurationComplete = false;

            WMIProvider.EC2_ConfigServiceEvent.Publish(
                WMIProvider.EC2_ConfigServiceEventType.ConfigurationComplete,
                WMIProvider.ConfigServiceInstance.Instance.ID,
                    "Some Message"
            );

            Assert.IsFalse(EC2ConfigUtil.CheckInstanceReady());
        }

        // Sample: Prints all the instances of the EC2_ConfigService management object.
        private void SampleManagementObjectQuery()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\Amazon", "SELECT * FROM EC2_ConfigService");

            foreach (ManagementObject queryObj in searcher.Get())
            {
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("EC2_ConfigService instance");
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("ConfigurationComplete: {0}", queryObj["ConfigurationComplete"]);
                Console.WriteLine("ID: {0}", queryObj["ID"]);
            }
        }
    }


}
