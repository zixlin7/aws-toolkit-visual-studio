using System;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ChangeTerminationProtectionControl.xaml
    /// </summary>
    public partial class ChangeTerminationProtectionControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ChangeTerminationProtectionControl));

        ChangeTerminationProtectionController _controller;

        public ChangeTerminationProtectionControl(ChangeTerminationProtectionController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title => "Change Termination Protection";

        public override bool OnCommit()
        {
            try
            {
                this._controller.ChangeTerminationProtection();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error changing termination protection", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error changing termination protection: " + e.Message);
                return false;
            }
        }
    }
}
