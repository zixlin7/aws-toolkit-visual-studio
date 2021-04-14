using System;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.CodeCommit.View.Controls;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Regions;
using log4net;

namespace Amazon.AWSToolkit.CodeCommit.Controller
{
    public class CreateRepositoryController
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(CreateRepositoryController));

        /// <summary>
        /// Constructs a controller that will display a dialog for repository selection.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="initialRegion">The initial region binding for the dialog</param>
        /// <param name="defaultCloneFolderRoot">The system default folder for cloned repos, discovered from the registry or a fallback default</param>
        public CreateRepositoryController(AccountViewModel account, ToolkitRegion initialRegion, string defaultCloneFolderRoot)
        {
            if (initialRegion == null)
            {
                throw new ArgumentNullException(nameof(initialRegion));
            }

            Model = new CreateRepositoryModel
            {
                Account = account,
                SelectedRegion = initialRegion,
                BaseFolder = defaultCloneFolderRoot
            };
        }

        public CreateRepositoryModel Model { get; }

        public CreateRepositoryControl View { get; private set; }

        public ActionResults Execute()
        {
            View = new CreateRepositoryControl(this);
            return new ActionResults().WithSuccess(ToolkitFactory.Instance.ShellProvider.ShowModal(View));
        }

    }
}
