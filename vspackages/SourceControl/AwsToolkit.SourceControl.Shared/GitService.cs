using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.SourceControl;

using log4net;

using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;

namespace Amazon.AwsToolkit.SourceControl
{
    public class GitService : IGitService
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(GitService));

        private readonly ToolkitContext _toolkitContext;

        private readonly IServiceProvider _serviceProvider;

        public GitService(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            _serviceProvider = _toolkitContext.ToolkitHost.QueryShellProviderService<IServiceProvider>();
        }

        public async Task CloneAsync(Uri remoteUri, string localPath, bool recurseSubmodules = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // For example of using VSSDK to perform git clone:
            // https://github.com/github/VisualStudio/blob/master/src/GitHub.StartPage/StartPagePackage.cs#L63-L96
            // https://github.com/github/VisualStudio/blob/6d428ef3bb1848ae0ece98fd57c9c8fa564aed7f/src/GitHub.TeamFoundation.14/Services/VSGitServices.cs#L74-L111
            try
            {
                var gitActionsExt = await _toolkitContext.ToolkitHost.QueryShellProviderServiceAsync<IGitActionsExt>();
                await gitActionsExt.CloneAsync(remoteUri.ToString(), localPath, recurseSubmodules, cancellationToken, null);
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(IGitActionsExt.CloneAsync)} failed.", ex);
                throw;
            }
        }

        public string GetDefaultRepositoryPath()
        {
            const string collectionPath = @"TeamFoundation\GitSourceControl\General";
            const string propertyName = "DefaultRepositoryPath";

            var clonePath = string.Empty;

            try
            {
                SettingsManager settingsManager = new ShellSettingsManager(_serviceProvider);
                SettingsStore userSettingsStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

                if (userSettingsStore.PropertyExists(collectionPath, propertyName))
                {
                    clonePath = userSettingsStore.GetString(collectionPath, propertyName);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error loading the default git clone path from the registry", ex);
            }

            if (string.IsNullOrEmpty(clonePath))
            {
                clonePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Source", "Repos");
            }

            return clonePath;
        }
    }
}
