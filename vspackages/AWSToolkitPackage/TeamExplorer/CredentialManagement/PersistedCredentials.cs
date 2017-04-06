using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Persistence;
using log4net;
using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CredentialManagement
{
    /// <summary>
    /// Keeps track of git credentials we have persisted to the OS credential
    /// store, so that on disconnect in Team Explorer we can clear them out.
    /// </summary>
    internal static class PersistedCredentials
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(PersistedCredentials));
        private static readonly HashSet<string> PersistedTargets = new HashSet<string>();

        private const string settingsFileFolderName = "codecommit";
        private const string settingsFileName = "credentialtargets.json";
        private const string targetsPropertyName = "CredentialTargets";

        /// <summary>
        /// Registers a persisted set of credentials based on the 'target',
        /// which can be thought of as a store key.
        /// </summary>
        /// <param name="target"></param>
        public static void RegisterPersistedTarget(string target)
        {
            if (PersistedTargets.Contains(target))
                return;

            PersistedTargets.Add(target);
            SavePersistedTargets();
        }

        /// <summary>
        /// Removes a specific target from the persisted set.
        /// </summary>
        /// <param name="target"></param>
        public static void DeregisterPersistedTarget(string target)
        {
            if (!PersistedTargets.Contains(target))
            {
                LOGGER.ErrorFormat("Received request to deregister unknown credential target {0}", target);
                return;
            }

            PersistedTargets.Remove(target);
            SavePersistedTargets();
        }

        /// <summary>
        /// Clears all persisted credential targets from both the
        /// backing file and the OS credential store. Used when we
        /// 'disconnect' from CodeCommit in Team Explorer.
        /// </summary>
        public static void ClearAllPersistedTargets()
        {
            foreach (var t in PersistedTargets)
            {
                if (!GitCredentials.Delete(t, GitCredentials.CredentialType.Generic))
                {
                     LOGGER.ErrorFormat("Attempt to clear credential target {0} failed.", t);
                }
            }

            PersistedTargets.Clear();
            SavePersistedTargets();
        }

        /// <summary>
        /// Serializes the set of persisted targets to disk.
        /// </summary>
        private static void SavePersistedTargets()
        {
            var storefile = GetSettingsFilePath();
            var data = PersistedTargetsToJson();
            using (var writer = new StreamWriter(storefile))
            {
                writer.Write(data);
            }
        }

        private static string PersistedTargetsToJson()
        {
            var jsonWriter = new JsonWriter {PrettyPrint = true};
            jsonWriter.WriteObjectStart();

            jsonWriter.WritePropertyName(targetsPropertyName);
            jsonWriter.WriteArrayStart();
            foreach (var t in PersistedTargets)
            {
                jsonWriter.Write(t);
            }
            jsonWriter.WriteArrayEnd();

            jsonWriter.WriteObjectEnd();

            return jsonWriter.ToString();
        }

        private static string GetSettingsFilePath()
        {
            var settingsFolder = PersistenceManager.GetSettingsStoreFolder();
            var location = Path.Combine(settingsFolder, settingsFileFolderName);

            if (!Directory.Exists(location))
                Directory.CreateDirectory(location);

            return Path.Combine(location, settingsFileName);
        }

        /// <summary>
        /// Loads from disk the set of targets we have persisted credentials for.
        /// </summary>
        static PersistedCredentials()
        {
            
        }
    }
}
