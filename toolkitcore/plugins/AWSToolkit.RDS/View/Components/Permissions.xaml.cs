using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.RDS.Controller;
using Amazon.AWSToolkit.RDS.Model;

using log4net;

namespace Amazon.AWSToolkit.RDS.View.Components
{
    /// <summary>
    /// Interaction logic for Permissions.xaml
    /// </summary>
    public partial class Permissions
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(Permissions));

        ViewDBSecurityGroupsController _controller;
        public Permissions()
        {
            InitializeComponent();

            this.DataContextChanged += onDataContextChanged;
        }

        public DBSecurityGroupWrapper DBSecurityGroup => this.DataContext as DBSecurityGroupWrapper;

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._ctlDelete.IsEnabled = false;
            this._ctlToolbar.IsEnabled = this.DataContext != null;
        }

        public void Initialize(ViewDBSecurityGroupsController controller)
        {
            this._controller = controller;
            this.DataContext = null;
        }

        void onAddPermission(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.AddPermission(this.DBSecurityGroup);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error adding permission", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error adding permission: " + e.Message);
            }
        }

        void onDeletePermission(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._ctlDataGrid.SelectedItems.Count == 0)
                    return;

                var toBeDeleted = new List<PermissionRule>();
                foreach (PermissionRule perm in this._ctlDataGrid.SelectedItems)
                {
                    toBeDeleted.Add(perm);
                }

                this._controller.DeletePermission(this.DBSecurityGroup, toBeDeleted);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting permission(s)", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting permission(s): " + e.Message);
            }
        }

        void onRefreshPermission(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshPermissions(this.DBSecurityGroup);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing ip permissions", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing permissions: " + e.Message);
            }
        }

        void onSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                this._ctlDelete.IsEnabled = this._ctlDataGrid.SelectedItems.Count > 0;
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error updating selection", ex);
            }
        }
    }
}
