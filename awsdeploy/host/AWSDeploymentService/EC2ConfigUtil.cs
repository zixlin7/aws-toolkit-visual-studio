using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Management;

using log4net;

namespace AWSDeploymentService
{
    public class EC2ConfigUtil
    {
        private static ILog LOGGER = LogManager.GetLogger(typeof(HarpStringService));

        private const string
            WMI_AMAZON_NS = "root\\Amazon",
            WMI_QUERY = "SELECT * FROM EC2_ConfigService",
            WMI_CONFIG_ID = "ID",
            WMI_CONFIG_COMPLETE = "ConfigurationComplete",
            WMI_REGISTRY_KEY = @"SYSTEM\CurrentControlSet\services\Ec2Config\Parameters",
            WMI_ID_NAME = "WmiIdentifier",
            WMI_ID_UNDEFINED = "UNDEFINED";

        public static string WMIInstanceId
        {
            get
            {
                string _wmiInstanceId = null;
                RegistryKey serviceKey = Registry.LocalMachine.OpenSubKey(WMI_REGISTRY_KEY);
                
                string tmp = null;
                if (serviceKey != null) 
                    tmp = serviceKey.GetValue(WMI_ID_NAME, WMI_ID_UNDEFINED).ToString();
                if (null != tmp && tmp.Length > 0 && !tmp.Equals(WMI_ID_UNDEFINED))
                {
                    _wmiInstanceId = tmp;
                    LOGGER.InfoFormat("Found WMI Instance ID: {0}", _wmiInstanceId);
                }
                else
                {
                    LOGGER.Warn("Failed to read WMI Instance ID from the registry.");
                }

                return _wmiInstanceId;
            }
        }

        // Look for a WMI configuration object which matches the WmiIdentifier registry value
        // and check to see if its ConfigComplete parameter is true.
        public static bool CheckInstanceReady()
        {
            if (null == WMIInstanceId)
                return false;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(WMI_AMAZON_NS, WMI_QUERY);

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (queryObj[WMI_CONFIG_ID].ToString().Equals(WMIInstanceId) && Convert.ToBoolean(queryObj[WMI_CONFIG_COMPLETE]))
                        return true;
                }
            }
            catch(Exception e)
            {
                LOGGER.Warn("Failed to get WMI Cofiguration object", e);
                return false;
            }

            return false;
        }
        
    }
}
