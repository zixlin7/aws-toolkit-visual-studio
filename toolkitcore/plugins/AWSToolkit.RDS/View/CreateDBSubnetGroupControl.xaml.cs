using System;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.RDS.Controller;
using log4net;

namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for CreateDBSubnetGroupControl.xaml
    /// </summary>
    public partial class CreateDBSubnetGroupControl : BaseAWSControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(CreateDBSubnetGroupControl));

        public CreateDBSubnetGroupControl()
        {
            InitializeComponent();
        }

        readonly CreateDBSubnetGroupController _controller;

        public CreateDBSubnetGroupControl(CreateDBSubnetGroupController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title => "Create Subnet Group";

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(this._controller.Model.Name))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Name is a required field.");
                return false;
            }

            if (this._controller.Model.Name.Equals("Default", StringComparison.OrdinalIgnoreCase))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("'Default' may not be used for the subnet group name.");
                return false;
            }

            if (string.IsNullOrEmpty(this._controller.Model.Description))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Description is a required field.");
                return false;
            }

            if (this._controller.Model.AssignedSubnets.Count < 2)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("DB subnet groups must contain at least one subnet in at least two AZs in the region.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.CreateDBSubnetGroup();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating subnet group", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating subnet group: " + e.Message);
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            this._ctlName.Focus();
        }

        private void _ctlVPCList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _controller.Model.SelectedZone = null;
            _controller.Model.AssignedSubnets = null;
            _controller.Model.SubnetsForVPCZone = null;
        }

        private void _ctlZoneList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _controller.LoadSubnetsForSelectedVPCAndZone();
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            _controller.AddSelectedZoneSubnet();
        }

        private void OnAddAllClick(object sender, RoutedEventArgs e)
        {
            _controller.AddAllAvailableZonesAndSubnets();
        }

        private void OnRemoveAssignedSubnetClick(object sender, RoutedEventArgs e)
        {
            var subnetid = (sender as Button).Tag as string;
            _controller.RemoveAssignedSubnet(subnetid);
        }
    }
}
