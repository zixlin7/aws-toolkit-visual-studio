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
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.Controllers;
using Amazon.AWSToolkit.CloudFormation.View.Components.ResourceDetails;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.View.Components
{
    /// <summary>
    /// Interaction logic for StackResourceDetailHostControl.xaml
    /// </summary>
    public partial class StackResourceDetailHostControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(StackResourceDetailHostControl));
        ResourceWrapper _resource;

        public StackResourceDetailHostControl()
        {
            InitializeComponent();
            this.Height = 0;
            this.DataContextChanged += onDataContextChanged;
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._resource = this.DataContext as ResourceWrapper;
            if (this._resource == null)
                return;

            
            this._resource.PropertyChanged += onPropertyChanged;
            if (this._resource.ResourceStatus == CloudFormationConstants.CreateCompleteStatus)
            {
                buildResourceDetailControl();
            }
        }

        void onPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, "Status") && this._resource.ResourceStatus == CloudFormationConstants.CreateCompleteStatus)
                buildResourceDetailControl();
            else
                this.Height = 0;
        }

        void buildResourceDetailControl()
        {
            try
            {
                var mainWindow = UIUtils.FindVisualParent<ViewStackControl>(this);
                if (mainWindow == null)
                {
                    LOGGER.Error("Can not find parent ViewStackControl");
                    return;
                }

                object dataContext = null;
                UserControl widget = null;

                switch(this._resource.ResourceType)
                {
                    case "AWS::AutoScaling::AutoScalingGroup":
                        dataContext = mainWindow.Controller.GetAutoScalingGroupDetails(this._resource.PhysicalResourceId);
                        widget = new AutoScalingGroupDetails();
                        break;
                    case "AWS::ElasticLoadBalancing::LoadBalancer":
                        dataContext = mainWindow.Controller.GetLoadBalancerDetails(this._resource.PhysicalResourceId);
                        widget = new LoadBalancerDetails();
                        break;
                    case "AWS::RDS::DBInstance":
                        dataContext = mainWindow.Controller.GetRDSInstanceDetails(this._resource.PhysicalResourceId);
                        widget = new RDSInstanceDetails();
                        break;
                    case "AWS::EC2::Instance":
                        dataContext = mainWindow.Controller.GetInstanceDetails(this._resource.PhysicalResourceId);
                        widget = new InstanceDetails();
                        break;
                }

                if (dataContext == null || widget == null)
                {
                    this.Height = 0;
                    return;
                }

                this.Height = double.NaN;
                widget.DataContext = dataContext;
                this._ctlMainPanel.Children.Add(widget);
            }
            catch (Exception e)
            {
                LOGGER.Error(string.Format("Error loading details for type {0}", this._resource.ResourceType), e);
            }
        }
    }
}
