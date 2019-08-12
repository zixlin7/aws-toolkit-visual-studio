using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Controller;

using log4net;

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for BucketPropertiesControl.xaml
    /// </summary>
    public partial class BucketPropertiesControl : BaseAWSControl
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(BucketPropertiesControl));

        BucketPropertiesController _controller;
        BucketPropertiesModel _model;

        public BucketPropertiesControl()
            : this(null)
        {
        }

        public BucketPropertiesControl(BucketPropertiesController controller)
        {
            this._controller = controller;
            this._model = controller.Model;
//            this.DataContext = this._model;

            InitializeComponent();
            this._ctlLifecycle.Initialize(this._controller);
            this._ctlBucketNotifications.Initialize(this._controller);
            this.DataContextChanged += onDataContextChanged;
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!isBucketNameValidDNS())
            {
                this._ctlMainWebSitePanel.IsEnabled = false;
                this._ctlDNSWarning.Visibility = Visibility.Visible;
            }
        }

        bool isBucketNameValidDNS()
        {
            foreach (char c in this._model.BucketName)
            {
                if(!(c == '.' || c == '-' || Char.IsDigit(c) || char.IsLower(c)))
                    return false;
            }

            return true;
        }

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public BucketPropertiesModel Model => this._controller.Model;

        public override string Title => string.Format("S3 Bucket Properties: {0}", this._model.BucketName);


        private void OnAddPermission(object sender, RoutedEventArgs args)
        {
            this._controller.AddPermission();
            this._ctlPermissionDataGrid.SelectedIndex = this.Model.PermissionEntries.Count - 1;

            DataGridHelper.PutCellInEditMode(this._ctlPermissionDataGrid, this._ctlPermissionDataGrid.SelectedIndex, 0);
        }

        private void OnRemovePermission(object sender, RoutedEventArgs args)
        {
            List<Permission> itemsToBeRemoved = new List<Permission>();
            foreach (Permission entry in this._ctlPermissionDataGrid.SelectedItems)
            {
                itemsToBeRemoved.Add(entry);
            }

            foreach (Permission entry in itemsToBeRemoved)
            {
                this.Model.PermissionEntries.Remove(entry);
            }
        }


        private void onCreateBucketClick(object sender, RoutedEventArgs e)
        {
            this._controller.CreateBucket();
        }

        public override bool Validated()
        {
            this._ctlLifecycle.CommitEdit();
            if (this._model.IsLoggingEnabled && string.IsNullOrEmpty(this._model.LoggingTargetBucket))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(
                    "Target bucket name is required when enabling logging.");
                return false;
            }
            if (this._model.IsWebSiteEnabled && string.IsNullOrEmpty(this._model.WebSiteIndexDocument))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(
                    "Index Document is required when enabling web site configuration.");
                return false;
            }

            try
            {
                foreach (var rule in this.Model.LifecycleRules)
                {
                    rule.Validate();
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(e.Message);
                return false;
            }
            
            return true;
        }

        private void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.Refresh();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing S3 bucket: " + e.Message);
            }
        }

        private void onApplyChangesClick(object sender, RoutedEventArgs evnt)
        {
            if (!this.Validated())
                return;

            try
            {
                this._controller.Persist();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error applying changes", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error applying changes to S3 bucket: " + e.Message);
            }
        }

        private void onWebsiteRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (this._ctlWebSiteLink.IsEnabled)
            {
                string url = this._model.WebSiteEndPoint;
                Process.Start(new ProcessStartInfo(url));
                e.Handled = true;
            }
        }
    }
}
