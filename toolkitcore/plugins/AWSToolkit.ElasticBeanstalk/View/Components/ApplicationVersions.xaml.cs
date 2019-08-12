using System;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    /// <summary>
    /// Interaction logic for ApplicationVersions.xaml
    /// </summary>
    public partial class ApplicationVersions
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(MonitorGraphs));

        ApplicationStatusController _controller;

        public ApplicationVersions()
        {
            InitializeComponent();
            this._ctlDataGrid.SelectionChanged += new SelectionChangedEventHandler(onSelectionChanged);
            this.onSelectionChanged(this, null);
        }

        public void Initialize(ApplicationStatusController controller)
        {
            this._controller = controller;
        }

        void onDeployVersion(object sender, RoutedEventArgs e)
        {
            try
            {
                var version = getSelectedVersion();
                if (version == null)
                    return;

                this._controller.DeployedVersion(version);
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Deploying",
                    string.Format("Error deploying version: {0}", ex.Message));
            }
        }

        void onDeleteVersion(object sender, RoutedEventArgs e)
        {
            try
            {
                string msg;
                if (this._ctlDataGrid.SelectedItems.Count == 1)
                {
                    msg = string.Format("Are you sure you want to delete version {0} of application {1}?  " +
                            "This will terminate any environments running this version of the application.", getSelectedVersion().VersionLabel, this._controller.Model.ApplicationName);
                }
                else
                {
                    msg = string.Format("Are you sure you want to delete the {0} selected versions for application {1}?  " +
                            "This will terminate any environments running these versions of the application.", this._ctlDataGrid.SelectedItems.Count, this._controller.Model.ApplicationName);
                }

                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Application Version", msg))
                    return;

                var toBeRemoved = new ApplicationVersionDescriptionWrapper[this._ctlDataGrid.SelectedItems.Count];
                this._ctlDataGrid.SelectedItems.CopyTo(toBeRemoved, 0);
                foreach (ApplicationVersionDescriptionWrapper version in toBeRemoved)
                    this._controller.DeleteVersion(version);
            }
            catch(Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Deleting",
                    string.Format("Error deleting version: {0}", ex.Message));
            }
        }

        ApplicationVersionDescriptionWrapper getSelectedVersion()
        {
            var version = this._ctlDataGrid.SelectedItem as ApplicationVersionDescriptionWrapper;
            if (version == null)
                return null;

            return version;
        }

        void onSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this._ctlDelete.IsEnabled = false;
            this._ctlDeploy.IsEnabled = false;

            if (this._ctlDataGrid.SelectedItems.Count == 1)
            {
                this._ctlDeploy.IsEnabled = true;
                this._ctlDelete.IsEnabled = true;
            }
            else if(this._ctlDataGrid.SelectedItems.Count >= 1)
            {
                this._ctlDelete.IsEnabled = true;
            }
        }

        void onGridContextMenu(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            MenuItem deploy = new MenuItem() { Header = "Publish" };
            deploy.Click += this.onDeployVersion;
            deploy.Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.publish.png");
            MenuItem delete = new MenuItem() { Header = "Delete" };
            delete.Click += this.onDeleteVersion;
            delete.Icon = IconHelper.GetIcon("delete.png");

            delete.IsEnabled = false;
            deploy.IsEnabled = false;

            if (this._ctlDataGrid.SelectedItems.Count == 1)
            {
                deploy.IsEnabled = true;
                delete.IsEnabled = true;
            }
            else if (this._ctlDataGrid.SelectedItems.Count >= 1)
            {
                delete.IsEnabled = true;
            }

            menu.Items.Add(deploy);
            menu.Items.Add(delete);

            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }
    }
}
