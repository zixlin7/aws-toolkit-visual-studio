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

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;

using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using static Amazon.AWSToolkit.ECS.WizardPages.ECSWizardUtils;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ConfigureLoadBalancerPage.xaml
    /// </summary>
    public partial class ConfigureLoadBalancerPage : BaseAWSUserControl, INotifyPropertyChanged
    {
        const string CREATE_NEW_TEXT = "Create New";

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

        public void InitializeForNewService()
        {
            if(this._ctlConfigureLoadBalancer.IsChecked.GetValueOrDefault())
                UpdateExistingResources();
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

        bool _shouldConfigureELB;
        public bool ShouldConfigureELB
        {
            get { return this._shouldConfigureELB; }
            set
            {
                this._shouldConfigureELB = value;
                NotifyPropertyChanged("ShouldConfigureELB");
            }
        }

        private void _ctlConfigureLoadBalancer_Click(object sender, RoutedEventArgs e)
        {
            if (this._ctlConfigureLoadBalancer.IsChecked.GetValueOrDefault())
                UpdateExistingResources();
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

        public bool CreateNewIAMRole
        {
            get { return this._ctlServiceIAMRole.SelectedIndex == 0; }
        }

        public string LoadBalancer
        {
            get { return this._ctlLoadBalancer.Text; }
            set { this._ctlLoadBalancer.Text = value; }
        }

        public string LoadBalancerArn
        {
            get
            {
                Amazon.ElasticLoadBalancingV2.Model.LoadBalancer loadBalancer;
                if (this._existingLoadBalancers.TryGetValue(this.LoadBalancer, out loadBalancer))
                    return loadBalancer.LoadBalancerArn;

                return null;
            }
        }

        private void _ctlLoadBalancer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("LoadBalancer");
            string loadBalancer = null;
            if (e.AddedItems.Count == 1)
                loadBalancer = e.AddedItems[0] as string;

            UpdateResourcesForLoadBalancerSelectionChange(loadBalancer);

            if (this._ctlLoadBalancer.SelectedIndex != 0)
            {
                this._ctlNewLoadBalancerName.Visibility = Visibility.Collapsed;
                this._ctlNewLoadBalancerName.IsEnabled = false;
                this._ctlNewLoadBalancerName.Text = PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string;

                this._ctlNewLoadBalancerName.IsEnabled = true;
                this._ctlListenerPorts.IsEnabled = true;
            }
            else
            {
                this._ctlNewLoadBalancerName.Visibility = Visibility.Visible;
                this._ctlNewLoadBalancerName.IsEnabled = true;
                this._ctlNewLoadBalancerName.Text = PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string;

                this._ctlListenerPorts.SelectedIndex = 0;
                this._ctlListenerPorts.IsEnabled = false;
                this._ctlNewListenerPort.Text = "80";

                this._ctlTargetGroup.SelectedIndex = 0;
                this._ctlTargetGroup.IsEnabled = false;
                this._ctlNewTargetGroupName.Text = PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string;
            }
        }

        public bool CreateNewLoadBalancer
        {
            get { return this._ctlLoadBalancer.SelectedIndex == 0; }
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

        public string ListenerPort
        {
            get { return this._ctlListenerPorts.Text; }
            set { this._ctlListenerPorts.Text = value; }
        }

        public string ListenerArn
        {
            get
            {
                Amazon.ElasticLoadBalancingV2.Model.Listener listener;
                if (this._existingListeners.TryGetValue(this.ListenerPort, out listener))
                    return listener.ListenerArn;

                return null;
            }
        }

        private void _ctlListenerPorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("ListenerPorts");

            string listener = null;
            if (e.AddedItems.Count == 1)
                listener = e.AddedItems[0] as string;

            UpdateResourcesForListenerSelectionChange(listener);

            if (this._ctlListenerPorts.SelectedIndex != 0)
            {
                this._ctlNewListenerPort.Visibility = Visibility.Collapsed;
                this._ctlNewListenerPort.IsEnabled = false;
            }
            else
            {
                this._ctlNewListenerPort.Visibility = Visibility.Visible;
                this._ctlNewListenerPort.IsEnabled = true;
                this._ctlTargetGroup.SelectedIndex = 0;
            }
        }

        public bool CreateNewListenerPort
        {
            get { return this._ctlListenerPorts.SelectedIndex == 0; }
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

        public string TargetGroupArn
        {
            get
            {
                Amazon.ElasticLoadBalancingV2.Model.TargetGroup targetGroup;
                if (this._existingTargetGroups.TryGetValue(this.TargetGroup, out targetGroup))
                    return targetGroup.TargetGroupArn;

                return null;
            }
        }

        private void _ctlTargetGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("TargetGroup");
            string targetGroup = null;
            if (e.AddedItems.Count == 1)
                targetGroup = e.AddedItems[0] as string;

            TargetGroup target;
            if(targetGroup != null && this._existingTargetGroups.TryGetValue(targetGroup, out target))
            {
                this._ctlNewTargetGroupName.Visibility = Visibility.Collapsed;
                this._ctlNewTargetGroupName.IsEnabled = false;

                this._ctlPathPattern.IsEnabled = false;

                this.HealthCheckPath = target.HealthCheckPath;

                foreach(var kvp in this._existingRules)
                {
                    if(string.Equals(kvp.Value.Actions[0].TargetGroupArn, target.TargetGroupArn))
                    {
                        this._ctlPathPattern.Text = kvp.Key;
                        break;
                    }
                }
            }
            else
            {
                this._ctlNewTargetGroupName.Visibility = Visibility.Visible;
                this._ctlNewTargetGroupName.IsEnabled = true;

                this._ctlPathPattern.IsEnabled = true;
                this._ctlPathPattern.Text = this._existingRules.ContainsKey("/") ? "" : "/";
                this.HealthCheckPath = this._ctlPathPattern.Text;
            }
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

        public bool CreateNewTargetGroup
        {
            get { return this._ctlTargetGroup.SelectedIndex == 0; }
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

        public string ExistingPathPatterns
        {
            get { return this._ctlListenerPorts.Text; }
            set { this._ctlListenerPorts.Text = value; }
        }

        private void _ctlExistingPathPatterns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("ExistingPathPatterns");
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

        public bool IsHealthCheckPathChanged
        {
            get
            {
                if (this.CreateNewTargetGroup)
                    return true;

                if (!this._existingTargetGroups.ContainsKey(this.TargetGroup))
                    return true;

                return !string.Equals(this.HealthCheckPath, this._existingTargetGroups[this.TargetGroup].HealthCheckPath);
            }
        }

        Dictionary<string, Amazon.ElasticLoadBalancingV2.Model.LoadBalancer> _existingLoadBalancers = new Dictionary<string, Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>();
        private void UpdateExistingResources()
        {
            this._ctlServiceIAMRole.Items.Clear();
            this._ctlServiceIAMRole.Items.Add(CREATE_NEW_TEXT);
            this._ctlServiceIAMRole.SelectedIndex = 0;

            this._ctlLoadBalancer.Items.Clear();
            this._ctlLoadBalancer.Items.Add(CREATE_NEW_TEXT);
//            this._ctlLoadBalancer.SelectedIndex = 0;
            this._existingLoadBalancers.Clear();

            Task.Run<List<string>>(() =>
            {
                using (var client = CreateIAMClient(this.PageController.HostingWizard))
                {
                    var roles = new List<string>();
                    var response = new ListRolesResponse();
                    do
                    {
                        var request = new ListRolesRequest() { Marker = response.Marker };
                        response = client.ListRoles(request);

                        var validRoles = RolePolicyFilter.FilterByAssumeRoleServicePrincipal(response.Roles, "ecs.amazonaws.com");
                        foreach (var role in validRoles)
                        {
                            roles.Add(role.RoleName);
                        }
                    } while (!string.IsNullOrEmpty(response.Marker));
                    return roles;
                }
            }).ContinueWith(t =>
            { 
                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((System.Action)(() =>
                {
                    foreach (var item in t.Result.OrderBy(x => x))
                    {
                        this._ctlServiceIAMRole.Items.Add(item);
                    }

                    var previousValue = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] as string;
                    if (!string.IsNullOrWhiteSpace(previousValue) && t.Result.Contains(previousValue))
                        this._ctlServiceIAMRole.SelectedItem = previousValue;
                    else
                    {
                        this._ctlServiceIAMRole.SelectedIndex = this._ctlServiceIAMRole.Items.Count > 1 ? 1 : 0;
                    }
                }));
            });

            Task.Run<List<string>>(() =>
            {
                using (var client = CreateELBv2Client(this.PageController.HostingWizard))
                {
                    var loadBalancers = new List<string>();
                    var response = new DescribeLoadBalancersResponse();
                    do
                    {
                        var request = new DescribeLoadBalancersRequest() { Marker = response.NextMarker };
                        response = client.DescribeLoadBalancers(request);

                        foreach (var loadBalancer in response.LoadBalancers)
                        {
                            if(loadBalancer.Type == LoadBalancerTypeEnum.Application)
                            {
                                loadBalancers.Add(loadBalancer.LoadBalancerName);
                                this._existingLoadBalancers[loadBalancer.LoadBalancerName] = loadBalancer;
                            }
                        }
                    } while (!string.IsNullOrEmpty(response.NextMarker));
                    return loadBalancers;
                }
            }).ContinueWith(t =>
            {
                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((System.Action)(() =>
                {
                    foreach (var item in t.Result.OrderBy(x => x))
                    {
                        this._ctlLoadBalancer.Items.Add(item);
                    }

//                    this._ctlLoadBalancer.SelectedIndex = this._ctlLoadBalancer.Items.Count > 1 ? 1 : 0;
                }));
            });
        }

        Dictionary<string, Listener> _existingListeners = new Dictionary<string, Listener>();
        public void UpdateResourcesForLoadBalancerSelectionChange(string loadBalancer)
        {
            this._ctlListenerPorts.Items.Clear();
            this._ctlListenerPorts.Items.Add(CREATE_NEW_TEXT);
//            this._ctlListenerPorts.SelectedIndex = 0;
            this._existingListeners.Clear();


            if (string.IsNullOrWhiteSpace(loadBalancer) || string.Equals(loadBalancer, CREATE_NEW_TEXT) || !this._existingLoadBalancers.ContainsKey(loadBalancer))
                return;

            Task.Run<List<string>>(() =>
            {
                using (var client = CreateELBv2Client(this.PageController.HostingWizard))
                {
                    var listenerPorts = new List<string>();
                    var response = new DescribeListenersResponse();
                    do
                    {

                        var request = new DescribeListenersRequest() { Marker = response.NextMarker, LoadBalancerArn = this._existingLoadBalancers[loadBalancer].LoadBalancerArn };
                        response = client.DescribeListeners(request);

                        foreach (var listener in response.Listeners)
                        {
                            var token = $"{listener.Port} ({listener.Protocol})";
                            listenerPorts.Add(token);
                            this._existingListeners[token] = listener;
                        }
                    } while (!string.IsNullOrEmpty(response.NextMarker));
                    return listenerPorts;
                }
            }).ContinueWith(t =>
            {
                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((System.Action)(() =>
                {
                    foreach (var item in t.Result.OrderBy(x => x))
                    {
                        this._ctlListenerPorts.Items.Add(item);
                    }

//                    this._ctlListenerPorts.SelectedIndex = this._ctlListenerPorts.Items.Count > 1 ? 1 : 0;
                }));
            });
        }

        Dictionary<string, Rule> _existingRules = new Dictionary<string, Rule>();
        Dictionary<string, ElasticLoadBalancingV2.Model.TargetGroup> _existingTargetGroups = new Dictionary<string, ElasticLoadBalancingV2.Model.TargetGroup>();
        public void UpdateResourcesForListenerSelectionChange(string listener)
        {
            this._ctlTargetGroup.Items.Clear();
            this._ctlTargetGroup.Items.Add(CREATE_NEW_TEXT);
//            this._ctlTargetGroup.SelectedIndex = 0;
            this._existingTargetGroups.Clear();
            this._existingRules.Clear();


            if (string.IsNullOrWhiteSpace(listener) || string.Equals(listener, CREATE_NEW_TEXT) || !this._existingListeners.ContainsKey(listener))
                return;

            string loadBalancerArn =  this._existingLoadBalancers[this.LoadBalancer].LoadBalancerArn;
            Task.Run<Tuple<List<string>, List<string>>>(() =>
            {
                using (var client = CreateELBv2Client(this.PageController.HostingWizard))
                {
                    var targetGroups = new List<string>();
                    {
                        var response = new DescribeTargetGroupsResponse();
                        do
                        {
                            var request = new DescribeTargetGroupsRequest() { Marker = response.NextMarker, LoadBalancerArn = loadBalancerArn };
                            response = client.DescribeTargetGroups(request);

                            foreach (var targetGroup in response.TargetGroups)
                            {
                                targetGroups.Add(targetGroup.TargetGroupName);
                                _existingTargetGroups[targetGroup.TargetGroupName] = targetGroup;
                            }
                        } while (!string.IsNullOrEmpty(response.NextMarker));
                    }

                    var pathPatterns = new List<string>();
                    {
                        var response = new DescribeRulesResponse();
                        do
                        {
                            var request = new DescribeRulesRequest { ListenerArn = this._existingListeners[listener].ListenerArn, Marker = response.NextMarker };
                            response = client.DescribeRules(request);
                            foreach (var rule in response.Rules)
                            {
                                string pathPattern;
                                if (rule.Conditions == null || rule.Conditions.Count == 0)
                                {
                                    pathPattern = "/";
                                }
                                else if (rule.Conditions[0].Values != null && rule.Conditions[0].Values.Count == 1 && string.Equals(rule.Conditions[0].Field, "path-pattern"))
                                {
                                    pathPattern = rule.Conditions[0].Values[0];
                                }
                                else
                                {
                                    continue;
                                }

                                pathPatterns.Add(pathPattern);
                                this._existingRules[pathPattern] = rule;
                            }

                        } while (!string.IsNullOrEmpty(response.NextMarker));
                    }
                    return new Tuple<List<string>, List<string>>(targetGroups, pathPatterns);
                }
            }).ContinueWith(t =>
            {
                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((System.Action)(() =>
                {
                    foreach (var item in t.Result.Item1.OrderBy(x => x))
                    {
                        this._ctlTargetGroup.Items.Add(item);
                    }

//                    this._ctlTargetGroup.SelectedIndex = this._ctlTargetGroup.Items.Count > 1 ? 1 : 0;
                }));
            });
        }
    }
}
