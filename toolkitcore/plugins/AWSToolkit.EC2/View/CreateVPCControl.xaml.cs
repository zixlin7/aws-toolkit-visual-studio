using System;
using System.Windows;
using System.Windows.Data;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for CreateVPCControl.xaml
    /// </summary>
    public partial class CreateVPCControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateVPCControl));

        CreateVPCController _controller;

        public CreateVPCControl(CreateVPCController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;

            InitializeComponent();

            // switch on grouping for the instance type dropdown
            var instanceTypesView = (CollectionView)CollectionViewSource.GetDefaultView(_instanceTypeSelector.ItemsSource);
            var familyGroupDescription = new PropertyGroupDescription("HardwareFamily");
            instanceTypesView.GroupDescriptions.Add(familyGroupDescription);
        }

        public override string Title => "Create VPC";

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(this._controller.Model.CIDRBlock))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("CIDR block is a required field.");
                return false;
            }

            if(this._controller.Model.WithPublicSubnet)
            {
                if (string.IsNullOrEmpty(this._controller.Model.PublicSubnetCIDRBlock))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("The public subnet's CIDR block is a required field.");
                    return false;
                }
            }

            if(this._controller.Model.WithPrivateSubnet)
            {
                if (string.IsNullOrEmpty(this._controller.Model.PrivateSubnetCIDRBlock))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("The private subnet's CIDR block is a required field.");
                    return false;
                }
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                var host = FindHost<OkCancelDialogHost>();
                if (host != null)
                    host.IsOkEnabled = false;
                this._controller.CreateVPC();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating vpc", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating vpc: " + e.Message);
            }
            return false;
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            this._ctlName.Focus();
        }

        private void _ctlWithPublic_Click(object sender, RoutedEventArgs e)
        {
            if (this._ctlWithPublic.IsChecked.GetValueOrDefault())
            {
                this._ctlWithPrivate.IsEnabled = true;
            }
            else
            {
                this._ctlWithPrivate.IsEnabled = false;
                this._ctlWithPrivate.IsChecked = false;
            }
        }

        public void CreateAsyncComplete(bool success)
        {
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                var host = FindHost<OkCancelDialogHost>();
                if (host == null)
                    return;

                if (!success)
                    host.IsOkEnabled = true;
                else
                    host.Close(true);
            }));
        }
    }
}
