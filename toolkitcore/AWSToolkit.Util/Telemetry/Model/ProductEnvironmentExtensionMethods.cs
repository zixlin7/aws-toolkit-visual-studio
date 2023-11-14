using System;
using System.Linq;

namespace Amazon.AWSToolkit.Telemetry.Model
{
    public static class ProductEnvironmentExtensionMethods
    {
        public static bool TryGetBaseParentProductVersion(
            this ProductEnvironment productEnvironment,
            out Version baseParentProductVersion)
        {
            baseParentProductVersion = null;

            if (string.IsNullOrWhiteSpace(productEnvironment.ParentProductVersion))
            {
                return false;
            }

            // Pull "17.8.0" from "17.8.0 Preview 7.0"
            var version = productEnvironment.ParentProductVersion
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? string.Empty;

            return Version.TryParse(version, out baseParentProductVersion);
        }
    }
}
