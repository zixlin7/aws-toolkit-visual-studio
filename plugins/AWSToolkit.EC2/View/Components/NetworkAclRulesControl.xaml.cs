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
    /// Interaction logic for NetworkAclEntriesControl.xaml
    /// </summary>
    public partial class NetworkAclRulesControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(NetworkAclRulesControl));

        ViewNetworkAclsController _controller;
        EC2Constants.PermissionType _permissionType;
        NetworkAclWrapper _networkAcl;

        public NetworkAclRulesControl()
        {
            InitializeComponent(); this.onDataContextChanged(null, default(DependencyPropertyChangedEventArgs));
            this._ctlDataGrid.DataContextChanged += this.onDataContextChanged;
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._ctlDelete.IsEnabled = false;
            this._ctlToolbar.IsEnabled = this.DataContext != null;
            this._networkAcl = this.DataContext as NetworkAclWrapper;
        }

        public void Initialize(ViewNetworkAclsController controller, EC2Constants.PermissionType permissionType)
        {
            this._controller = controller;
            this._permissionType = permissionType;

            if (permissionType == EC2Constants.PermissionType.Ingress)
            {
                this._ctlDirection.Header = "Source";
            }
            else
            {
                this._ctlDirection.Header = "Destination";
            }

            this.DataContext = null;
            var binding = permissionType == EC2Constants.PermissionType.Ingress ?
                new Binding("IngressEntries") : new Binding("EgressEntries");

            this._ctlDataGrid.SetBinding(System.Windows.Controls.DataGrid.ItemsSourceProperty, binding);
        }

        void onAddRule(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.AddRule(this._networkAcl, this._permissionType);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error adding network acl rule", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error adding network acl rule: " + e.Message);
            }
        }

        void onDeleteRule(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._ctlDataGrid.SelectedItems.Count == 0)
                    return;

                List<NetworkAclEntryWrapper> toBeDeleted = new List<NetworkAclEntryWrapper>();
                foreach (NetworkAclEntryWrapper item in this._ctlDataGrid.SelectedItems)
                {
                    toBeDeleted.Add(item);
                }

                this._controller.DeleteRules(this._networkAcl, toBeDeleted, this._permissionType);
                this._controller.RefreshRules(this._networkAcl, this._permissionType);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting network acl rule(s)", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting network acl rule(s): " + e.Message);
            }
        }

        void onRefreshRules(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshRules(this._networkAcl, this._permissionType);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing network acl rules", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing network acl rules: " + e.Message);
            }
        }

        void onSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                this._ctlDelete.IsEnabled = this._ctlDataGrid.SelectedItems.Count > 0;

                if (this._ctlDataGrid.SelectedItems.Count == 1 && ((NetworkAclEntryWrapper)this._ctlDataGrid.SelectedItems[0]).NativeNetworkAclEntry.RuleNumber == NetworkAclEntryWrapper.DEFAULT_RULE_NUMBER)
                {
                    this._ctlDelete.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error updating selection", ex);
            }
        }
    }
}
