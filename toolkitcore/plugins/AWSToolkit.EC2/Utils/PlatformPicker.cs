using System.Collections.Generic;

namespace Amazon.AWSToolkit.EC2.Utils
{
    public class PlatformPicker
    {
        public static readonly PlatformPicker ALL_PLATFORMS = new PlatformPicker("All Platforms");
        public static readonly PlatformPicker WINDOWS = new PlatformPicker("Windows");
        public static readonly PlatformPicker LINUX = new PlatformPicker("Linux");

        private PlatformPicker(string displayName)
        {
            this.DisplayName = displayName;
        }

        static PlatformPicker[] _allPlatforms;

        public static IEnumerable<PlatformPicker> AllPlatforms => _allPlatforms;

        static PlatformPicker()
        {
            _allPlatforms = new PlatformPicker[]
            {
                ALL_PLATFORMS,
                WINDOWS,
                LINUX
            };
        }

        public string DisplayName
        {
            get;
        }
    }
}
