using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.RDS.Controller;
using log4net;

namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for CreateSecurityGroupControl.xaml
    /// </summary>
    public partial class CreateSecurityGroupControl : BaseAWSControl
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CreateSecurityGroupControl));

        public CreateSecurityGroupControl()
        {
            InitializeComponent();
        }

        CreateSecurityGroupController _controller;

        public CreateSecurityGroupControl(CreateSecurityGroupController controller)
        {
            InitializeComponent();
            _controller = controller;
            DataContext = _controller.Model;
        }

        public override string Title => "Create Security Group";

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(_controller.Model.Name))
            {
                _controller.ToolkitContext.ToolkitHost.ShowError("Name is a required field.");
                return false;
            }

            foreach (var c in _controller.Model.Name)
            {
                if (!char.IsLetterOrDigit(c) && c != '-')
                {
                    throw new Exception("Name must contain only letters, digits, or hyphens.");
                }
            }

            if (string.IsNullOrEmpty(_controller.Model.Description))
            {
                _controller.ToolkitContext.ToolkitHost.ShowError("Description is a required field.");
                return false;
            }
            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                _controller.CreateSecurityGroup();
                return true;
            }
            catch (Exception e)
            {
                _logger.Error("Error creating security group", e);
                _controller.ToolkitContext.ToolkitHost.ShowError("Error creating security group: " + e.Message);

                // Record failures immediately -- the top level call records success/cancel once the dialog is closed
                _controller.RecordMetric(ActionResults.CreateFailed(e));
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            _ctlName.Focus();
        }
    }
}
