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

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ECSServicePage.xaml
    /// </summary>
    public partial class ECSServicePage : BaseAWSUserControl, INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ECSServicePage));

        public ECSServicePageController PageController { get; private set; }

        public ECSServicePage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ECSServicePage(ECSServicePageController pageController)
            : this()
        {
            PageController = pageController;

            this._ctlPlacementTemplate.ItemsSource = ECSWizardUtils.PlacementTemplates.Options;
        }

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                if (this.CreateNewService && string.IsNullOrWhiteSpace(this._ctlNewServiceName.Text))
                    return false;
                if (!this.DesiredCount.HasValue || this.DesiredCount.Value <= 0)
                    return false;
                if (!this.MinimumHealthy.HasValue || this.MinimumHealthy.Value <= 0)
                    return false;
                if (!this.MaximumPercent.HasValue || this.MaximumPercent.Value <= 0)
                    return false;
                if (this.MaximumPercent < this.MinimumHealthy)
                    return false;

                return true;
            }
        }

        public void InitializeWithNewCluster()
        {
            UpdateExistingServices();
        }

        public void PageActivating()
        {
            UpdatePlacementEditableState();
        }

        public bool CreateNewService
        {
            get { return this._ctlServicePicker.SelectedIndex == 0; }
        }


        public string Service
        {
            get { return this._ctlServicePicker.Text; }
            set { this._ctlServicePicker.Text = value; }
        }

        private void _ctlServicePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(this.CreateNewService)
            {
                this._ctlNewServiceName.Visibility = Visibility.Visible;
                this._ctlNewServiceName.IsEnabled = true;
                this._ctlNewServiceName.Focus();

                if(this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.ExistingCluster] != null)
                {
                    var cluster = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.ExistingCluster] as Cluster;
                    this._ctlDesiredCount.Text = (cluster.RegisteredContainerInstancesCount == 0 ? 1 : cluster.RegisteredContainerInstancesCount).ToString();
                }
                else
                {
                    this._ctlDesiredCount.Text = "1";
                }

                this._ctlMinimumHealthy.Text = "50";
                this._ctlMaximumPercent.Text = "200";

                if (!this._ctlServicePicker.Items.Contains(PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string))
                    this._ctlNewServiceName.Text = PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string;
            }
            else
            {
                this._ctlNewServiceName.Visibility = Visibility.Collapsed;
                this._ctlNewServiceName.IsEnabled = false;

                
                FetchDetailsForExistingService();
            }

            UpdatePlacementEditableState();
            NotifyPropertyChanged("Service");
        }

        private void UpdatePlacementEditableState()
        {
            if(!this.PageController.HostingWizard.IsFargateLaunch() && this.CreateNewService)
            {
                this._ctlPlacementTemplate.IsEnabled = true;
                this._ctlPlacementTemplate.Visibility = Visibility.Visible;
                this._ctlPlacementDescription.Visibility = Visibility.Visible;
                this._ctlPlacementLabel.Visibility = Visibility.Visible;
            }
            else
            {
                this._ctlPlacementTemplate.IsEnabled = false;
                this._ctlPlacementTemplate.Visibility = Visibility.Collapsed;
                this._ctlPlacementDescription.Visibility = Visibility.Collapsed;
                this._ctlPlacementLabel.Visibility = Visibility.Collapsed;
            }
        }

        string _newServiceName;
        public string NewServiceName
        {
            get { return _newServiceName; }
            set
            {
                _newServiceName = value;
                NotifyPropertyChanged("NewServiceName");
            }
        }

        int? _desiredCount;
        public int? DesiredCount
        {
            get { return _desiredCount; }
            set
            {
                _desiredCount = value;
                NotifyPropertyChanged("DesiredCount");
            }
        }

        int? _minimumHealthy;
        public int? MinimumHealthy
        {
            get { return _minimumHealthy; }
            set
            {
                _minimumHealthy = value;
                NotifyPropertyChanged("MinimumHealthy");
            }
        }

        int? _maximumPercent;
        public int? MaximumPercent
        {
            get { return _maximumPercent; }
            set
            {
                _maximumPercent = value;
                NotifyPropertyChanged("MaximumPercent");
            }
        }

        public ECSWizardUtils.PlacementTemplates PlacementTemplate
        {
            get { return this._ctlPlacementTemplate.SelectedItem as ECSWizardUtils.PlacementTemplates; }
        }

        public bool IsPlacementTemplateEnabled
        {
            get { return this._ctlPlacementTemplate.IsEnabled; }
            set { this._ctlPlacementTemplate.IsEnabled = value; }
        }



        private void _ctlPlacementTemplate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("PlacementTemplate");
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0] as ECSWizardUtils.PlacementTemplates;
                this._ctlPlacementDescription.Text = item.Description;
            }
            else
            {
                this._ctlPlacementDescription.Text = null;
            }
        }

        IDictionary<string, Service> _previousFetchServices = new Dictionary<string, Service>();
        private void FetchDetailsForExistingService()
        {
            Service service;
            if(!_previousFetchServices.TryGetValue(this._ctlServicePicker.Text, out service))
            {
                using (var ecsClient = CreateECSClient(PageController.HostingWizard))
                {
                    var response = ecsClient.DescribeServices(new DescribeServicesRequest
                    {
                        Cluster = this.PageController.Cluster,
                        Services = new List<string> { this._ctlServicePicker.Text }
                    });

                    if (response.Services.Count == 0)
                        return;

                    service = response.Services[0];
                }

                _previousFetchServices[service.ServiceName] = service;
            }

            this._ctlDesiredCount.Text = service.DesiredCount == 0 ? "1" : service.DesiredCount.ToString();
            if (service.DeploymentConfiguration != null)
            {
                this._ctlMinimumHealthy.Text = service.DeploymentConfiguration.MinimumHealthyPercent.ToString();
                this._ctlMaximumPercent.Text = service.DeploymentConfiguration.MaximumPercent.ToString();
            }
        }

        private void UpdateExistingServices()
        {
            this._ctlServicePicker.Items.Clear();
            this._ctlServicePicker.Items.Add(CREATE_NEW_TEXT);
            try
            {
                if (this.PageController.Cluster == null)
                    return;

                Task task1 = Task.Run(() =>
                {
                    var items = new List<string>();
                    using (var ecsClient = CreateECSClient(PageController.HostingWizard))
                    {
                        var response = new ListServicesResponse();
                        do
                        {
                            var request = new ListServicesRequest() { Cluster = this.PageController.Cluster, NextToken = response.NextToken };

                            response = ecsClient.ListServices(request);

                            foreach (var arn in response.ServiceArns)
                            {
                                var name = arn.Substring(arn.IndexOf('/') + 1);
                                items.Add(name);
                            }
                        } while (!string.IsNullOrEmpty(response.NextToken));
                    }

                    ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                    {
                        foreach (var service in items.OrderBy(x => x))
                        {
                            this._ctlServicePicker.Items.Add(service);
                        }

                        this._ctlServicePicker.Text = "";

                        var previousValue = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.Service] as string;
                        if (!string.IsNullOrWhiteSpace(previousValue) && items.Contains(previousValue))
                            this._ctlServicePicker.SelectedItem = previousValue;
                        else
                        {
                            this._ctlServicePicker.SelectedIndex = this._ctlServicePicker.Items.Count > 1 ? 1 : 0;
                        }
                    }));
                });
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing existing services for cluster.", e);
            }
        }
    }
}
