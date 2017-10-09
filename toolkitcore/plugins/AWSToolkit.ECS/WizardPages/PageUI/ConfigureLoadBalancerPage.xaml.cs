using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.WizardPages.PageControllers;
using log4net;
using System;
using Amazon.AWSToolkit.Account;
using System.ComponentModel;
using System.Windows.Controls;

using Task = System.Threading.Tasks.Task;

using Amazon.ECS;
using Amazon.ECS.Model;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ConfigureLoadBalancerPage.xaml
    /// </summary>
    public partial class ConfigureLoadBalancerPage : BaseAWSUserControl, INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ECSServicePage));

        public ConfigureLoadBalancerPageController PageController { get; private set; }

        public ConfigureLoadBalancerPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ConfigureLoadBalancerPage(ConfigureLoadBalancerPageController pageController)
            : this()
        {
            PageController = pageController;
        }

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                if (!this._ctlConfigureLoadBalancer.IsChecked.GetValueOrDefault())
                    return true;


                return true;
            }
        }

        public string ServiceIAMRole
        {
            get { return this._ctlServiceIAMRole.Text; }
            set { this._ctlServiceIAMRole.Text = value; }
        }

        private void _ctlServiceIAMRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("ServiceIAMRole");
        }

        public string LoadBalancer
        {
            get { return this._ctlLoadBalancer.Text; }
            set { this._ctlLoadBalancer.Text = value; }
        }

        private void _ctlLoadBalancer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("LoadBalancer");
        }

        string _newLoadBalancerName;
        public string NewLoadBalancerName
        {
            get { return this._newLoadBalancerName; }
            set
            {
                this._newLoadBalancerName = value;
                NotifyPropertyChanged("NewLoadBalancerName");
            }
        }

        public string ListenerPorts
        {
            get { return this._ctlListenerPorts.Text; }
            set { this._ctlListenerPorts.Text = value; }
        }

        private void _ctlListenerPorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("ListenerPorts");
        }

        int? _newListenerPort;
        public int? NewListenerPort
        {
            get { return this._newListenerPort; }
            set
            {
                this._newListenerPort = value;
                NotifyPropertyChanged("NewListenerPort");
            }
        }

        public string TargetGroup
        {
            get { return this._ctlTargetGroup.Text; }
            set { this._ctlTargetGroup.Text = value; }
        }

        private void _ctlTargetGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("TargetGroup");
        }

        string _newTargetGroupName;
        public string NewTargetGroupName
        {
            get { return this._newTargetGroupName; }
            set
            {
                this._newTargetGroupName = value;
                NotifyPropertyChanged("NewTargetGroupName");
            }
        }

        string _pathPattern;
        public string PathPattern
        {
            get { return this._pathPattern; }
            set
            {
                this._pathPattern = value;
                NotifyPropertyChanged("PathPattern");
            }
        }

        string _healthCheckPath;
        public string HealthCheckPath
        {
            get { return this._healthCheckPath; }
            set
            {
                this._healthCheckPath = value;
                NotifyPropertyChanged("HealthCheckPath");
            }
        }
    }
}
