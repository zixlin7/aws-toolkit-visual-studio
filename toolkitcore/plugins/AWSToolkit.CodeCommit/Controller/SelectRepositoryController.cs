using System;
using System.IO;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Controls;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.Navigator;
using log4net;

namespace Amazon.AWSToolkit.CodeCommit.Controller
{
    /// <summary>
    /// Controller for prompting the user to select a repository for cloning.
    /// </summary>
    public class SelectRepositoryController
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(SelectRepositoryController));

        /// <summary>
        /// Constructs a controller that will display a dialog for repository selection.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="initialRegion">The initial region binding for the dialog</param>
        /// <param name="defaultCloneFolderRoot">The system default folder for cloned repos, discovered from the registry or a fallback default</param>
        public SelectRepositoryController(AccountViewModel account, RegionEndPointsManager.RegionEndPoints initialRegion, string defaultCloneFolderRoot)
        {
            Model = new SelectRepositoryModel
            {
                Account = account,
                SelectedRegion = initialRegion ?? RegionEndPointsManager.Instance.GetRegion("us-east-1"),
                LocalFolder = defaultCloneFolderRoot
            };
        }

        public SelectRepositoryModel Model { get; }

        public SelectRepositoryControl View { get; private set; }

        public ActionResults Execute()
        {
            View = new SelectRepositoryControl(this);
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(View))
            {
                // for now, append the repo name onto the selected path - we'll want to show
                // this in the dialog eventually
                var finalPathComponent = Path.GetFileName(Model.LocalFolder);
                if (!finalPathComponent.Equals(Model.SelectedRepository.Name, StringComparison.OrdinalIgnoreCase))
                {
                    Model.LocalFolder = Path.Combine(Model.LocalFolder, Model.SelectedRepository.Name);
                }

                Model.RepositoryUrl = Model.SelectedRepository.RepositoryMetadata.CloneUrlHttp;
                return new ActionResults().WithSuccess(true);
            }

            return new ActionResults().WithSuccess(false);   
        }
    }
}
