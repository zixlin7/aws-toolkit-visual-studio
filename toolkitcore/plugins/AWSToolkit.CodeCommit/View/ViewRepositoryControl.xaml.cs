using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Amazon.AWSToolkit.CodeCommit.Controller;
using Amazon.AWSToolkit.CodeCommit.Model;

namespace Amazon.AWSToolkit.CodeCommit.View
{
    /// <summary>
    /// Interaction logic for ViewRepositoryControl.xaml
    /// </summary>
    public partial class ViewRepositoryControl
    {
        public ViewRepositoryControl()
        {
            InitializeComponent();
        }

        public ViewRepositoryControl(ViewRepositoryController controller)
            : this()
        {
            Model = controller.Model;
            DataContext = this;
        }

        public override string Title => Model == null ? null : string.Concat("Repository ", Model.Name);

        public ViewRepositoryModel Model { get; }

        private void onLinkNavigate(object sender, RequestNavigateEventArgs e)
        {
            const string fileScheme = "file";
            const string httpsScheme = "https";
            const string httpScheme = "http";

            try
            {
                if (e.Uri.Scheme.Equals(httpsScheme, StringComparison.OrdinalIgnoreCase)
                    || e.Uri.Scheme.Equals(httpScheme, StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
                }
                else if (e.Uri.Scheme.Equals(fileScheme, StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(e.Uri.AbsolutePath);
                }
                else
                {
                    throw new InvalidOperationException("Unknown sheme: " + e.Uri.Scheme);
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error navigating to repository: " + ex.Message);
            }
        }

        private void _ctlClone_OnClick(object sender, RoutedEventArgs e)
        {/*
            try
            {
                var controller = new CloneRepositoryController();
                var results = controller.Execute(Controller.Account,
                    Model.ProjectResources.CodeCommitRepositoryModel.RepositoryName,
                    Model.ProjectResources.CodeCommitRepositoryModel.CloneUrlHttp);

                if (results.Success)
                {
                    Model.UpdateLocalWorkspaceFolder(controller.Model.RepositoryFolder);
                }
            }
            catch (Exception exc)
            {
                LOGGER.Error(exc);
                ToolkitFactory.Instance.ShellProvider.ShowError("Clone Error", exc.Message);
            }
        */
        }


    }
}
