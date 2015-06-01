using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using AWSDeploymentHostManager;

namespace AWSDeploymentUnitTest
{
    internal static class TestUtil
    {
        public static void SetHostManagerConfig(HostManagerConfig newConfig)
        {
            Assembly asm = Assembly.GetAssembly(typeof(HostManager));
            Type type = asm.GetType("AWSDeploymentHostManager.HostManager");
            PropertyInfo configProp = type.GetProperty("Config", BindingFlags.Static | BindingFlags.NonPublic);
            configProp.SetValue(null, newConfig, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
        }

        public static void CallNonPublicHostManagerMethod(HostManager hm, string methodName, object[] args)
        {
            Assembly asm = Assembly.GetAssembly(typeof(HostManager));
            Type type = asm.GetType("AWSBeanstalkHostManager.HostManager");
            MethodInfo exitMethod = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
            exitMethod.Invoke(hm, args);
        }

        public static string CallStringNonPublicHostManagerMethod(HostManager hm, string methodName, object[] args)
        {
            Assembly asm = Assembly.GetAssembly(typeof(HostManager));
            Type type = asm.GetType("AWSBeanstalkHostManager.HostManager");
            MethodInfo exitMethod = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
            return (string)exitMethod.Invoke(hm, args);
        }
    }
}
