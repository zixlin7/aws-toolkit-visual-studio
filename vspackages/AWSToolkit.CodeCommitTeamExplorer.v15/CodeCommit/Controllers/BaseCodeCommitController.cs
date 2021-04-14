using System;
using System.IO;
using Microsoft.Win32;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement;
using Amazon.AWSToolkit.Regions;
using log4net;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controllers
{
    /// <summary>
    /// Base controller class for all CodeCommit work originating inside Team Explorer.
    /// </summary>
    internal abstract class BaseCodeCommitController
    {
        const string TeamExplorerGitKey = @"SOFTWARE\Microsoft\VisualStudio\15.0\TeamFoundation\GitSourceControl";

        protected IAWSCodeCommit CodeCommitPlugin { get; set; }
        protected AccountViewModel Account { get; set; }
        protected ToolkitRegion Region { get; set; }

        protected ILog Logger { get; set; }

        public abstract void Execute();

        protected BaseCodeCommitController()
        {
            // Before commencing any work, we'll have gotten the user to supply their
            // AWS credentials so by now we have access to the account bound to Team Explorer 
            // as well as a default region from the navigator.
            Account = TeamExplorerConnection.ActiveConnection.Account;
            Region = ToolkitFactory.Instance.Navigator.SelectedRegion;

            if (Account.PartitionId != Region.PartitionId && Account.Region != null)
            {
                // User is using an account for a different Partition in TeamExplorer than what is
                // selected in the AWS Explorer. Try to initialize to the account's default region
                // so that Clone/Create dialogs try to select a partition-relevant region.
                Region = Account.Region;
            }
        }

        /// <summary>
        /// Looks for service specific credentials to allow https access to git in CodeCommit.
        /// If no credentials are available locally we will attempt to create a set, and if that
        /// fails (or credentials exist but are not local) we will prompt the user to supply them.
        /// </summary>
        /// <returns></returns>
        public ServiceSpecificCredentials ObtainGitCredentials()
        {
            return CodeCommitPlugin.ObtainGitCredentials(Account, Region, false);
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
                using (var key = Registry.CurrentUser.OpenSubKey(TeamExplorerGitKey + "\\General", true))
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
