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
            InitializeComponent();
            DataContext = this;
        }

        public ECSClusterPage(ECSClusterPageController pageController)
            : this()
        {
            PageController = pageController;

            UpdateExistingClusters();
        }

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.Cluster))
                    return false;
                if (string.IsNullOrWhiteSpace(this.Service))
                    return false;
                if (!this.DesiredCount.HasValue || this.DesiredCount < 0)
                    return false;

                return true;
            }
        }

        private void UpdateExistingClusters()
        {
            this._ctlClusterPicker.Items.Clear();
            this._ctlServicePicker.Items.Clear();

            try
            {
                var account = PageController.HostingWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
                var region = PageController.HostingWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

                Task task1 = Task.Run(() =>
                {
                    var items = new List<string>();
                    using (var ecsClient = account.CreateServiceClient<AmazonECSClient>(region.GetEndpoint(Constants.ECS_ENDPOINT_LOOKUP)))
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

                        this._ctlClusterPicker.Text = "";
                    }));
                });
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing existing ECS Clusters.", e);
            }
        }

        private void UpdateExistingServices()
        {
            this._ctlServicePicker.Items.Clear();
            try
            {
                if (this._ctlClusterPicker.SelectedItem == null)
                    return;

                var cluster = this._ctlClusterPicker.SelectedItem as string;

                var account = PageController.HostingWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
                var region = PageController.HostingWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

                Task task1 = Task.Run(() =>
                {
                    var items = new List<string>();
                    using (var ecsClient = account.CreateServiceClient<AmazonECSClient>(region.GetEndpoint(Constants.ECS_ENDPOINT_LOOKUP)))
                    {
                        var response = new ListServicesResponse();
                        do
                        {
                            var request = new ListServicesRequest() { Cluster = cluster, NextToken = response.NextToken };

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
                    }));
                });
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing existing ECS Task Definition Container.", e);
            }
        }


        public string Cluster
        {
            get { return this._ctlClusterPicker.Text; }
            set { this._ctlClusterPicker.Text = value; }
        }

        private void _ctlClusterPicker_TextChanged(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged("Cluster");
        }

        private void _ctlClusterPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Cluster");
            UpdateExistingServices();
        }

        public string Service
        {
            get { return this._ctlServicePicker.Text; }
            set { this._ctlServicePicker.Text = value; }
        }

        private void _ctlServicePicker_TextChanged(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged("Service");
        }

        private void _ctlServicePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Service");
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
    }
}
