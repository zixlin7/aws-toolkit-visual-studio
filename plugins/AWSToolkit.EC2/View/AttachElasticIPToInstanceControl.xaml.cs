using System;
using System.Collections.Generic;
using System.Linq;
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
using Amazon.AWSToolkit.Util;

using Amazon.EC2;
using Amazon.EC2.Model;
using AMIImage = Amazon.EC2.Model.Image;

using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for AttachElasticIPToInstance.xaml
    /// </summary>
    public partial class AttachElasticIPToInstanceControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(AttachVolumeControl));

        AttachElasticIPToInstanceController _controller;

        public AttachElasticIPToInstanceControl(AttachElasticIPToInstanceController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title
        {
            get { return "Attach Elastic IP to Instance"; }
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.Attach();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error attaching Elastic IP to instance", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error attaching Elastic IP to instance: " + e.Message);
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            if (this._ctlElasticIPs.Items.Count == 0)
            {
                this._ctlSelectExisting.IsEnabled = false;
                this._controller.Model.ActionCreateNewAddress = true;
            }
            else
            {
                this._ctlElasticIPs.SelectedIndex = 0;
                this._controller.Model.ActionSelectedAddress = true;
            }
        }
    }
}
