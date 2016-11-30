using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public override string Title
        {
            get { return "Create Image";}
        }

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
