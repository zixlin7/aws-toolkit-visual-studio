using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using log4net;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    /// <summary>
    /// Retrieves contents from the Toolkit embedded resources.
    /// </summary>
    public class AssemblyResourceFetcher : IResourceFetcher
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(AssemblyResourceFetcher));

        public AssemblyResourceFetcher()
        {
        }

        /// <summary>
        /// Requests contents from a specific resource in this assembly (AWSToolkit.Util)
        /// </summary>
        /// <returns>Stream of contents, null if there was an error or no contents were available.</returns>
        public Task<Stream> GetAsync(string relativePath, CancellationToken token = default)
        {
            try
            {
                var assemblyPath = "Amazon.AWSToolkit.HostedFiles." + relativePath
                    .Replace(@"\", "/")
                    .Replace('/', '.');

                var stream = this.GetType().Assembly
                    .GetManifestResourceStream(assemblyPath);

                if (stream != null)
                {
                    Logger.Info($"Resource loaded from Assembly: {relativePath} ({assemblyPath})");
                }

                return Task.FromResult(stream);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to resource from assembly: {relativePath}", e);
                return null;
            }
        }
    }
}
