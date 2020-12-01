using Amazon.AWSToolkit.Lambda.ViewModel;
using Amazon.AWSToolkit.Shared;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class UploadFunctionViewModelTests
    {
        const string SampleFunctionName = "someFunction";
        private static readonly string[] SampleRepositoryNames = new string[] {"somerepo", "somerepo2"};

        private readonly Mock<IAWSToolkitShellProvider> _shellProvider = new Mock<IAWSToolkitShellProvider>();
        private readonly UploadFunctionViewModel _sut;
        private readonly Mock<IAmazonLambda> _lambdaClient = new Mock<IAmazonLambda>();
        private readonly Mock<IAmazonECR> _ecrClient = new Mock<IAmazonECR>();

        private readonly ListFunctionsResponse _listFunctionsResponse = new ListFunctionsResponse();
        private readonly DescribeRepositoriesResponse _describeReposResponse = new DescribeRepositoriesResponse();

        public UploadFunctionViewModelTests()
        {
            _sut = new UploadFunctionViewModel(_shellProvider.Object);

            _shellProvider.Setup(mock => mock.ExecuteOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(action => action());

            _listFunctionsResponse.Functions = new List<FunctionConfiguration>()
            {
                new FunctionConfiguration()
                {
                    FunctionName = SampleFunctionName
                }
            };

            _lambdaClient.Setup(mock =>
                    mock.ListFunctionsAsync(It.IsAny<ListFunctionsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_listFunctionsResponse);

            SetupEcrClient();
        }

        [Theory]
        [InlineData("LambdaNetCore21::LambdaNetCore21.Function::FunctionHandler", "LambdaNetCore21",
            "LambdaNetCore21.Function", "FunctionHandler")]
        [InlineData("", "", "", "")]
        [InlineData(null, "", "", "")]
        [InlineData("garbage", "", "", "")]
        [InlineData("aaa::::", "aaa", "", "")]
        [InlineData("::aaa::", "", "aaa", "")]
        [InlineData("::::aaa", "", "", "aaa")]
        public void ApplyDotNetHandler(string handler, string expectedAssembly, string expectedType,
            string expectedMethod)
        {
            _sut.ApplyDotNetHandler(handler);

            Assert.Equal(handler, _sut.Handler);
            Assert.Equal(expectedAssembly, _sut.HandlerAssembly);
            Assert.Equal(expectedType, _sut.HandlerType);
            Assert.Equal(expectedMethod, _sut.HandlerMethod);
        }

        [Theory]
        [InlineData("LambdaNetCore21", "LambdaNetCore21.Function", "FunctionHandler",
            "LambdaNetCore21::LambdaNetCore21.Function::FunctionHandler")]
        [InlineData(" aaa ", " bbb ", " ccc ", "aaa::bbb::ccc")]
        [InlineData(null, "bbb", "ccc", "::bbb::ccc")]
        [InlineData("aaa", null, "ccc", "aaa::::ccc")]
        [InlineData("aaa", "bbb", null, "aaa::bbb::")]
        [InlineData("", "", "", "")]
        public void CreateDotNetHandler(string handlerAssembly, string handlerType, string handlerMethod,
            string expectedHandler)
        {
            _sut.HandlerAssembly = handlerAssembly;
            _sut.HandlerType = handlerType;
            _sut.HandlerMethod = handlerMethod;

            Assert.Equal(expectedHandler, _sut.CreateDotNetHandler());
        }

        [Fact]
        public void SetFrameworkIfExists_WhenExists()
        {
            _sut.Frameworks.Add("bee");
            _sut.SetFrameworkIfExists("bee");
            Assert.Equal("bee", _sut.Framework);
        }

        [Fact]
        public void SetFrameworkIfExists_WhenNotExists()
        {
            _sut.SetFrameworkIfExists("bee");
            Assert.NotEqual("bee", _sut.Framework);
        }

        [Fact]
        public async Task UpdateFunctionsList()
        {
            await _sut.UpdateFunctionsList(_lambdaClient.Object);

            Assert.Single(_sut.Functions);
            Assert.Equal(SampleFunctionName, _sut.Functions.First());
        }

        [Fact]
        public async Task UpdateImageRepos()
        {
            await _sut.UpdateImageRepos(_ecrClient.Object);
            Assert.Equal(SampleRepositoryNames, _sut.ImageRepos);
        }

        [Fact]
        public async Task UpdateImageTags()
        {
            var expectedTags = new string[] {"helloworld1", "helloworld2", "latest"};

            _sut.ImageRepo = "helloworld";
            await _sut.UpdateImageTags(_ecrClient.Object);
            Assert.Equal(expectedTags, _sut.ImageTags);
        }

        [Fact]
        public async Task UpdateImageTags_BlankRepo()
        {
            _sut.ImageRepo = string.Empty;
            await _sut.UpdateImageTags(_ecrClient.Object);
            Assert.Empty(_sut.ImageTags);
        }

        [Fact]
        public async Task FunctionExists()
        {
            await _sut.UpdateFunctionsList(_lambdaClient.Object);

            _sut.FunctionName = "fakeFunction";
            Assert.False(_sut.FunctionExists);

            _sut.FunctionName = SampleFunctionName;
            Assert.True(_sut.FunctionExists);
        }

        [Fact]
        public async Task TryGetFunctionConfig()
        {
            await _sut.UpdateFunctionsList(_lambdaClient.Object);

            Assert.False(_sut.TryGetFunctionConfig("fakeFunction", out _));
            Assert.True(_sut.TryGetFunctionConfig(SampleFunctionName, out _));
        }

        private void SetupEcrClient()
        {
            _describeReposResponse.Repositories = SampleRepositoryNames.Select(repoName => new Repository()
            {
                RepositoryName = repoName
            }).ToList();

            _ecrClient.Setup(mock =>
                    mock.DescribeRepositoriesAsync(It.IsAny<DescribeRepositoriesRequest>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(_describeReposResponse);

            // DescribeImagesAsync returns image tags "foo1, foo2, latest" where "foo" is repo name
            _ecrClient.Setup(mock => mock.DescribeImagesAsync(It.IsAny<DescribeImagesRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((DescribeImagesRequest request, CancellationToken token) =>
                {
                    var imageDetails = new List<ImageDetail>()
                    {
                        new ImageDetail()
                        {
                            RepositoryName = request.RepositoryName, ImageTags = new List<string>()
                            {
                                $"{request.RepositoryName}1",
                                $"{request.RepositoryName}2",
                                "latest"
                            }
                        }
                    };
                    
                    var response = new DescribeImagesResponse()
                    {
                        ImageDetails = imageDetails
                    };
                    
                    return response;
                });
        }
    }
}