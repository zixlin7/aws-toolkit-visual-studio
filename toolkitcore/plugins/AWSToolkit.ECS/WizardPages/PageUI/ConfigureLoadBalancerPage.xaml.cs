using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ECS.WizardPages.PageControllers;
using log4net;
using System;
using System.ComponentModel;
using System.Windows.Controls;

using Task = System.Threading.Tasks.Task;
using Amazon.IdentityManagement.Model;

using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;

using System.Collections.Generic;
using System.Linq;
using static Amazon.AWSToolkit.ECS.WizardPages.ECSWizardUtils;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ConfigureLoadBalancerPage.xaml
    /// </summary>
    public partial class ConfigureLoadBalancerPage : BaseAWSUserControl, INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ECSServicePage));

        public ConfigureLoadBalancerPageController PageController { get; }

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
                if (!this.ShouldConfigureELB)
                    return true;

                if (!this.CreateNewIAMRole && this.ServiceIAMRole == null)
                    return false;

                if (this.CreateNewLoadBalancer && string.IsNullOrEmpty(this.NewLoadBalancerName))
                    return false;
                if (!this.CreateNewLoadBalancer && string.IsNullOrEmpty(this.LoadBalancer))
                    return false;

                if (this.CreateNewListenerPort && !this.NewListenerPort.HasValue)
                    return false;
                if (!this.CreateNewListenerPort && string.IsNullOrEmpty(this.ListenerPort))
                    return false;

                if (this.CreateNewTargetGroup && string.IsNullOrEmpty(this.NewTargetGroupName))
                    return false;
                if (!this.CreateNewTargetGroup && string.IsNullOrEmpty(this.TargetGroup))
                    return false;

                if (string.IsNullOrEmpty(this.PathPattern))
                    return false;
                if (string.IsNullOrEmpty(this.HealthCheckPath))
                    return false;

                return true;
            }
        }

        bool _shouldConfigureELB;
        public bool ShouldConfigureELB
        {
            get => this._shouldConfigureELB;
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

        public Role ServiceIAMRole
        {
            get
            {
                if (this._ctlServiceIAMRole.SelectedIndex == 0)
                    return null;

                return this._ctlServiceIAMRole.SelectedValue as Role;
            }
            set => this._ctlServiceIAMRole.SelectedValue = value;
        }

        public bool EnableServiceIAMRole
        {
            get => this._ctlServiceIAMRole.Visibility == Visibility.Visible;
            set
            {
                Visibility vis = value ? Visibility.Visible : Visibility.Collapsed;
                this._ctlServiceIAMRole.Visibility = vis;
                this._ctlServiceIAMRoleLabel.Visibility = vis;
            }
        }


        private void _ctlServiceIAMRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("ServiceIAMRole");
        }

        public bool CreateNewIAMRole => this.ShouldConfigureELB && this._ctlServiceIAMRole.SelectedIndex == 0;

        public void SetCreateNewIAMRole()
        {
            this._ctlServiceIAMRole.SelectedIndex = 0;
        }

        public string LoadBalancer
        {
            get => this._ctlLoadBalancer.SelectedItem as string;
            set => this._ctlLoadBalancer.SelectedItem = value;
        }

        public string LoadBalancerArn
        {
            get
            {
                if (!this.ShouldConfigureELB || string.IsNullOrEmpty(this.LoadBalancer))
                    return null;

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

        public bool CreateNewLoadBalancer => this._ctlLoadBalancer.SelectedIndex == 0;

        string _newLoadBalancerName;
        public string NewLoadBalancerName
        {
            get => this._newLoadBalancerName;
            set
            {
                this._newLoadBalancerName = value;
                NotifyPropertyChanged("NewLoadBalancerName");
            }
        }

        public string ListenerPort
        {
            get => this._ctlListenerPorts.SelectedItem as string;
            set => this._ctlListenerPorts.SelectedItem = value;
        }

        public string ListenerArn
        {
            get
            {
                if (!this.ShouldConfigureELB || string.IsNullOrEmpty(this.ListenerPort))
                    return null;

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
                this._ctlTargetGroup.IsEnabled = true;
            }
            else
            {
                this._ctlNewListenerPort.Visibility = Visibility.Visible;
                this._ctlNewListenerPort.IsEnabled = true;
                this._ctlTargetGroup.SelectedIndex = 0;
                this._ctlTargetGroup.IsEnabled = false;
            }
        }

        public bool CreateNewListenerPort => this._ctlListenerPorts.SelectedIndex == 0;

        int? _newListenerPort;
        public int? NewListenerPort
        {
            get => this._newListenerPort;
            set
            {
                this._newListenerPort = value;
                NotifyPropertyChanged("NewListenerPort");
            }
        }

        public string TargetGroup
        {
            get => this._ctlTargetGroup.SelectedItem as string;
            set => this._ctlTargetGroup.SelectedItem = value;
        }

        public string TargetGroupArn
        {
            get
            {
                if (!this.ShouldConfigureELB || string.IsNullOrEmpty(this.TargetGroup))
                    return null;

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
            get => this._newTargetGroupName;
            set
            {
                this._newTargetGroupName = value;
                NotifyPropertyChanged("NewTargetGroupName");
            }
        }

        public bool CreateNewTargetGroup => this._ctlTargetGroup.SelectedIndex == 0;

        string _pathPattern;
        public string PathPattern
        {
            get => this._pathPattern;
            set
            {
                this._pathPattern = value;
                NotifyPropertyChanged("PathPattern");
            }
        }

        string _healthCheckPath;
        public string HealthCheckPath
        {
            get => this._healthCheckPath;
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
                if (!this.ShouldConfigureELB || string.IsNullOrEmpty(this.TargetGroup))
                    return false;

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
            this._ctlServiceIAMRole.Items.Add(new Role {RoleName = CREATE_NEW_TEXT });
            this._ctlServiceIAMRole.SelectedIndex = 0;

            this._ctlLoadBalancer.Items.Clear();
            this._ctlLoadBalancer.Items.Add(CREATE_NEW_TEXT);
            this._existingLoadBalancers.Clear();


            System.Threading.Tasks.Task.Run<List<Role>>(() =>
            {
                try
                {
                    return LoadECSRoles(this.PageController.HostingWizard);
                }
                catch(Exception e)
                {
                    this.PageController.HostingWizard.SetPageError("Error listing IAM roles: " + e.Message);
                    return new List<Role>();
                }
            }).ContinueWith(t =>
            {
                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((System.Action)(() =>
                {
                    foreach (var item in t.Result.OrderBy(x => x.RoleName))
                    {
                        this._ctlServiceIAMRole.Items.Add(item);
                    }

                    var previousValue = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] as string;
                    if (!string.IsNullOrWhiteSpace(previousValue) && t.Result.FirstOrDefault(x => string.Equals(x.RoleName, previousValue, StringComparison.Ordinal)) != null)
                    {
                        this._ctlServiceIAMRole.SelectedItem = t.Result.FirstOrDefault(x => string.Equals(x.RoleName, previousValue, StringComparison.Ordinal));
                    }
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
                    try
                    {
                        var response = new DescribeLoadBalancersResponse();
                        do
                        {
                            var request = new DescribeLoadBalancersRequest() { Marker = response.NextMarker };
                            response = client.DescribeLoadBalancers(request);

                            foreach (var loadBalancer in response.LoadBalancers)
                            {
                                if (loadBalancer.Type == LoadBalancerTypeEnum.Application)
                                {
                                    loadBalancers.Add(loadBalancer.LoadBalancerName);
                                    this._existingLoadBalancers[loadBalancer.LoadBalancerName] = loadBalancer;
                                }
                            }
                        } while (!string.IsNullOrEmpty(response.NextMarker));
                    }
                    catch(Exception e)
                    {
                        this.PageController.HostingWizard.SetPageError("Error describing existing load balancers: " + e.Message);
                    }
                    return loadBalancers;
                }
            }).ContinueWith(t =>
            {
                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((System.Action)(() =>
                {
                    foreach (var item in t.Result.OrderBy(x => x))
                    {
                        this._ctlLoadBalancer.Items.Add(item);
                    }
                }));
            });
        }

        Dictionary<string, Listener> _existingListeners = new Dictionary<string, Listener>();
        public void UpdateResourcesForLoadBalancerSelectionChange(string loadBalancer)
        {
            this._ctlListenerPorts.Items.Clear();
            this._ctlListenerPorts.Items.Add(CREATE_NEW_TEXT);
            this._existingListeners.Clear();


            if (string.IsNullOrWhiteSpace(loadBalancer) || string.Equals(loadBalancer, CREATE_NEW_TEXT) || !this._existingLoadBalancers.ContainsKey(loadBalancer))
                return;

            Task.Run<List<string>>(() =>
            {
                using (var client = CreateELBv2Client(this.PageController.HostingWizard))
                {
                    var listenerPorts = new List<string>();
                    try
                    {
                        var response = new DescribeListenersResponse();
                        do
                        {

                            var request = new DescribeListenersRequest() { Marker = response.NextMarker, LoadBalancerArn = this._existingLoadBalancers[loadBalancer].LoadBalancerArn };
                            response = client.DescribeListeners(request);

                            foreach (var listener in response.Listeners)
                            {
                                var token = listener.Port + "(" + listener.Protocol + ")";
                                listenerPorts.Add(token);
                                this._existingListeners[token] = listener;
                            }
                        } while (!string.IsNullOrEmpty(response.NextMarker));
                    }
                    catch(Exception e)
                    {
                        this.PageController.HostingWizard.SetPageError("Error describing existing listeners: " + e.Message);
                    }
                    return listenerPorts;
                }
            }).ContinueWith(t =>
            {
                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((System.Action)(() =>
                {
                    foreach (var item in t.Result.OrderBy(x => x))
                    {
                        this._ctlListenerPorts.Items.Add(item);
                    }
                }));
            });
        }

        Dictionary<string, Rule> _existingRules = new Dictionary<string, Rule>();
        Dictionary<string, ElasticLoadBalancingV2.Model.TargetGroup> _existingTargetGroups = new Dictionary<string, ElasticLoadBalancingV2.Model.TargetGroup>();
        public void UpdateResourcesForListenerSelectionChange(string listener)
        {
            this._ctlTargetGroup.Items.Clear();
            this._ctlTargetGroup.Items.Add(CREATE_NEW_TEXT);
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
                    try
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
                    catch(Exception e)
                    {
                        this.PageController.HostingWizard.SetPageError("Error describing existing ELB target groups: " + e.Message);
                    }

                    var pathPatterns = new List<string>();
                    try
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
                    catch(Exception e)
                    {
                        this.PageController.HostingWizard.SetPageError("Error describing existing rules for ELB listener: " + e.Message);
                    }
                    return new Tuple<List<string>, List<string>>(targetGroups, pathPatterns);
                }
            }).ContinueWith(t =>
            {
                ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((System.Action)(() =>
                {
                    foreach (var item in t.Result.Item1.OrderBy(x => x))
                    {
                        this._ctlTargetGroup.Items.Add(item);
                    }
                }));
            });
        }
    }
}
