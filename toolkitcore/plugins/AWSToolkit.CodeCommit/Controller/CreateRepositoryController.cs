using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.CodeCommit.View;
using Amazon.AWSToolkit.Navigator;
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
        public CreateRepositoryController(AccountViewModel account, RegionEndPointsManager.RegionEndPoints initialRegion, string defaultCloneFolderRoot)
        {
            Model = new CreateRepositoryModel
            {
                Account = account,
                SelectedRegion = initialRegion ?? RegionEndPointsManager.Instance.GetRegion("us-east-1"),
                LocalFolder = defaultCloneFolderRoot
            };
        }

        public CreateRepositoryModel Model { get; }

        public CreateRepositoryControl View { get; private set; }

        public ActionResults Execute()
        {
            View = new CreateRepositoryControl(this);
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(View))
            {
                // for now, append the repo name onto the selected path - we'll want to show
                // this in the dialog eventually
                /*
                var finalPathComponent = Path.GetFileName(Model.LocalFolder);
                if (!finalPathComponent.Equals(Model.SelectedRepository.Name, StringComparison.OrdinalIgnoreCase))
                {
                    Model.LocalFolder = Path.Combine(Model.LocalFolder, Model.SelectedRepository.Name);
                }

                Model.RepositoryUrl = Model.SelectedRepository.RepositoryMetadata.CloneUrlHttp;
                return new ActionResults().WithSuccess(true);
                */
            }

            return new ActionResults().WithSuccess(false);
        }

    }
}
