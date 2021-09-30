using System;

using Amazon.AWSToolkit.Util;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.Utilities.DTE
{
    public static class DteVersion
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DteVersion));

        /// <summary>
        /// Represents the minimum VS version supported by the Toolkit
        /// </summary>
        public static readonly IToolkitHostInfo MinimumVersion = GetMinimumSupportedVsVersion();

        private static IToolkitHostInfo GetMinimumSupportedVsVersion()
        {
#if VS2022
            return ToolkitHosts.Vs2022;
#elif VS2017_OR_LATER
            return ToolkitHosts.Vs2017;
#endif
        }

        /// <summary>
        /// Converts the Visual Studio shell's version (<see cref="EnvDTE.DTE.Version"/>)
        /// to a strongly typed version.
        /// </summary>
        /// <param name="shellVersion">DTE Version value to convert</param>
        /// <returns>An object representing the VS Major version (<see cref="ToolkitHosts"/>)</returns>
        public static IToolkitHostInfo AsHostInfo(string shellVersion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(shellVersion))
                {
                    return MinimumVersion;
                }

                if (shellVersion.StartsWith("12"))
                {
                    return ToolkitHosts.Vs2013;
                }

                if (shellVersion.StartsWith("14"))
                {
                    return ToolkitHosts.Vs2015;
                }

                if (shellVersion.StartsWith("15"))
                {
                    return ToolkitHosts.Vs2017;
                }

                if (shellVersion.StartsWith("16"))
                {
                    return ToolkitHosts.Vs2019;
                }

                if (shellVersion.StartsWith("17"))
                {
                    return ToolkitHosts.Vs2022;
                }

                return MinimumVersion;
            }
            catch (Exception e)
            {
                Logger.Error("Unable to determine Host version, assuming minimum supported version.", e);
                return MinimumVersion;
            }
        }
    }
}
