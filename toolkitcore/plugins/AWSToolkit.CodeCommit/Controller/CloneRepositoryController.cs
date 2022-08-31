using System;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.CodeCommit.View.Controls;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CodeCommit.Controller
{
    /// <summary>
    /// Controller for prompting the user to select a repository for cloning.
    /// </summary>
    public class CloneRepositoryController
    {
        private readonly ToolkitContext _toolkitContext;

        /// <summary>
        /// Constructs a controller that will display a dialog for repository selection.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="initialRegion">The initial region binding for the dialog</param>
        /// <param name="defaultCloneFolderRoot">The system default folder for cloned repos, discovered from the registry or a fallback default</param>
        /// <param name="toolkitContext"></param>
        public CloneRepositoryController(AccountViewModel account, ToolkitRegion initialRegion,
            string defaultCloneFolderRoot, ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;

            if (initialRegion == null)
            {
                throw new ArgumentNullException(nameof(initialRegion));
            }

            Model = new CloneRepositoryModel
            {
                Account = account,
                SelectedRegion = initialRegion,
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

        public void BrowseForBaseFolder()
        {
            var dlg = _toolkitContext.ToolkitHost.GetDialogFactory().CreateFolderBrowserDialog();
            dlg.Title = "Select folder to clone repository to";
            dlg.FolderPath = Model.BaseFolder;

            if (dlg.ShowModal())
            {
                Model.BaseFolder = dlg.FolderPath;
            }
        }
    }
}
