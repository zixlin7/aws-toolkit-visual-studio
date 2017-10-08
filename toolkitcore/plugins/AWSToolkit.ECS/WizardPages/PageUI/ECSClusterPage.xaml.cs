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
            LoadPreviousValues(PageController.HostingWizard);
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

                return true;
            }
        }

        private void UpdateExistingClusters()
        {
            var currentTextValue = !string.IsNullOrWhiteSpace(this._ctlClusterPicker.Text) && 
                this._ctlClusterPicker.SelectedValue == null ? this._ctlClusterPicker.Text : null;
            this._ctlClusterPicker.Items.Clear();

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


                        var previousValue = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.Cluster] as string;
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
        }
    }
}
