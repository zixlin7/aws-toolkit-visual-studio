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

using Amazon.ECR;
using Amazon.ECR.Model;
using System.Collections.Generic;
using System.Linq;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for PushImageToECRPage.xaml
    /// </summary>
    public partial class PushImageToECRPage : BaseAWSUserControl, INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(PushImageToECRPage));

        public PushImageToECRPageController PageController { get; private set; }

        IAmazonECR _ecrClient;


        public PushImageToECRPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public PushImageToECRPage(PushImageToECRPageController pageController)
            : this()
        {
            PageController = pageController;
            var hostWizard = PageController.HostingWizard;

            var userAccount = hostWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
            var regionEndpoints = hostWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

            this._ctlAccountAndRegion.Initialize(userAccount, regionEndpoints, new string[] { Constants.ECR_ENDPOINT_LOOKUP });
            this._ctlAccountAndRegion.PropertyChanged += _ctlAccountAndRegion_PropertyChanged;

            this._ctlConfigurationPicker.Items.Add("Release");
            this._ctlConfigurationPicker.Items.Add("Debug");
            this.Configuration = "Release";

            var buildConfiguration = hostWizard[PublishContainerToAWSWizardProperties.Configuration] as string;
            if (!string.IsNullOrEmpty(buildConfiguration) && this._ctlConfigurationPicker.Items.Contains(buildConfiguration))
            {
                this.Configuration = buildConfiguration;
            }

            UpdateExistingResources();
        }


        void UpdateExistingResources()
        {
            this._ctlDockerRepositoryPicker.Items.Clear();
            this._ctlDockerTagPicker.Items.Clear();
            try
            {
                if (this._ctlAccountAndRegion.SelectedAccount == null || this._ctlAccountAndRegion.SelectedRegion == null)
                    return;

                if (this._ecrClient != null)
                    this._ecrClient.Dispose();

                this._ecrClient = this._ctlAccountAndRegion.SelectedAccount.CreateServiceClient<AmazonECRClient>(this._ctlAccountAndRegion.SelectedRegion.GetEndpoint(Constants.ECR_ENDPOINT_LOOKUP));
                Task task1 = Task.Run(() =>
                {
                    var items = new List<string>();
                    var response = new DescribeRepositoriesResponse();
                    do
                    {
                        var request = new DescribeRepositoriesRequest() { NextToken = response.NextToken };

                        response = this._ecrClient.DescribeRepositories(request);

                        foreach (var repo in response.Repositories)
                        {
                            items.Add(repo.RepositoryName);
                        }
                    } while (!string.IsNullOrEmpty(response.NextToken));

                    ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                    {
                        foreach (var repository in items.OrderBy(x => x))
                        {
                            this._ctlDockerRepositoryPicker.Items.Add(repository);
                        }

                        this._ctlDockerRepositoryPicker.Text = "";
                    }));
                });

            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing existing ECR Repositories.", e);
            }
        }

        void UpdateExistingTags()
        {
            this._ctlDockerTagPicker.Items.Clear();

            if (this._ecrClient == null || string.IsNullOrWhiteSpace(this._ctlDockerRepositoryPicker.SelectedItem?.ToString()))
                return;

            var repository = this._ctlDockerRepositoryPicker.SelectedItem.ToString();

            Task task1 = Task.Run(() =>
            {
                var items = new HashSet<string>();
                var response = new ListImagesResponse();
                do
                {
                    var request = new ListImagesRequest() { RepositoryName = repository, NextToken = response.NextToken };

                    response = this._ecrClient.ListImages(request);

                    foreach (var image in response.ImageIds)
                    {
                        if(!items.Contains(image.ImageTag))
                            items.Add(image.ImageTag);
                    }
                } while (!string.IsNullOrEmpty(response.NextToken));

                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                {
                    foreach (var tag in items.OrderBy(x => x))
                    {
                        this._ctlDockerTagPicker.Items.Add(tag);
                    }

                    this._ctlDockerTagPicker.Text = "";
                }));
            });
        }

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                if (this.SelectedAccount == null)
                    return false;
                if (this.SelectedRegion == null)
                    return false;
                if (string.IsNullOrWhiteSpace(this.DockerRepository))
                    return false;
                if (string.IsNullOrWhiteSpace(this.Configuration))
                    return false;

                return true;
            }
        }

        public AccountViewModel SelectedAccount
        {
            get
            {
                return _ctlAccountAndRegion.SelectedAccount;
            }
        }

        public RegionEndPointsManager.RegionEndPoints SelectedRegion
        {
            get
            {
                return _ctlAccountAndRegion.SelectedRegion;
            }
        }

        void _ctlAccountAndRegion_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (this._ctlAccountAndRegion.SelectedAccount == null || this._ctlAccountAndRegion.SelectedRegion == null)
                return;

            PageController.HostingWizard.SetProperty(PublishContainerToAWSWizardProperties.UserAccount, this._ctlAccountAndRegion.SelectedAccount);
            PageController.HostingWizard.SetProperty(PublishContainerToAWSWizardProperties.Region, this._ctlAccountAndRegion.SelectedRegion);

            UpdateExistingResources();
        }

        string _configuration;
        public string Configuration
        {
            get { return _configuration; }
            set
            {
                _configuration = value;
                NotifyPropertyChanged("Configuration");
            }
        }

        public string DockerRepository
        {
            get { return this._ctlDockerRepositoryPicker.Text; }
            set { this._ctlDockerRepositoryPicker.Text = value; }
        }

        private void _ctlDockerRepositoryPicker_TextChanged(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged("DockerRepository");
        }

        private void _ctlDockerRepositoryPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("DockerRepository");
            UpdateExistingTags();
        }

        public string DockerTag
        {
            get { return this._ctlDockerTagPicker.Text; }
            set { this._ctlDockerTagPicker.Text = value; }
        }

        private void _ctlDockerTagPicker_TextChanged(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged("DockerTag");
        }

        private void _ctlDockerTagPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("DockerTag");
        }

    }
}
