using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;
using System.Xml.Linq;

using log4net;

using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit.VersionInfo
{
    public class VersionManager
    {
        static bool PerformedVersionCheck = false;
        static ILog LOGGER = LogManager.GetLogger(typeof(VersionManager));

        public void CheckVersion(object state)
        {
            CheckVersion();
        }

        public void CheckVersion()
        {
            if (PerformedVersionCheck)
                return;

            try
            {
                IList<Version> versions;
                string newUpdateLocation;
                getVersions(out versions, out newUpdateLocation);
                if (versions.Count == 0)
                    return;

                LOGGER.InfoFormat("Current version: {0}", Constants.VERSION_NUMBER);
                bool foundCurrentVersion = false;
                bool shouldPrompt = false;
                for (int i = 0; i < versions.Count; i++)
                {
                    if (Constants.VERSION_NUMBER.Equals(versions[i].Number))
                    {
                        foundCurrentVersion = true;
                        break;
                    }
                    else if (versions[i].ShouldPrompt)
                    {
                        shouldPrompt = true;
                    }
                }

                // If the current version number was not found then this is probably an unreleased version
                // so skip prompting to update.
                if (!foundCurrentVersion)
                {
                    return;
                }

                LOGGER.InfoFormat("Latest Version: {0}", versions[0].Number);

                if (!shouldPrompt)
                    return;

                string value = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RecentUsages)[ToolkitSettingsConstants.VersionCheck][ToolkitSettingsConstants.LastVersionDoNotRemindMe];
                if (versions[0].Number.Equals(value))
                    return;

                this.prompt(versions, newUpdateLocation);
            }
            finally
            {
                PerformedVersionCheck = true;
            }
        }        

        void prompt(IList<Version> versions, string newUpdateLocation)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                NewVersionAlertControl control = new NewVersionAlertControl(versions, newUpdateLocation);
                ToolkitFactory.Instance.ShellProvider.ShowModal(control, System.Windows.MessageBoxButton.OK);

                var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RecentUsages);
                var oc = settings[ToolkitSettingsConstants.VersionCheck];
                oc[ToolkitSettingsConstants.LastVersionDoNotRemindMe] = control.DoNotRemindMeAgain ? versions[0].Number : null;
                PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.RecentUsages, settings);
            }));
        }

        void getVersions(out IList<Version> versions, out string newUpdateLocation)
        {
            try
            {
                string content = getVersionFile();
                if (content == null)
                {
                    versions = new List<Version>();
                    newUpdateLocation = string.Empty;
                    return;
                }

                ParseVersionInfoFile(content, out versions, out newUpdateLocation);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error loading version info", e);
                versions = new List<Version>();
                newUpdateLocation = string.Empty;
            }
        }

        public static void ParseVersionInfoFile(string content, out IList<Version> versions, out string newUpdateLocation)
        {
            versions = new List<Version>();
            XDocument xdoc = XDocument.Parse(content);
            newUpdateLocation = xdoc.Root.Attribute("location").Value;
            var query = from p in xdoc.Elements("versions").Elements("version")
                        select new Version()
                        {
                            Number = p.Attribute("number").Value,
                            ReleaseDate = DateTime.Parse(p.Attribute("release-date").Value),
                            ShouldPrompt = p.Attribute("prompt") != null ? bool.Parse(p.Attribute("prompt").Value) : false,
                            Changes = (from i in p.Elements("change") select (i.Value)).ToList()
                        };

            foreach (Version version in query)
            {
                versions.Add(version);
            }
        }

        string getVersionFile()
        {
            string content = S3FileFetcher.Instance.GetFileContent(Constants.VERSION_INFO_FILE, S3FileFetcher.CacheMode.Never);
            return content;
        }

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
