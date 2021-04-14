using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.S3;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.CloudFormation;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Lambda.View.Components;
using Amazon.AWSToolkit.Util;
using Amazon.Lambda;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for PublishServerlessDetailsPage.xaml
    /// </summary>
    public partial class PublishServerlessDetailsPage : INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(PublishServerlessDetailsPage));
        public static readonly string LambdaServiceName = new AmazonLambdaConfig().RegionEndpointServiceName;

        private const int AccountRegionChangedDebounceMs = 250;

        public IAWSWizardPageController PageController { get; set; }
        public AccountAndRegionPickerViewModel Connection { get; }

        private readonly DebounceDispatcher _accountRegionChangeDebounceDispatcher = new DebounceDispatcher();

        private string SeedS3Bucket { get; set; }
        private string SeedStackName { get; set; }

        public PublishServerlessDetailsPage(IAWSWizardPageController pageController, ToolkitContext toolkitContext)
        {
            Connection = new AccountAndRegionPickerViewModel(toolkitContext);
            Connection.SetServiceFilter(new List<string>() {LambdaServiceName});

            InitializeComponent();

            DataContext = this;

            PageController = pageController;
            var hostWizard = PageController.HostingWizard;

            var userAccount = hostWizard.GetSelectedAccount(UploadFunctionWizardProperties.UserAccount);
            var region = hostWizard.GetSelectedRegion(UploadFunctionWizardProperties.Region);

            Connection.Account = userAccount;
            Connection.Region = region;

            this.SeedS3Bucket = hostWizard[UploadFunctionWizardProperties.S3Bucket] as string;
            this.SeedStackName = hostWizard[UploadFunctionWizardProperties.StackName] as string;

            UpdateExistingResources();

            this._ctlPersistSettings.IsChecked = true;
        }

        public string StackName
        {
            get => this._ctlStackPicker.Text;
            set => this._ctlStackPicker.Text = value;
        }

        public bool SaveSettings => this._ctlPersistSettings.IsChecked.GetValueOrDefault();

        public bool IsNewStack => !this._ctlStackPicker.Items.Contains(this._ctlStackPicker.Text);

        string _s3Bucket;
        public string S3Bucket
        {
            get => _s3Bucket;
            set
            {
                _s3Bucket = value;
                NotifyPropertyChanged("S3Bucket");
            }
        }

        void ConnectionChanged(object sender, EventArgs e)
        {
            if (!Connection.ConnectionIsValid)
            {
                return;
            }

            PageController.HostingWizard.SetSelectedAccount(Connection.Account, UploadFunctionWizardProperties.UserAccount);
            PageController.HostingWizard.SetSelectedRegion(Connection.Region, UploadFunctionWizardProperties.Region);

            // Prevent multiple loads caused by property changed events in rapid succession
            _accountRegionChangeDebounceDispatcher.Debounce(AccountRegionChangedDebounceMs, _ =>
            {
                ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread(() =>
                {
                    this.UpdateExistingResources();
                });
            });
        }

        void UpdateExistingResources()
        {
            this._ctlStackPicker.Items.Clear();
            this._ctlBucketPicker.Items.Clear();

            try
            {
                if (!Connection.ConnectionIsValid)
                {
                    return;
                }

                var account = Connection.Account;
                var region = Connection.Region;

                var cloudFormationClient = account.CreateServiceClient<AmazonCloudFormationClient>(region);
                var s3Client = account.CreateServiceClient<AmazonS3Client>(region);

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
                if (!Connection.ConnectionIsValid || Connection.IsValidating)
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
            if (!Connection.ConnectionIsValid)
            {
                return null;
            }

            var account = Connection.Account;
            var region = Connection.Region;

            IAmazonS3 s3Client = account.CreateServiceClient<AmazonS3Client>(region);

            var control = new NewS3Bucket(s3Client);
            if(ToolkitFactory.Instance.ShellProvider.ShowModal(control))
            {
                return control.BucketName;
            }

            return null;
        }
    }
}
