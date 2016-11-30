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
    /// Interaction logic for RouteTableAssociationControl.xaml
    /// </summary>
    public partial class SubnetAssociationControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(SubnetAssociationControl));

        ISubnetAssociationController _controller;
        ISubnetAssociationWrapper _associatedItem;

        public SubnetAssociationControl()
        {
            InitializeComponent();
            this.onDataContextChanged(null, default(DependencyPropertyChangedEventArgs));
            this._ctlDataGrid.DataContextChanged += this.onDataContextChanged;
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._ctlDelete.IsEnabled = false;
            this._ctlToolbar.IsEnabled = this.DataContext != null;
            this._associatedItem = this.DataContext as ISubnetAssociationWrapper;
        }

        public void Initialize(ISubnetAssociationController controller)
        {
            this._controller = controller;

            this.DataContext = null;
        }

        void onAddSubnetClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                var association = this._controller.AddSubnetAssociation(this._associatedItem);
                if (association != null)
                    Amazon.AWSToolkit.CommonUI.DataGridHelper.SelectAndScrollIntoView(this._ctlDataGrid, association);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error associate subnet", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error associate subnet: " + e.Message);
            }
        }

        void onDeleteSubnetClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                if (this._ctlDataGrid.SelectedItems.Count == 0)
                    return;

                string message = "Are you sure you want to disassociate this subnet?";
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Disassociate Subnet", message))
                    return;

                List<string> toBeDeleted = new List<string>();
                foreach (IAssociationWrapper item in this._ctlDataGrid.SelectedItems)
                {
                    toBeDeleted.Add(item.AssocationId);
                }

                this._controller.DisassociateSubnets(this._associatedItem.VpcId, toBeDeleted);
                this._controller.RefreshAssociations(this._associatedItem);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting subnet(s)", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting subnet(s): " + e.Message);
            }
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.RefreshAssociations(this._associatedItem);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing subnets", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing subnets: " + e.Message);
            }
        }

        void onSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                this._ctlDelete.IsEnabled = this._ctlDataGrid.SelectedItems.Count > 0 && this._associatedItem.CanDisassociate;
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error updating selection", ex);
            }
        }
    }
}
