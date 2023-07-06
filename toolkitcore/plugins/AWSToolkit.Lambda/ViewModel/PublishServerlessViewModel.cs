using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudFormation;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.CommonValidators;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Lambda.View.Components;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Runtime;
using Amazon.S3;

using log4net;

namespace Amazon.AWSToolkit.Lambda.ViewModel
{
    public class PublishServerlessViewModel : BaseModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(PublishServerlessViewModel));
        private readonly ToolkitContext _toolkitContext;
        private string _stack;
        private string _s3Bucket;
        private bool _saveSettings;
        private bool _loading = true;

        private ObservableCollection<string> _stacks = new ObservableCollection<string>();
        private ObservableCollection<string> _buckets = new ObservableCollection<string>();

        private ICommand _createBucketCommand;

        public PublishServerlessViewModel(ToolkitContext toolkitContext)
        {
            Connection = new AccountAndRegionPickerViewModel(toolkitContext);
            _toolkitContext = toolkitContext;
            CreateBucketCommand = new RelayCommand(CreateBucket);
        }

        public AccountAndRegionPickerViewModel Connection { get; }

        public bool IsValid => !DataErrorInfo.HasErrors;

        public string S3Bucket
        {
            get => _s3Bucket;
            set
            {
                SetProperty(ref _s3Bucket, value);
                ValidateS3Bucket(S3Bucket);
            }
        }

        public string Stack
        {
            get => _stack;
            set
            {
                SetProperty(ref _stack, value);
                ValidateStack(Stack);
            }
        }


        public ICommand CreateBucketCommand
        {
            get => _createBucketCommand;
            set => SetProperty(ref _createBucketCommand, value);
        }


        public bool SaveSettings
        {
            get => _saveSettings;
            set => SetProperty(ref _saveSettings, value);
        }

        public ObservableCollection<string> Stacks
        {
            get => _stacks;
            set => SetProperty(ref _stacks, value);
        }

        public ObservableCollection<string> Buckets
        {
            get => _buckets;
            set => SetProperty(ref _buckets, value);
        }

        public bool Loading
        {
            get => _loading;
            set
            {
                if (object.Equals(_loading, value))
                {
                    return;
                }

                _loading = value;
                // Validate required property values after loading completes
                // This has been done to prevent showing the name error validation when loading is in progress
                // and ensuring the finish button remains disabled until after the validation completes
                if (!_loading)
                {
                    ValidateRequiredNames();
                }
                NotifyPropertyChanged(nameof(Loading));
            }
        }

        public bool IsNewStack => !Stacks.Contains(Stack);

        public async Task UpdateExistingResourcesAsync()
        {
            try
            {
                DataErrorInfo.ClearErrors(nameof(S3Bucket), nameof(Stack));
                using (CreateLoadingScope())
                {
                    await Task.WhenAll(UpdateStacksAsync(), UpdateBucketsAsync()).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.Error("Error refreshing existing resources", e);
            }
        }

        public bool IsValidConfiguration()
        {
            if (!Connection.ConnectionIsValid || Connection.IsValidating)
            {
                return false;
            }

            if (Loading)
            {
                return false;
            }

            if (string.IsNullOrEmpty(Stack))
            {
                return false;
            }

            if (string.IsNullOrEmpty(S3Bucket))
            {
                return false;
            }

            return IsValid;
        }


        private IDisposable CreateLoadingScope()
        {
            SetLoading(true);
            return new DisposingAction(() => SetLoading(false));
        }

        private void SetLoading(bool value)
        {
            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                Loading = value;
            });
        }


        private async Task UpdateStacksAsync()
        {
            using (var cfnClient = CreateServiceClient<AmazonCloudFormationClient>())
            {
                await UpdateStacksAsync(cfnClient).ConfigureAwait(false);
            }
        }

        private async Task UpdateBucketsAsync()
        {
            using (var s3Client = CreateServiceClient<AmazonS3Client>())
            {
                await UpdateBucketsAsync(s3Client).ConfigureAwait(false);
            }
        }

        public async Task UpdateStacksAsync(IAmazonCloudFormation cfnClient)
        {
            var currentStackName = Stack;
            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() => Stacks.Clear());

            try
            {
                var items = new List<Stack>();
                var response = new DescribeStacksResponse();
                do
                {
                    var request = new DescribeStacksRequest() { NextToken = response.NextToken };
                    response = await cfnClient.DescribeStacksAsync(request);

                    foreach (var stack in response.Stacks)
                    {
                        if (stack.StackStatus == CloudFormationConstants.DeleteInProgressStatus ||
                            stack.StackStatus == CloudFormationConstants.DeleteCompleteStatus)
                        {
                            continue;
                        }

                        items.Add(stack);
                    }
                } while (!string.IsNullOrEmpty(response.NextToken));


                _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
                {
                    var names = items.OrderBy(st => st.StackName).Select(x => x.StackName);
                    Stacks = new ObservableCollection<string>(names);
                    Stack = Stacks.FirstOrDefault(stack => stack.Equals(currentStackName)) ?? currentStackName ?? string.Empty;
                });
            }
            catch (Exception e)
            {
                _logger.Error("Error refreshing existing CloudFormation stacks", e);
            }
        }

        public async Task UpdateBucketsAsync(IAmazonS3 s3Client)
        {
            var previousBucket = S3Bucket;
            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() => Buckets.Clear());

            try
            {
                var response = await s3Client.ListBucketsAsync();
                var buckets = response.Buckets.OrderBy(bucket => bucket.BucketName).Select(x => x.BucketName);

                _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
                {
                    Buckets = new ObservableCollection<string>(buckets);


                    S3Bucket = Buckets.FirstOrDefault(bucket => bucket.Equals(previousBucket)) ??
                               Buckets.FirstOrDefault();
                });
            }
            catch (Exception e)
            {
                _logger.Error("Error refreshing existing S3 buckets.", e);
            }
        }

        private TServiceClient CreateServiceClient<TServiceClient>() where TServiceClient : class, IAmazonService
        {
            return _toolkitContext.ServiceClientManager.CreateServiceClient<TServiceClient>(
                Connection.Account.Identifier, Connection.Region);
        }


        private void CreateBucket(object obj)
        {
            var bucket = CreateNewBucket();
            if (string.IsNullOrEmpty(bucket))
            {
                return;
            }

            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                Buckets.Add(bucket);
                S3Bucket = Buckets.FirstOrDefault(x => x.Equals(bucket)) ??
                           Buckets.FirstOrDefault();
            });
        }

        private string CreateNewBucket()
        {
            if (!Connection.ConnectionIsValid)
            {
                return null;
            }

            using (var s3Client = CreateServiceClient<AmazonS3Client>())
            {
                var control = new NewS3Bucket(s3Client);
                if (_toolkitContext.ToolkitHost.ShowModal(control))
                {
                    return control.BucketName;
                }
            }

            return null;
        }

        private void ValidateS3Bucket(string bucket)
        {
            DataErrorInfo.ClearErrors(nameof(S3Bucket));
            if (string.IsNullOrWhiteSpace(bucket))
            {
                if (!Loading)
                {
                    DataErrorInfo.AddError("Bucket name cannot be empty", nameof(S3Bucket));
                }
            }

            using (var s3Client = CreateServiceClient<AmazonS3Client>())
            {
                var result= S3BucketLocationValidator.Validate(s3Client, bucket, Connection.Region.Id);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    DataErrorInfo.AddError(result, nameof(S3Bucket));
                }
            }
            NotifyPropertyChanged(nameof(IsValid));
        }

        private void ValidateStack(string stack)
        {
            try
            {
                DataErrorInfo.ClearErrors(nameof(Stack));

                if (string.IsNullOrWhiteSpace(stack))
                {
                    if (!Loading)
                    {
                        DataErrorInfo.AddError("Stack name cannot be empty", nameof(Stack));
                    }
                }

                if (!Stacks.Contains(stack))
                {
                    // don't validate if new stack is being created
                    return;
                }

                using (var cfnClient = CreateServiceClient<AmazonCloudFormationClient>())
                {
                    var result = CfnStackStatusValidator.Validate(cfnClient, stack);
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        DataErrorInfo.AddError(result, nameof(Stack));
                    }
                }
            }
            finally
            {
                NotifyPropertyChanged(nameof(IsValid));
            }
        }

        private void ValidateRequiredNames()
        {
            if (string.IsNullOrWhiteSpace(S3Bucket))
            {
                DataErrorInfo.AddError("Bucket name cannot be empty", nameof(S3Bucket));
            }

            if (string.IsNullOrWhiteSpace(Stack))
            {
                DataErrorInfo.AddError("Stack name cannot be empty", nameof(Stack));
            }
        }
    }
}
