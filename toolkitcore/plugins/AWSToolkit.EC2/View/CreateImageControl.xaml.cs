using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for CreateImageControl.xaml
    /// </summary>
    public partial class CreateImageControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateImageControl));

        CreateImageController _controller;

        public CreateImageControl(CreateImageController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title => "Create Image";

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(this._controller.Model.Name))
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
                string imageId = this._controller.CreateImage();
                ToolkitFactory.Instance.ShellProvider.ShowMessage("Image Created", string.Format("Image was created with id {0}.", imageId));
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating image", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating image: " + e.Message);
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            this._ctlName.Focus();
        }
    }
}
