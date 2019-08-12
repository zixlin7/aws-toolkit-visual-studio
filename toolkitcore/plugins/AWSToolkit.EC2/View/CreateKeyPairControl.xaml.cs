using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for CreateKeyPairControl.xaml
    /// </summary>
    public partial class CreateKeyPairControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateKeyPairControl));

        CreateKeyPairController _controller;

        public CreateKeyPairControl(CreateKeyPairController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title => "Create Key Pair";

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(this._controller.Model.KeyPairName))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Required Field", "Name is required.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                return this._controller.CreateKeyPair();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating key pair", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating key pair: " + e.Message);
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            this._ctlName.Focus();
        }
    }
}
