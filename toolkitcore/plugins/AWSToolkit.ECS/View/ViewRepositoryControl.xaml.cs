using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.MobileAnalytics;
using log4net;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Interaction logic for ViewRepositoryControl.xaml
    /// </summary>
    public partial class ViewRepositoryControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewRepositoryControl));

        readonly ViewRepositoryController _controller;

        public ViewRepositoryControl(ViewRepositoryController controller)
        {
            this._controller = controller;
            InitializeComponent();
        }

        public override string Title
        {
            get
            {
                return string.Format("Repository {0}", this._controller.RepositoryName);
            }
        }

        public override string UniqueId
        {
            get
            {
                return string.Format("Repository: {0} {1}_{2}",
                                     this._controller.RepositoryArn,
                                     this._controller.EndPoint,
                                     this._controller.Account.SettingsUniqueKey);
            }
        }

        public override bool SupportsBackGroundDataLoad
        {
            get
            {
                return true;
            }
        }

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        void onLoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.Refresh();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing repository data: " + e.Message);
            }
        }

        public override void RefreshInitialData(object initialData)
        {
            try
            {
                this._controller.Refresh();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing repository data: " + e.Message);
            }
        }

        void SortHandler(object sender, DataGridSortingEventArgs e)
        {
            
        }

        private void ViewPushCommands_OnClick(object sender, RoutedEventArgs e)
        {
            if (_ctlPushCommandsFlyover.Visibility == Visibility.Collapsed)
            {
                _ctlPushCommandsFlyover.Visibility = Visibility.Visible;
                _ctlViewHidePushCommands.Content = "Hide Push Commands";
            }
            else
            {
                _ctlPushCommandsFlyover.Visibility = Visibility.Collapsed;
                _ctlViewHidePushCommands.Content = "View Push Commands";
            }
        }

        private void ECRDocumentationLink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            LaunchBrowserToUrl(e.Uri);
            e.Handled = true;
        }

        private void LaunchBrowserToUrl(Uri uri)
        {
            try
            {
                ToolkitFactory.Instance.ShellProvider.OpenInBrowser(uri.ToString(), true);
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to launch process to go to endpoint " + uri, ex);
            }
        }

    }
}
