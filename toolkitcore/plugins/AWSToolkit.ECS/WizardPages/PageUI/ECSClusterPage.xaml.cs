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

        public ECSClusterPage(ECSClusterPageController pageController)
            : this()
        {
            PageController = pageController;

            UpdateExistingResources();
            LoadPreviousValues(PageController.HostingWizard);

            this._ctlLaunchTypePicker.Items.Add(Amazon.ECS.LaunchType.FARGATE);
            this._ctlLaunchTypePicker.Items.Add(Amazon.ECS.LaunchType.EC2);
            this._ctlLaunchTypePicker.SelectedIndex = 0;

        }

        private void ForwardEmbeddedControlPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
        }

        private void LoadPreviousValues(IAWSWizard hostWizard)
        {
        }

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.Cluster))
                    return false;

                //if (_ctlSubnetsPicker.SubnetsSpanVPCs)
                //    return false;

                return true;
            }
        }

        private void UpdateExistingResources()
        {
            var currentTextValue = !string.IsNullOrWhiteSpace(this._ctlClusterPicker.Text) && 
                this._ctlClusterPicker.SelectedValue == null ? this._ctlClusterPicker.Text : null;
            this._ctlClusterPicker.Items.Clear();
            this._ctlClusterPicker.Items.Add(CREATE_NEW_TEXT);

            try
            {
                var account = PageController.HostingWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
                var region = PageController.HostingWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

                new QueryVpcsAndSubnetsWorker(ECSWizardUtils.CreateEC2Client(PageController.HostingWizard), LOGGER, new QueryVpcsAndSubnetsWorker.DataAvailableCallback(OnVpcSubnetsAvailable));

                Task task1 = Task.Run(() =>
                {
                    var items = new List<string>();
                    using (var ecsClient = account.CreateServiceClient<AmazonECSClient>(region.GetEndpoint(RegionEndPointsManager.ECS_ENDPOINT_LOOKUP)))
                    {
                        var response = new ListClustersResponse();
                        do
                        {
                            var request = new ListClustersRequest() { NextToken = response.NextToken };

                            response = ecsClient.ListClusters(request);

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


                        string previousValue = null; // TODO  this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.Cluster] as string;
                        if (!string.IsNullOrWhiteSpace(previousValue) && items.Contains(previousValue))
                            this._ctlClusterPicker.SelectedItem = previousValue;
                        else
                        {
                            if (currentTextValue != null)
                                this._ctlClusterPicker.Text = currentTextValue;
                            else
                                this._ctlClusterPicker.Text = "";
                        }
                    }));
                });
            }
            catch (Exception e)
            {
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
                this._ctlNewClusterName.IsEnabled = true;
                this._ctlNewClusterName.Focus();
                this._ctlLaunchTypePicker.SelectedIndex = 0;
                this._ctlLaunchTypePicker.IsEnabled = false;
            }
            else
            {
                this._ctlNewClusterName.Visibility = Visibility.Collapsed;
                this._ctlNewClusterName.IsEnabled = false;
                this._ctlLaunchTypePicker.IsEnabled = true;
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
            this._ctlSecurityGroup.IsEnabled = isFargateLaunch;

            if(isFargateLaunch)
            {
                this._ctlTaskCPU.ItemsSource = TaskCPUAllowedValues;
                this._ctlTaskCPU.SelectedIndex = 0;
            }
            else
            {
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
                this._ctlTaskMemory.SelectedIndex = 0;
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

        }

        private void _ctlSubnetsPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count > 0)
            {
                var subnet = e.AddedItems[0] as VpcAndSubnetWrapper;
                if(subnet != null)
                {
                    LoadExistingSecurityGroups(subnet.Vpc.VpcId);
                }
            }
        }

        public ObservableCollection<SecurityGroupWrapper> AvailableSecurityGroups { get; private set; }

        void LoadExistingSecurityGroups(string vpcId)
        {
            new QuerySecurityGroupsWorker(CreateEC2Client(PageController.HostingWizard),
                                          vpcId,
                                          LOGGER,
                                          OnSecurityGroupsAvailable);
        }

        void OnSecurityGroupsAvailable(ICollection<SecurityGroup> securityGroups)
        {
            SetAvailableSecurityGroups(securityGroups, string.Empty);
        }


        public void SetAvailableSecurityGroups(ICollection<SecurityGroup> existingGroups, string autoSelectGroup)
        {
            this._ctlSecurityGroup.Items.Clear();
            this._ctlSecurityGroup.Items.Add(CREATE_NEW_TEXT);
            foreach(var group in existingGroups)
            {
                this._ctlSecurityGroup.Items.Add(string.Format("{0} ({1})", group.GroupId, group.GroupName));
            }
        }

        void OnVpcSubnetsAvailable(ICollection<Vpc> vpcs, ICollection<Subnet> subnets)
        {
            var defaultSelection = _ctlSubnetsPicker.SetAvailableVpcSubnets(vpcs, subnets, null);
            if (defaultSelection == null)
                this._ctlSecurityGroup.Items.Clear();
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


    }
}
