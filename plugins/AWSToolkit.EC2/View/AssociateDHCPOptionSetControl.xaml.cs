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

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Util;

using Amazon.EC2;
using Amazon.EC2.Model;
using AMIImage = Amazon.EC2.Model.Image;

using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for AssociateDHCPOptionSet.xaml
    /// </summary>
    public partial class AssociateDHCPOptionSetControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(AssociateDHCPOptionSetControl));

        AssociateDHCPOptionSetController _controller;

        public AssociateDHCPOptionSetControl(AssociateDHCPOptionSetController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title
        {
            get { return "Associate DHCP Options Set";}
        }

        public override bool Validated()
        {
            if (this._controller.Model.IsExisting)
            {
                if (this._controller.Model.SelectedDHCPOptions == null)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("A DHCP Option Set must first be selected");
                    return false;
                }
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.Commit();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error associating DHCP option set", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error associating DHCP option set: " + e.Message);
                return false;
            }
        }

        private void onLoad(object sender, RoutedEventArgs e)
        {
            if (this._ctlExistingDHCPOptions.Items.Count == 0)
            {
                this._ctlActionCreate.IsChecked = true;
                this._ctlActionExisting.IsEnabled = false;
            }
            else
            {
                this._ctlActionExisting.IsChecked = true;
                this._ctlExistingDHCPOptions.SelectedIndex = 0;
            }
        }

        private void onDelete(object sender, RoutedEventArgs evnt)
        {
            if (!(sender is Button))
                return;

            var dhcpOptions = ((Button)sender).DataContext as DHCPOptionsWrapper;
            if (dhcpOptions == null)
                return;

            try
            {
                string message = string.Format("Are you sure you want to delete the DHCP Options set {0}?", dhcpOptions.DhcpOptionsId);
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Subnet", message))
                    return;

                this._controller.DeleteDHCPOptionSet(dhcpOptions);
            }
            catch(Exception e)
            {
                LOGGER.Error("Error deleting DHCP option set", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting DHCP option set: " + e.Message);
            }
        }
    }
}
