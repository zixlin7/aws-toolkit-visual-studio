using System;
using System.Collections.Generic;

using log4net;

namespace Amazon.AWSToolkit.VersionInfo
{
    public class VersionManager
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(VersionManager));

        public static bool IsVersionGreaterThanToolkit(string versionString)
        {
#if DEBUG
            return false;
#else

            string[] toolkitParts = Constants.VERSION_NUMBER.Split('.');
            string[] versionParts = versionString.Split('.');

            if (toolkitParts.Length != 4 || versionParts.Length != 4)
                return true;

            for (int i = 0; i < 4; i++)
            {
                int tp = 0;
                int vp = 0;
                if (!int.TryParse(toolkitParts[i], out tp))
                    return true;
                if (!int.TryParse(versionParts[i], out vp))
                    return true;

                // Toolkit has a greater version number.
                if (vp < tp)
                    return false;

                // Passed in version is later
                if (vp > tp)
                    return true;
            }

            return false;
#endif
        }


        public class Version
        {
            public Version()
            {
            }

            public string Number
            {
                get;
                set;
            }

            public DateTime ReleaseDate
            {
                get;
                set;
            }

            public IList<string> Changes
            {
                get;
                set;
            }

            public bool ShouldPrompt
            {
                get;
                set;
            }
        }
    }
}
