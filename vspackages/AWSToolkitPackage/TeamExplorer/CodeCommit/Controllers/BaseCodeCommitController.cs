using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Account.Model;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CredentialManagement;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controllers
{
    /// <summary>
    /// Base controller class for all CodeCommit work originating inside Team Explorer.
    /// </summary>
    internal abstract class BaseCodeCommitController
    {
        protected IAWSCodeCommit CodeCommitPlugin { get; set; }
        protected AccountViewModel Account { get; set; }
        protected RegionEndPointsManager.RegionEndPoints Region { get; set; }

        protected ILog Logger { get; set; }

        public abstract void Execute();

        protected BaseCodeCommitController()
        {
            // Before commencing any work, we'll have gotten the user to supply their
            // AWS credentials so by now we have access to the account bound to Team Explorer 
            // as well as a default region from the navigator.
            Account = TeamExplorerConnection.ActiveConnection.Account;
            Region = ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints;
        }

        /// <summary>
        /// Looks for service specific credentials to allow https access to git in CodeCommit.
        /// If no credentials are available locally we will attempt to create a set, and if that
        /// fails (or credentials exist but are not local) we will prompt the user to supply them.
        /// </summary>
        /// <returns></returns>
        public ServiceSpecificCredentials ObtainGitCredentials()
        {
            return CodeCommitPlugin.ObtainGitCredentials(Account, Region);
        }

        /// <summary>
        /// Try and determine the user-preferred local clone path, falling back to the same
        /// default as github if necessary.
        /// </summary>
        /// <returns></returns>
        public string GetLocalClonePathFromGitProvider()
        {
            var clonePath = string.Empty;

            try
            {
#if VS2017_OR_LATER
                const string TEGitKey = @"SOFTWARE\Microsoft\VisualStudio\15.0\TeamFoundation\GitSourceControl";
#else
                const string TEGitKey = @"SOFTWARE\Microsoft\VisualStudio\14.0\TeamFoundation\GitSourceControl";
#endif

                using (var key = Registry.CurrentUser.OpenSubKey(TEGitKey + "\\General", true))
                {
                    clonePath = (string)key?.GetValue("DefaultRepositoryPath", string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames);
                }
            }
            catch (Exception e)
            {
                Logger?.ErrorFormat("Error loading the default cloning path from the registry '{0}'", e);
            }

            if (string.IsNullOrEmpty(clonePath))
            {
                clonePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Source", "Repos");
            }

            return clonePath;
        }

    }
}
