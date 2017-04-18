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
    public class CloneRepositoryController
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(CloneRepositoryController));

        /// <summary>
        /// Constructs a controller that will display a dialog for repository selection.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="initialRegion">The initial region binding for the dialog</param>
        /// <param name="defaultCloneFolderRoot">The system default folder for cloned repos, discovered from the registry or a fallback default</param>
        public CloneRepositoryController(AccountViewModel account, RegionEndPointsManager.RegionEndPoints initialRegion, string defaultCloneFolderRoot)
        {
            Model = new CloneRepositoryModel
            {
                Account = account,
                SelectedRegion = initialRegion ?? RegionEndPointsManager.Instance.GetRegion("us-east-1"),
                BaseFolder = defaultCloneFolderRoot
            };
        }

        public CloneRepositoryModel Model { get; }

        public CloneRepositoryControl View { get; private set; }

        public ActionResults Execute()
        {
            View = new CloneRepositoryControl(this);
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(View))
            {
                Model.RepositoryUrl = Model.SelectedRepository.RepositoryMetadata.CloneUrlHttp;
                return new ActionResults().WithSuccess(true);
            }

            return new ActionResults().WithSuccess(false);   
        }
    }
}
