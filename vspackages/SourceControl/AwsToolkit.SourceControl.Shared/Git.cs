using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;

using log4net;

using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;

using sh = Microsoft.VisualStudio.Shell;

namespace Amazon.AwsToolkit.SourceControl
{
    public class Git
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(Git));

        private readonly ToolkitContext _toolkitContext;

        public Git(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public async Task CloneAsync(Uri remoteUri, string localPath, bool recurseSubmodules = false,
            IProgress<sh.ServiceProgressData> downloadProgress = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // For example of using VSSDK to perform git clone:
            // https://github.com/github/VisualStudio/blob/master/src/GitHub.StartPage/StartPagePackage.cs#L63-L96
            // https://github.com/github/VisualStudio/blob/6d428ef3bb1848ae0ece98fd57c9c8fa564aed7f/src/GitHub.TeamFoundation.14/Services/VSGitServices.cs#L74-L111
            try
            {
                var gitActionsExt = await _toolkitContext.ToolkitHost.QueryShellProviderServiceAsync<IGitActionsExt>();
                await gitActionsExt.CloneAsync(remoteUri.ToString(), localPath, recurseSubmodules, cancellationToken, downloadProgress);
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(IGitActionsExt.CloneAsync)} failed.", ex);
                throw;
            }
        }
    }
}
