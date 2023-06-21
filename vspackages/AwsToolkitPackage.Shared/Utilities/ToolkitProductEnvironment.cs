using System;
using System.Runtime.InteropServices;

using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AWSToolkit.VisualStudio.Utilities.VsAppId;

using EnvDTE80;

using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.Utilities
{
    internal static class ToolkitProductEnvironment
    {
        internal static ProductEnvironment CreateProductEnvironment(IVsAppId vsAppId, DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var env = new ProductEnvironment
            {
                AwsProduct = ProductEnvironment.Default.AwsProduct,
                AwsProductVersion = typeof(ToolkitProductEnvironment).Assembly.GetName().Version.ToString(),
                OperatingSystem = ProductEnvironment.Default.OperatingSystem,
                OperatingSystemArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                OperatingSystemVersion = ProductEnvironment.Default.OperatingSystemVersion,
                ParentProduct = GetParentProduct(vsAppId, dte),
                ParentProductVersion = GetParentProductVersion(vsAppId, dte),
            };

            return env;
        }

        private static string GetParentProduct(IVsAppId vsAppId, DTE2 dte)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (vsAppId.TryGetProperty(VSAPropID.VSAPROPID_AppShortBrandName, out object brandName))
                {
                    return brandName.ToString(); // eg: "VS Professional 2017"
                }

                return GetParentProduct(dte);
            }
            catch (Exception)
            {
                return GetParentProduct(dte);
            }
        }
        
        /// <summary>
        /// This is the fallback resolver, returning a less exact VS Product Name
        /// </summary>
        private static string GetParentProduct(DTE2 dte)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return $"Visual Studio {dte.Edition}"; // eg: "Professional"
            }
            catch (Exception)
            {
                return ProductEnvironment.Default.ParentProduct;
            }
        }

        private static string GetParentProductVersion(IVsAppId vsAppId, DTE2 dte)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (vsAppId.TryGetProperty(VSAPropID.VSAPROPID_ProductDisplayVersion, out object version))
                {
                    return version.ToString(); // eg: "15.9.20"
                }

                return GetParentProductVersion(dte);
            }
            catch (Exception)
            {
                return GetParentProductVersion(dte);
            }
        }

        /// <summary>
        /// This is the fallback resolver, returning a less exact VS Product version
        /// </summary>
        private static string GetParentProductVersion(DTE2 dte)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return dte.Version; // eg: "15.0" (doesn't indicate more than the major version)
            }
            catch (Exception)
            {
                return ProductEnvironment.Default.ParentProductVersion;
            }
        }
    }
}
