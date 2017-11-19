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

using static Amazon.AWSToolkit.ECS.WizardPages.ECSWizardUtils;
using Amazon.AWSToolkit.EC2.Model;
using System.Collections.ObjectModel;
using Amazon.EC2.Model;
using System.Windows.Input;
using Amazon.AWSToolkit.EC2.Workers;
using Amazon.AWSToolkit.SimpleWorkers;
using System.Windows.Data;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ECSClusterPage.xaml
    /// </summary>
    public partial class ECSClusterPage : BaseAWSUserControl, INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ECSClusterPage));

        public ECSClusterPageController PageController { get; private set; }

        public ECSClusterPage()
        {
            AvailableSecurityGroups = new ObservableCollection<SecurityGroupWrapper>();
            AvailableVpcSubnets = new ObservableCollection<VpcAndSubnetWrapper>();

            InitializeComponent();
            DataContext = this;

            _ctlSubnetsPicker.PropertyChanged += ForwardEmbeddedControlPropertyChanged;
            _ctlSubnetsPicker.PropertyChanged += _ctlSubnetsPicker_PropertyChanged;
        }

        public ECSClusterPage(ECSClusterPageController pageController)
            : this()
        {
            PageController = pageController;

            UpdateExistingResources();


            this._ctlLaunchTypePicker.Items.Add(Amazon.ECS.LaunchType.FARGATE);
            this._ctlLaunchTypePicker.Items.Add(Amazon.ECS.LaunchType.EC2);
            this._ctlLaunchTypePicker.SelectedIndex = 0;

            string previousLaunchType = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.LaunchType] as string;
            if(!string.IsNullOrEmpty(previousLaunchType))
            {
                this._ctlLaunchTypePicker.SelectedItem = Amazon.ECS.LaunchType.FindValue(previousLaunchType);
            }
        }

        public void PageActivated()
        {
            if (this.PageController.DeploymentMode.Value == Constants.DeployMode.ScheduleTask)
            {
                this._ctlLaunchTypePicker.SelectedIndex = 1;
                this._ctlLaunchTypePicker.IsEnabled = false;
            }
            else
            {
                this._ctlLaunchTypePicker.IsEnabled = true;
            }
        }

        private void _ctlSubnetsPicker_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var subnets = SelectedSubnets;
            string vpcId = null;
            if (subnets != null)
            {
                // relying here that the page has rejected attempt to select subnets
                // across multiple vpcs
                foreach (var subnet in subnets)
                {
                    vpcId = subnet.VpcId;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(vpcId))
                LoadExistingSecurityGroups(vpcId);
            else
                SetAvailableSecurityGroups(null, null);
        }

        private void ForwardEmbeddedControlPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
        }

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                if (this.CreateNewCluster && string.IsNullOrEmpty(this.NewClusterName))
                    return false;
                if (!this.CreateNewCluster && string.IsNullOrEmpty(this.Cluster))
                    return false;

                if (this.LaunchType == LaunchType.FARGATE)
                {
                    if (string.IsNullOrEmpty(this.TaskCPU))
                        return false;
                    if (string.IsNullOrEmpty(this.TaskMemory))
                        return false;

                    if (_ctlSubnetsPicker.SubnetsSpanVPCs)
                        return false;

                    if (this.SelectedSubnets.Count() == 0)
                        return false;
                    if (string.IsNullOrEmpty(this.SecurityGroup))
                        return false;
                }


                return true;
            }
        }

        private void UpdateExistingResources()
        {
            this._ctlClusterPicker.Items.Clear();
            this._ctlClusterPicker.Items.Add("Create an empty cluster");

            try
            {
                var account = PageController.HostingWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
                var region = PageController.HostingWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

                new QueryVpcsAndSubnetsWorker(ECSWizardUtils.CreateEC2Client(PageController.HostingWizard), LOGGER, 
                    OnVpcSubnetsAvailable, OnVpcSubnetsError);

                Task task1 = Task.Run(() =>
                {
                    var items = new List<string>();
                    using (var ecsClient = account.CreateServiceClient<AmazonECSClient>(region.GetEndpoint(RegionEndPointsManager.ECS_ENDPOINT_LOOKUP)))
                    {
                        var response = new ListClustersResponse();
                        do
                        {
                            var request = new ListClustersRequest() { NextToken = response.NextToken };

                            try
                            {
                                response = ecsClient.ListClusters(request);
                            }
                            catch(Exception e)
                            {
                                this.PageController.HostingWizard.SetPageError("Error listing existing clusters: " + e.Message);
                                throw;
                            }

                            foreach (var arn in response.ClusterArns)
                            {
                                var name = arn.Substring(arn.IndexOf('/') + 1);
                                items.Add(name);
                            }
                        } while (!string.IsNullOrEmpty(response.NextToken));
                    }

                    ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                    {
                        foreach (var cluster in items.OrderBy(x => x))
                        {
                            this._ctlClusterPicker.Items.Add(cluster);
                        }


                        string previousValue = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.ClusterName] as string;
                        if (!string.IsNullOrWhiteSpace(previousValue) && items.Contains(previousValue))
                        {
                            this._ctlClusterPicker.SelectedItem = previousValue;
                        }
                        // If they have no clusters preselect create new
                        else if (this._ctlClusterPicker.Items.Count == 1)
                        {
                            this._ctlClusterPicker.SelectedIndex = 0;
                        }
                        // If they only have one cluster select that cluster
                        else if (this._ctlClusterPicker.Items.Count == 2)
                        {
                            this._ctlClusterPicker.SelectedIndex = 1;
                        }
                    }));
                });
            }
            catch (Exception e)
            {
                this.PageController.HostingWizard.SetPageError("Error refreshing existing ECS Clusters: " + e.Message);
                LOGGER.Error("Error refreshing existing ECS Clusters.", e);
            }
        }


        public string Cluster
        {
            get { return this._ctlClusterPicker.SelectedValue as string; }
            set { this._ctlClusterPicker.SelectedValue = value; }
        }

        public bool CreateNewCluster
        {
            get { return this._ctlClusterPicker.SelectedIndex == 0; }
        }

        string _newClusterName;
        public string NewClusterName
        {
            get { return _newClusterName; }
            set
            {
                _newClusterName = value;
                NotifyPropertyChanged("NewClusterName");
            }
        }

        private void _ctlClusterPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.CreateNewCluster)
            {
                this._ctlNewClusterName.Visibility = Visibility.Visible;
                this._ctNewClusterDescription.Visibility = Visibility.Visible;
                this._ctlNewClusterName.IsEnabled = true;
                this._ctlNewClusterName.Focus();
                this._ctlLaunchTypePicker.SelectedIndex = 0;
                this._ctlLaunchTypePicker.IsEnabled = false;
            }
            else
            {
                this._ctlNewClusterName.Visibility = Visibility.Collapsed;
                this._ctNewClusterDescription.Visibility = Visibility.Collapsed;
                this._ctlNewClusterName.IsEnabled = false;
                this._ctlLaunchTypePicker.IsEnabled = !this.PageController.DeploymentMode.HasValue || this.PageController.DeploymentMode.Value != Constants.DeployMode.ScheduleTask;
            }

            NotifyPropertyChanged("Cluster");
        }

        public Amazon.ECS.LaunchType LaunchType
        {
            get { return this._ctlLaunchTypePicker.SelectedItem as Amazon.ECS.LaunchType; }
        }


        private void _ctlLaunchTypePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var isFargateLaunch = this._ctlLaunchTypePicker.SelectedIndex == 0;
            this._ctlTaskCPU.IsEnabled = isFargateLaunch;
            this._ctlTaskMemory.IsEnabled = isFargateLaunch;
            this._ctlSubnetsPicker.IsEnabled = isFargateLaunch;
            this._ctlSecurityGroup.IsEnabled = isFargateLaunch;
            this._ctlAssignPublicIp.IsEnabled = isFargateLaunch;

            if (isFargateLaunch)
            {
                this._ctlLaunchTypeDescription.Text = "FARGATE will automatically provision the necessary compute capacity needed to run the application based on the CPU and Memory settings. " +
                                                      "This removes the need to add any EC2 instances to your cluster.";
                this._ctlTaskCPU.ItemsSource = TaskCPUAllowedValues;

                var previousCPU = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.AllocatedTaskCPU] as string;
                if (!string.IsNullOrEmpty(previousCPU))
                {
                    var item = TaskCPUAllowedValues.FirstOrDefault(x => string.Equals(x.SystemName, previousCPU, StringComparison.Ordinal));
                    if (item != null)
                    {
                        this._ctlTaskCPU.SelectedItem = item;
                    }
                }
                else
                {
                    this._ctlTaskCPU.SelectedIndex = 0;
                }

                if (this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.AssignPublicIpAddress] is bool)
                {
                    var previousAssign = (bool)this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.AssignPublicIpAddress];
                    this.AssignPublicIpAddress = previousAssign;
                }
                
            }
            else
            {
                this._ctlLaunchTypeDescription.Text = "With the EC2 launch type, the application will run on the registered container instances for the cluster.";
                this._ctlTaskCPU.ItemsSource = new TaskCPUItemValue[0];
                this._ctlTaskMemory.Items.Clear();
            }
        }

        public string TaskCPU
        {
            get
            {
                var item = this._ctlTaskCPU.SelectedValue as TaskCPUItemValue;
                if (item == null)
                    return null;

                return item.SystemName;
            }
        }

        private void _ctlTaskCPU_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = this._ctlTaskCPU.SelectedItem as TaskCPUItemValue;
            if(item != null)
            {
                this._ctlTaskMemory.Items.Clear();
                foreach(var memory in item.MemoryOptions)
                {
                    this._ctlTaskMemory.Items.Add(memory);
                }

                var previous = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.AllocatedTaskMemory] as string;
                if (!string.IsNullOrEmpty(previous))
                {
                    var cpu = this._ctlTaskCPU.SelectedItem as TaskCPUItemValue;
                    if(cpu != null)
                    {
                        var itemMemory = cpu.MemoryOptions.FirstOrDefault(x => string.Equals(x.SystemName, previous, StringComparison.Ordinal));
                        if (itemMemory != null)
                        {
                            this._ctlTaskMemory.SelectedItem = itemMemory;
                        }
                    }
                }

                if(this._ctlTaskMemory.SelectedItem == null)
                {
                    this._ctlTaskMemory.SelectedIndex = 0;
                }
            }
            else
            {
                this._ctlTaskMemory.Items.Clear();
            }

            NotifyPropertyChanged("TaskCPU");
        }

        public string TaskMemory
        {
            get
            {
                var item = this._ctlTaskMemory.SelectedValue as MemoryOption;
                if (item == null)
                    return null;

                return item.SystemName;
            }
        }

        private void _ctlTaskMemory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("TaskMemory");
        }

        private void _ctlSecurityGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("SecurityGroup");
        }

        public ObservableCollection<SecurityGroupWrapper> AvailableSecurityGroups { get; private set; }

        void LoadExistingSecurityGroups(string vpcId)
        {
            new QuerySecurityGroupsWorker(CreateEC2Client(PageController.HostingWizard),
                                          vpcId,
                                          LOGGER,
                                          OnSecurityGroupsAvailable,
                                          OnSecurityGroupsError);
        }

        void OnSecurityGroupsAvailable(ICollection<SecurityGroup> securityGroups)
        {
            string previousSecurityGroup = null;
            var previousSecurityGroups = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.LaunchSecurityGroups] as string[];
            if (previousSecurityGroups != null && previousSecurityGroups.Length == 1)
            {
                previousSecurityGroup = previousSecurityGroups[0];
            }

            SetAvailableSecurityGroups(securityGroups, previousSecurityGroup);
        }

        void OnSecurityGroupsError(Exception e)
        {
            this.PageController.HostingWizard.SetPageError("Error describing existing security groups: " + e.Message);
            SetAvailableSecurityGroups(null, null);
        }


        public void SetAvailableSecurityGroups(ICollection<SecurityGroup> existingGroups, string autoSelectGroup)
        {
            this._ctlSecurityGroup.Items.Clear();
            this._ctlSecurityGroup.Items.Add(CREATE_NEW_TEXT);

            if (existingGroups != null)
            {
                string selectedItem = null;
                foreach (var group in existingGroups)
                {
                    var item = string.Format("{0} ({1})", group.GroupId, group.GroupName);
                    if (string.Equals(group.GroupId, autoSelectGroup, StringComparison.Ordinal))
                    {
                        selectedItem = item;
                    }

                    this._ctlSecurityGroup.Items.Add(item);
                }

                if (!string.IsNullOrEmpty(selectedItem))
                {
                    this._ctlSecurityGroup.SelectedItem = selectedItem;
                }
            }
        }

        void OnVpcSubnetsAvailable(ICollection<Vpc> vpcs, ICollection<Subnet> subnets)
        {
            var previousSubnets = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.LaunchSubnets] as string[];
            var defaultSelection = _ctlSubnetsPicker.SetAvailableVpcSubnets(vpcs, subnets, previousSubnets);
            if (defaultSelection == null)
            {
                this._ctlSecurityGroup.Items.Clear();
            }
        }

        void OnVpcSubnetsError(Exception e)
        {
            this.PageController.HostingWizard.SetPageError("Error describing existing subnets: " + e.Message);
            _ctlSubnetsPicker.SetAvailableVpcSubnets(new Vpc[0], new Subnet[0], new string[0]);
        }

        public ObservableCollection<VpcAndSubnetWrapper> AvailableVpcSubnets { get; private set; }

        public IEnumerable<SubnetWrapper> SelectedSubnets
        {
            get
            {
                return _ctlSubnetsPicker.SelectedSubnets;
            }
        }

        public string SecurityGroup
        {
            get
            {
                if (this.CreateNewSecurityGroup)
                    return null;

                var groupLabel = this._ctlSecurityGroup.SelectedValue as string;
                if (groupLabel == null)
                    return null;

                int pos = groupLabel.IndexOf('(');
                if (pos == -1)
                    return groupLabel;

                return groupLabel.Substring(0, pos).Trim();
            }
        }

        public bool CreateNewSecurityGroup
        {
            get { return this._ctlSecurityGroup.SelectedIndex == 0; }
        }

        private bool _assignPublicIpAddress;
        public bool AssignPublicIpAddress
        {
            get { return _assignPublicIpAddress; }
            set { this._assignPublicIpAddress = value; }
        }
    }
}
