using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.RDS.Controller;
using log4net;

namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for CreateSecurityGroupControl.xaml
    /// </summary>
    public partial class CreateSecurityGroupControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateSecurityGroupControl));

        public CreateSecurityGroupControl()
        {
            InitializeComponent();
        }

        CreateSecurityGroupController _controller;

        public CreateSecurityGroupControl(CreateSecurityGroupController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title => "Create Security Group";

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(this._controller.Model.Name))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Name is a required field.");
                return false;
            }

            foreach (var c in this._controller.Model.Name)
            {
                if (!char.IsLetterOrDigit(c) && c != '-')
                {
                    throw new Exception("Name must contain only letters, digits, or hyphens.");
                }
            }

            if (string.IsNullOrEmpty(this._controller.Model.Description))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Description is a required field.");
                return false;
            }
            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.CreateSecurityGroup();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating security group", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating security group: " + e.Message);
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            this._ctlName.Focus();
        }
    }
}
