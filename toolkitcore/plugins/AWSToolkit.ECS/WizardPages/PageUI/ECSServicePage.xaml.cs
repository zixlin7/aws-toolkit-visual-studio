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
    /// Interaction logic for ECSServicePahge.xaml
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
        }

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                return true;
            }
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


        /*
                private void UpdateExistingServices()
                {
                    var currentTextValue = !string.IsNullOrWhiteSpace(this._ctlServicePicker.Text) &&
                        this._ctlServicePicker.SelectedValue == null ? this._ctlServicePicker.Text : null;
                    this._ctlServicePicker.Items.Clear();

                    this._ctlServicePicker.Items.Clear();
                    try
                    {
                        if (this.PageController.Cluster == null)
                            return;

                        var account = PageController.HostingWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
                        var region = PageController.HostingWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

                        var unsetDesiredCount = string.IsNullOrWhiteSpace(this._ctlDesiredCount.Text);

                        Task task1 = Task.Run(() =>
                        {
                            int? instanceCount = null;
                            var items = new List<string>();
                            using (var ecsClient = account.CreateServiceClient<AmazonECSClient>(region.GetEndpoint(Constants.ECS_ENDPOINT_LOOKUP)))
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

                                if (unsetDesiredCount)
                                {
                                    var describeClusterResponse = ecsClient.DescribeClusters(new DescribeClustersRequest { Clusters = new List<string> { cluster } });
                                    if (describeClusterResponse.Clusters.Count == 1)
                                        instanceCount = describeClusterResponse.Clusters[0].RegisteredContainerInstancesCount;
                                }
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
                                    if (currentTextValue != null)
                                        this._ctlServicePicker.Text = currentTextValue;
                                    else
                                        this._ctlServicePicker.Text = "";
                                }

                                if (instanceCount.HasValue && unsetDesiredCount)
                                {
                                    this._ctlDesiredCount.Text = instanceCount.Value.ToString();
                                }
                            }));
                        });
                    }
                    catch (Exception e)
                    {
                        LOGGER.Error("Error refreshing existing ECS Task Definition Container.", e);
                    }
                }
        */
    }
}
