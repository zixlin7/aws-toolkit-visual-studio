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
using Amazon.AWSToolkit.CloudFormation.Controllers;
using Amazon.AWSToolkit.CloudFormation.Model;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.View
{
    /// <summary>
    /// Interaction logic for ViewDeploymentLogControl.xaml
    /// </summary>
    public partial class ViewDeploymentLogControl : BaseAWSControl
    {
        ViewDeploymentLogController _controller;

        public ViewDeploymentLogControl(ViewDeploymentLogController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContextChanged += onDataContextChanged;            
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(string.IsNullOrEmpty(this._controller.Model.ErrorMessage))
                this._ctlLoadingStatus.Text = string.Format("Retrieved log from instance {0}", this._controller.Model.InstanceId);
            else
                this._ctlLoadingStatus.Text = string.Format("Error retrieving log: {0}", this._controller.Model.ErrorMessage);
        }

        public override string Title
        {
            get
            {
                return string.Format("Deployment Log for Instance {0}", this._controller.Model.InstanceId);
            }
        }

        public override bool SupportsBackGroundDataLoad
        {
            get
            {
                return true;
            }
        }

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }
    }
}
