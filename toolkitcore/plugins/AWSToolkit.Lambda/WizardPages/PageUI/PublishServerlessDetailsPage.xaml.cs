using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Lambda.Nodes;
using Amazon.AWSToolkit.Lambda.WizardPages.PageControllers;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.S3;
using Amazon.S3.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.CloudFormation;

using ThirdParty.Json.LitJson;
using static Amazon.AWSToolkit.Lambda.Controller.UploadFunctionController;
using Amazon.AWSToolkit.Lambda.View.Components;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for PublishServerlessDetailsPage.xaml
    /// </summary>
    public partial class PublishServerlessDetailsPage : INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(PublishServerlessDetailsPage));

        public IAWSWizardPageController PageController { get; private set; }

        private string SeedS3Bucket { get; set; }
        private string SeedStackName { get; set; }


        public PublishServerlessDetailsPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public PublishServerlessDetailsPage(IAWSWizardPageController pageController)
            : this()
        {
            InitializeComponent();

            PageController = pageController;
            var hostWizard = PageController.HostingWizard;

            var userAccount = hostWizard[UploadFunctionWizardProperties.UserAccount] as AccountViewModel;
            var regionEndpoints = hostWizard[UploadFunctionWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

            this._ctlAccountAndRegion.Initialize(userAccount, regionEndpoints, new string[] { LambdaRootViewMetaNode.LAMBDA_ENDPOINT_LOOKUP });
            this._ctlAccountAndRegion.PropertyChanged += _ctlAccountAndRegion_PropertyChanged;

            this.SeedS3Bucket = hostWizard[UploadFunctionWizardProperties.S3Bucket] as string;
            this.SeedStackName = hostWizard[UploadFunctionWizardProperties.StackName] as string;

            UpdateExistingResources();

            InitializeNETCoreFields();


            var buildConfiguration = hostWizard[UploadFunctionWizardProperties.Configuration] as string;
            if (!string.IsNullOrEmpty(buildConfiguration) && this._ctlConfigurationPicker.Items.Contains(buildConfiguration))
            {
                this.Configuration = buildConfiguration;
            }

            var targetFramework = hostWizard[UploadFunctionWizardProperties.Framework] as string;
            if (!string.IsNullOrEmpty(targetFramework) && this._ctlFrameworkPicker.Items.Contains(targetFramework))
            {
                this.Framework = targetFramework;
            }

            this._ctlPersistSettings.IsChecked = true;
        }

        private void InitializeNETCoreFields()
        {
            this._ctlConfigurationPicker.Items.Add("Release");
            this._ctlConfigurationPicker.Items.Add("Debug");
            this.Configuration = "Release";

            var projectFrameworks = this.PageController.HostingWizard[UploadFunctionWizardProperties.ProjectTargetFrameworks] as IList<string>;
            if (projectFrameworks != null && projectFrameworks.Count > 0)
            {
                foreach (var framework in projectFrameworks)
                {
                    this._ctlFrameworkPicker.Items.Add(framework);
                }
            }
            else
            {
                if(!this.IsVS2015)
                {
                    this._ctlFrameworkPicker.Items.Add("netcoreapp2.0");
                }
                this._ctlFrameworkPicker.Items.Add("netcoreapp1.0");
            }

            this._ctlFrameworkPicker.SelectedIndex = 0;
            this.Framework = this._ctlFrameworkPicker.Items[0].ToString();
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

        string _framework;
        public string Framework
        {
            get { return _framework; }
            set
            {
                _framework = value;
                NotifyPropertyChanged("Framework");
            }
        }

        public string StackName
        {
            get { return this._ctlStackPicker.Text; }
            set { this._ctlStackPicker.Text = value; }
        }

        public bool SaveSettings
        {
            get { return this._ctlPersistSettings.IsChecked.GetValueOrDefault(); }
        }

        public bool IsNewStack
        {
            get
            {
                return !this._ctlStackPicker.Items.Contains(this._ctlStackPicker.Text);
            }
        }

        string _s3Bucket;
        public string S3Bucket
        {
            get { return _s3Bucket; }
            set
            {
                _s3Bucket = value;
                NotifyPropertyChanged("S3Bucket");
            }
        }

        void _ctlAccountAndRegion_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (this._ctlAccountAndRegion.SelectedAccount == null || this._ctlAccountAndRegion.SelectedRegion == null)
                return;

            PageController.HostingWizard.SetProperty(UploadFunctionWizardProperties.UserAccount, this._ctlAccountAndRegion.SelectedAccount);
            PageController.HostingWizard.SetProperty(UploadFunctionWizardProperties.Region, this._ctlAccountAndRegion.SelectedRegion);

            this.UpdateExistingResources();
        }

        void UpdateExistingResources()
        {
            this._ctlStackPicker.Items.Clear();
            this._ctlBucketPicker.Items.Clear();

            try
            {
                if (this._ctlAccountAndRegion.SelectedAccount == null || this._ctlAccountAndRegion.SelectedRegion == null)
                    return;

                var cloudFormationClient = this._ctlAccountAndRegion.SelectedAccount.CreateServiceClient<AmazonCloudFormationClient>(this._ctlAccountAndRegion.SelectedRegion.GetEndpoint(RegionEndPointsManager.CLOUDFORMATION_SERVICE_NAME));
                var s3Client = this._ctlAccountAndRegion.SelectedAccount.CreateServiceClient<AmazonS3Client>(this._ctlAccountAndRegion.SelectedRegion.GetEndpoint(RegionEndPointsManager.S3_SERVICE_NAME));

                Task task1 = Task.Run(() =>
                {
                    try
                    {
                        var items = new List<Stack>();
                        var response = new DescribeStacksResponse();
                        do
                        {
                            var request = new DescribeStacksRequest() { NextToken = response.NextToken };
                            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

                            response = cloudFormationClient.DescribeStacks(request);

                            foreach (var stack in response.Stacks)
                            {
                                if (stack.StackStatus == CloudFormationConstants.DeleteInProgressStatus ||
                                    stack.StackStatus == CloudFormationConstants.DeleteCompleteStatus)
                                    continue;


                                items.Add(stack);
                            }
                        } while (!string.IsNullOrEmpty(response.NextToken));


                        ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                        {
                            foreach (var stack in items.OrderBy(x => x.StackName))
                            {
                                this._ctlStackPicker.Items.Add(stack.StackName);
                            }

                            if (!string.IsNullOrEmpty(this.SeedStackName))
                                this._ctlStackPicker.Text = this.SeedStackName;
                            else
                                this._ctlStackPicker.Text = "";
                        }));
                    }
                    catch(Exception e)
                    {
                        LOGGER.Error("Error refreshing existing CloudFormation stacks.", e);
                    }
                });

                Task task2 = Task.Run(() =>
                {
                    try
                    {
                        var buckets = s3Client.ListBuckets().Buckets;

                        ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                        {
                            foreach (var bucket in buckets.OrderBy(x => x.BucketName))
                            {
                                this._ctlBucketPicker.Items.Add(bucket.BucketName);
                            }

                            if (!string.IsNullOrEmpty(this.SeedS3Bucket) && this._ctlBucketPicker.Items.Contains(this.SeedS3Bucket))
                                this._ctlBucketPicker.SelectedValue = this.SeedS3Bucket;
                        }));
                    }
                    catch(Exception e)
                    {
                        LOGGER.Error("Error refreshing existing S3 buckets.", e);
                    }
                });

                Task.WaitAll(task1, task2);
                cloudFormationClient.Dispose();
                s3Client.Dispose();

            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing existing CloudFormation stacks.", e);
            }

        }


        public bool AllRequiredFieldsAreSet
        {
            get
            {
                if (string.IsNullOrEmpty(this.Configuration))
                {
                    return false;
                }
                if (string.IsNullOrEmpty(this.Framework))
                {
                    return false;
                }
                if (string.IsNullOrEmpty(this.StackName))
                {
                    return false;
                }
                if (string.IsNullOrEmpty(this.S3Bucket))
                {
                    return false;
                }

                return true;
            }
        }

        private void _ctlStackPicker_TextChanged(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged("StackName");
        }

        private void _ctlStackPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("StackName");
        }

        private void _ctlNewBucket_Click(object sender, RoutedEventArgs e)
        {
            var bucket = CreateBucket();
            if (string.IsNullOrEmpty(bucket))
                return;

            int index = this._ctlBucketPicker.Items.Add(bucket);
            this._ctlBucketPicker.SelectedIndex = index;
        }

        private string CreateBucket()
        {
            var account = this._ctlAccountAndRegion.SelectedAccount;
            if (account == null)
                return null;
            var region = this._ctlAccountAndRegion.SelectedRegion;
            if (region == null)
                return null;

            IAmazonS3 s3Client = account.CreateServiceClient<AmazonS3Client>(region);

            var control = new NewS3Bucket(s3Client);
            if(ToolkitFactory.Instance.ShellProvider.ShowModal(control))
            {
                return control.BucketName;
            }

            return null;
        }

        private bool IsVS2015
        {
            get
            {
                if (string.Equals("2015", ToolkitFactory.Instance.ShellProvider.ShellVersion, StringComparison.Ordinal))
                    return true;

                return false;
            }
        }
    }
}