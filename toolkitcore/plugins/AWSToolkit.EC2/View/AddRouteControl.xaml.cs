using System;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for AddRouteControl.xaml
    /// </summary>
    public partial class AddRouteControl : BaseAWSControl
    {
        AddRouteController _controller;

        public AddRouteControl(AddRouteController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title => "Add Route";

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(this._controller.Model.Destination))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Destination is a required field.");
                return false;
            }
            if (this._controller.Model.SelectedTarget == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Target is a required field.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.AddRoute();
                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error adding route: " + e.Message);
                return false;
            }
        }

        private void _ctlVPC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this._controller.Model.SelectedTarget != null && this._controller.Model.SelectedTarget.Type == AddRouteModel.Target.TargetType.InternetGateway)
            {
                this._controller.Model.Destination = "0.0.0.0/0";
            }
        }
    }
}
