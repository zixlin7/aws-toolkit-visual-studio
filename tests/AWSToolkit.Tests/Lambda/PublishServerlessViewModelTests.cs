using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Lambda.ViewModel;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Account;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class PublishServerlessViewModelTests
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly PublishServerlessViewModel _viewModel;
        private readonly string _sampleBucket = "testBucket";
        private readonly string _sampleStack = "testStack";
        private readonly Mock<IAmazonS3> _s3Client = new Mock<IAmazonS3>();
        private readonly Mock<IAmazonCloudFormation> _cfnClient = new Mock<IAmazonCloudFormation>();
        private readonly DescribeStacksResponse _describeStacksResponse = new DescribeStacksResponse();
        private readonly ListBucketsResponse _listBucketsResponse = new ListBucketsResponse();

        private static readonly ToolkitRegion _sampleRegion = new ToolkitRegion()
        {
            DisplayName = "sample-region", Id = "sample-region",
        };


        public PublishServerlessViewModelTests()
        {
            _viewModel = new PublishServerlessViewModel(_toolkitContextFixture.ToolkitContext);

            _toolkitContextFixture.ToolkitHost.Setup(mock => mock.ExecuteOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(action => action());

            _describeStacksResponse.Stacks = new List<Stack>()
            {
                new Stack() { StackName = _sampleStack, StackStatus = StackStatus.CREATE_COMPLETE }
            };

            _cfnClient.Setup(mock =>
                    mock.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_describeStacksResponse);


            _listBucketsResponse.Buckets = new List<S3Bucket>() { new S3Bucket() { BucketName = _sampleBucket } };

            _s3Client.Setup(mock =>
                    mock.ListBucketsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_listBucketsResponse);

            SetupValidConfiguration();
        }


        [Fact]
        public void InvalidConfiguration_WhenConnectionInvalid()
        {
            _viewModel.Connection.ConnectionIsValid = false;
            Assert.False(_viewModel.IsValidConfiguration());
        }

        [Fact]
        public void VerifyInvalidConfiguration_WhenBucketEmpty()
        {
            _viewModel.S3Bucket = string.Empty;
            Assert.False(_viewModel.IsValidConfiguration());
        }

        [Fact]
        public void VerifyInvalidConfiguration_WhenStackEmpty()
        {
            _viewModel.Stack = string.Empty;
            Assert.False(_viewModel.IsValidConfiguration());
        }

        [Fact]
        public void VerifyInvalidConfiguration_WhenLoading()
        {
            _viewModel.Loading = true;
            Assert.False(_viewModel.IsValidConfiguration());
        }

        [Fact]
        public async Task UpdateStacks()
        {
            Assert.Empty(_viewModel.Stacks);
            await _viewModel.UpdateStacksAsync(_cfnClient.Object);

            Assert.Single(_viewModel.Stacks);
            Assert.Equal(_sampleStack, _viewModel.Stacks.First());
        }

        [Fact]
        public async Task UpdateStacks_FiltersInvalidStacks()
        {
            Assert.Empty(_viewModel.Stacks);
            _describeStacksResponse.Stacks = new List<Stack>()
            {
                new Stack() { StackName = _sampleStack, StackStatus = StackStatus.DELETE_COMPLETE }
            };

            await _viewModel.UpdateStacksAsync(_cfnClient.Object);

            Assert.Empty(_viewModel.Stacks);
        }

        [Fact]
        public async Task UpdateStacks_WhenError()
        {
            Assert.Empty(_viewModel.Stacks);
            _cfnClient.Setup(mock =>
                    mock.DescribeStacksAsync(It.IsAny<DescribeStacksRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonServiceException("error"));

            await _viewModel.UpdateStacksAsync(_cfnClient.Object);

            Assert.Empty(_viewModel.Stacks);
        }

        [Fact]
        public async Task IsNewStack()
        {
            Assert.Empty(_viewModel.Stacks);
            await _viewModel.UpdateStacksAsync(_cfnClient.Object);

            _viewModel.Stack = "fakeStack";
            Assert.True(_viewModel.IsNewStack);

            _viewModel.Stack = _sampleStack;
            Assert.False(_viewModel.IsNewStack);
        }


        [Fact]
        public async Task UpdateBuckets()
        {
            Assert.Empty(_viewModel.Buckets);
            await _viewModel.UpdateBucketsAsync(_s3Client.Object);

            Assert.Single(_viewModel.Buckets);
            Assert.Equal(_sampleBucket, _viewModel.Buckets.First());
        }

        [Fact]
        public async Task UpdateBuckets_WhenError()
        {
            Assert.Empty(_viewModel.Buckets);
            _s3Client.Setup(mock =>
                    mock.ListBucketsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonS3Exception("error"));

            await _viewModel.UpdateBucketsAsync(_s3Client.Object);

            Assert.Empty(_viewModel.Buckets);
        }


        [Fact]
        public void CreateBucketCommand_WhenConnectionInvalid()
        {
            Assert.Empty(_viewModel.Buckets);

            _viewModel.Connection.ConnectionIsValid = false;
            SetupShowModal(true);

            _viewModel.CreateBucketCommand.Execute(null);

            Assert.Empty(_viewModel.Buckets);
        }

        [StaFact]
        public void CreateBucketCommand_WhenCancelled()
        {
            Assert.Empty(_viewModel.Buckets);

            SetupShowModal(false);

            _viewModel.CreateBucketCommand.Execute(null);

            Assert.Empty(_viewModel.Buckets);
        }


        [Fact]
        public void VerifyStackChangeTriggersValidation_WhenEmpty()
        {
           Assert.Equal(_sampleStack, _viewModel.Stack);
           Assert.True(_viewModel.IsValid);

           _viewModel.Stack = string.Empty;
           Assert.False(_viewModel.IsValid);
        }

        [Fact]
        public void VerifyStackChangeDoesNotTriggerValidation_WhenNew()
        {
            Assert.Equal(_sampleStack, _viewModel.Stack);
            Assert.True(_viewModel.IsValid);

            _viewModel.Stack = "testStack2";
            Assert.True(_viewModel.IsValid);
        }

        [Fact]
        public void VerifyStackChangeHasNoValidationError_WhenEmptyAndLoading()
        {
            Assert.Equal(_sampleStack, _viewModel.Stack);
            Assert.True(_viewModel.IsValid);

            _viewModel.Loading = true;
            _viewModel.Stack = string.Empty;
            Assert.True(_viewModel.IsValid);
        }

        [Fact]
        public void VerifyBucketChangeTriggersValidation_WhenEmpty()
        {
            Assert.Equal(_sampleBucket, _viewModel.S3Bucket);
            Assert.True(_viewModel.IsValid);

            _viewModel.S3Bucket = string.Empty;
            Assert.False(_viewModel.IsValid);
        }

        [Fact]
        public void VerifyBucketChangeHasNoValidationError_WhenEmptyAndLoading()
        {
            Assert.Equal(_sampleBucket, _viewModel.S3Bucket);
            Assert.True(_viewModel.IsValid);

            _viewModel.Loading = true;
            _viewModel.S3Bucket = string.Empty;
            Assert.True(_viewModel.IsValid);
        }


        [Fact]
        public void VerifyLoadingCompletionTriggersValidation()
        {
            _viewModel.Loading = true;
            _viewModel.S3Bucket = string.Empty;
            Assert.True(_viewModel.IsValid);

            _viewModel.Loading = false;
            Assert.False(_viewModel.IsValid);
        }

        private void SetupShowModal(bool result)
        {
            _toolkitContextFixture.ToolkitHost.Setup(mock => mock.ShowModal(It.IsAny<IAWSToolkitControl>()))
                .Returns(result);
        }

        private void SetupValidConfiguration()
        {
            _viewModel.Connection.ConnectionIsValid = true;
            _viewModel.Connection.IsValidating = false;
            _viewModel.Connection.Account = AccountFixture.CreateFakeCredentialAccount();
            _viewModel.Connection.Region = _sampleRegion;
            _viewModel.S3Bucket = _sampleBucket;
            _viewModel.Stack = _sampleStack;
            _viewModel.Loading = false;
        }
    }
}
