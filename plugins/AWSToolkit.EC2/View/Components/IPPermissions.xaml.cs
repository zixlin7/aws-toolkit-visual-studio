using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Controller;

using log4net;

namespace Amazon.AWSToolkit.EC2.View.Components
{
    /// <summary>
    /// Interaction logic for IPPermissions.xaml
    /// </summary>
    public partial class IPPermissions
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(IPPermissions));

        IIPPermissionController _controller;
        EC2Constants.PermissionType _permissionType;

        public IPPermissions()
        {
            InitializeComponent();
            this.onDataContextChanged(null, default(DependencyPropertyChangedEventArgs));
            this._ctlDataGrid.DataContextChanged += this.onDataContextChanged;
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._ctlDelete.IsEnabled = false;
            this._ctlToolbar.IsEnabled = this.DataContext != null;
        }

        public void Initialize(IIPPermissionController controller, EC2Constants.PermissionType permissionType)
        {
            this._controller = controller;
            this._permissionType = permissionType;

            this.DataContext = null;
            var binding = permissionType == EC2Constants.PermissionType.Ingress ?
                new Binding("IpIngressPermissions") : new Binding("IpEgressPermissions");

            this._ctlDataGrid.SetBinding(System.Windows.Controls.DataGrid.ItemsSourceProperty, binding);
        }

        void onAddPermission(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.AddPermission(this._permissionType);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error adding ip permission", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error adding ip permission: " + e.Message);
            }
        }

        void onDeletePermission(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._ctlDataGrid.SelectedItems.Count == 0)
                    return;

                List<IPPermissionWrapper> toBeDeleted = new List<IPPermissionWrapper>();
                foreach (IPPermissionWrapper perm in this._ctlDataGrid.SelectedItems)
                {
                    toBeDeleted.Add(perm);
                }

                this._controller.DeletePermission(toBeDeleted, this._permissionType);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting ip permission(s)", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting ip permission(s): " + e.Message);
            }
        }

        void onRefreshPermission(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshPermission(this._permissionType);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing ip permissions", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing ip permissions: " + e.Message);
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
