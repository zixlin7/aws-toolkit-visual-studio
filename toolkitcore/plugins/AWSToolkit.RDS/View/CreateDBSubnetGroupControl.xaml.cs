using System;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.RDS.Controller;
using log4net;

namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for CreateDBSubnetGroupControl.xaml
    /// </summary>
    public partial class CreateDBSubnetGroupControl : BaseAWSControl
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CreateDBSubnetGroupControl));

        public CreateDBSubnetGroupControl()
        {
            InitializeComponent();
        }

        private readonly CreateDBSubnetGroupController _controller;

        public CreateDBSubnetGroupControl(CreateDBSubnetGroupController controller)
        {
            InitializeComponent();
            _controller = controller;
            DataContext = _controller.Model;
        }

        public override string Title => "Create Subnet Group";

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(_controller.Model.Name))
            {
                _controller.ToolkitContext.ToolkitHost.ShowError("Name is a required field.");
                return false;
            }

            if (_controller.Model.Name.Equals("Default", StringComparison.OrdinalIgnoreCase))
            {
                _controller.ToolkitContext.ToolkitHost.ShowError("'Default' may not be used for the subnet group name.");
                return false;
            }

            if (string.IsNullOrEmpty(_controller.Model.Description))
            {
                _controller.ToolkitContext.ToolkitHost.ShowError("Description is a required field.");
                return false;
            }

            if (_controller.Model.AssignedSubnets.Count < 2)
            {
                _controller.ToolkitContext.ToolkitHost.ShowError("DB subnet groups must contain at least one subnet in at least two AZs in the region.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                _controller.CreateDBSubnetGroup();
                return true;
            }
            catch (Exception e)
            {
                _logger.Error("Error creating subnet group", e);
                _controller.ToolkitContext.ToolkitHost.ShowError("Error creating subnet group: " + e.Message);

                // Record failures immediately -- the top level call records success/cancel once the dialog is closed
                _controller.RecordMetric(ActionResults.CreateFailed(e));
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            _ctlName.Focus();
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
