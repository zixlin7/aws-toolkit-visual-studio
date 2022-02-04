using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.DeploymentWorkers;
using Amazon.AWSToolkit.Lambda.Util;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.AWSToolkit.Util;
using Amazon.Lambda;
using Amazon.Lambda.Model;

using Moq;

using Xunit;

using Runtime = Amazon.Lambda.Runtime;

namespace AWSToolkit.Tests.Lambda
{
    public class UploadGenericWorkerTests : IDisposable
    {
        private static readonly string[] ExpectedIncludedFiles =
        {
            "ReadMe.txt",
            "sample.zip",
            "app.js",
            "node_modules/aws-sdk/index.js",
        };
        private static readonly string[] ExpectedExcludedFiles =
        {
            "sample.njsproj",
            "sampleUpper.NJsProj",
            "sample.sln",
            "sampleUpper.SLN",
            "sample.suo",
            "sampleUpper.SUO",
            ".gitignore",
            "_testdriver.js",
            "_sampleEvent.json",
        };

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly Mock<ILambdaFunctionUploadHelpers> _uploadHelpers = new Mock<ILambdaFunctionUploadHelpers>();
        private readonly Mock<IAmazonLambda> _lambda = new Mock<IAmazonLambda>();
        private readonly UploadGenericWorker _sut;
        private readonly UploadFunctionController.UploadFunctionState _uploadFunctionState;

        private GetFunctionConfigurationResponse _getExistingConfigurationResponse;

        public UploadGenericWorkerTests()
        {
            SetupGetExistingConfiguration();

            _sut = new UploadGenericWorker(_uploadHelpers.Object, _lambda.Object, null,
                _toolkitContextFixture.ToolkitContext);

            _uploadFunctionState = new UploadFunctionController.UploadFunctionState()
            {
                Request = new CreateFunctionRequest()
                {
                    Runtime = Runtime.Nodejs12X,
                    FunctionName = "sample-function",
                },

                Region = new ToolkitRegion()
                {
                    PartitionId = "aws",
                    DisplayName = "US East",
                    Id = "us-east-1",
                },

                AccountId = "0123456789"
            };

            // Create a sample project and files to use in tests
            ExpectedExcludedFiles
                .Concat(ExpectedIncludedFiles)
                .Select(file => Path.Combine(_testLocation.InputFolder, file))
                .ToList()
                .ForEach(PlaceholderFile.Create);
        }

        private void SetupGetExistingConfiguration()
        {
            _uploadHelpers.Setup(mock => mock.GetExistingConfiguration(It.IsAny<IAmazonLambda>(), It.IsAny<string>()))
                .Returns(() => _getExistingConfigurationResponse);
        }

        [Fact]
        public void GetProjectFilesToUpload()
        {
            var files = UploadGenericWorker.GetProjectFilesToUpload(_testLocation.InputFolder);

            var relativePaths = files
                .Select(file => file.Substring(_testLocation.InputFolder.Length + 1))
                .OrderBy(file => file)
                .ToArray();

            Assert.Empty(NormalizePaths(files).Intersect(NormalizePaths(ExpectedExcludedFiles)));

            var expectedFiles = NormalizePaths(ExpectedIncludedFiles).OrderBy(file => file);
            Assert.Equal(expectedFiles, NormalizePaths(relativePaths));

            Assert.DoesNotContain(relativePaths, file => UploadGenericWorker.ExcludedFiles.Any(regex => regex.IsMatch(file)));
        }

        [Fact]
        public void UploadProjectFolder()
        {
            _uploadFunctionState.SourcePath = _testLocation.InputFolder;

            _lambda
                .Setup(mock => mock.CreateFunction(It.IsAny<CreateFunctionRequest>()))
                .Callback<CreateFunctionRequest>(request =>
                {
                    UnzipToOutputFolder(request.Code.ZipFile);
                });

            _sut.UploadFunction(_uploadFunctionState);

            // Check the zip file that was produced (by seeing what we've extracted from it)
            _lambda.Verify(mock => mock.CreateFunction(It.IsAny<CreateFunctionRequest>()), Times.Once);

            var unZippedFiles = Directory.GetFiles(_testLocation.OutputFolder, "*.*", SearchOption.AllDirectories)
                .Select(file => file.Substring(_testLocation.OutputFolder.Length + 1))
                .ToArray();

            Assert.NotEmpty(unZippedFiles);
            Assert.DoesNotContain(unZippedFiles, file => UploadGenericWorker.ExcludedFiles.Any(regex => regex.IsMatch(file)));
            AssertSuccessfulDeployMetric();
        }

        [Fact]
        public void UploadZipFile()
        {
            // our fake zip is a text file
            var sampleZipPath = Path.Combine(_testLocation.InputFolder, "sample.zip");
            var expectedZipContents = File.ReadAllText(sampleZipPath);
            string actualZipContents = null;

            _uploadFunctionState.SourcePath = sampleZipPath;

            _lambda
                .Setup(mock => mock.CreateFunction(It.IsAny<CreateFunctionRequest>()))
                .Callback<CreateFunctionRequest>(request =>
                {
                    using (var reader = new StreamReader(request.Code.ZipFile))
                    {
                        actualZipContents = reader.ReadToEnd();
                    }
                });

            _sut.UploadFunction(_uploadFunctionState);

            _lambda.Verify(mock => mock.CreateFunction(It.IsAny<CreateFunctionRequest>()), Times.Once);

            // Check that our fake zip file was "uploaded"
            Assert.Equal(expectedZipContents, actualZipContents);
            AssertSuccessfulDeployMetric();
        }

        [Fact]
        public void UploadSingleFile()
        {
            var fileToUpload = Path.Combine(_testLocation.InputFolder, "app.js");

            _uploadFunctionState.SourcePath = fileToUpload;

            _lambda
                .Setup(mock => mock.CreateFunction(It.IsAny<CreateFunctionRequest>()))
                .Callback<CreateFunctionRequest>(request =>
                {
                    UnzipToOutputFolder(request.Code.ZipFile);
                });

            _sut.UploadFunction(_uploadFunctionState);

            _lambda.Verify(mock => mock.CreateFunction(It.IsAny<CreateFunctionRequest>()), Times.Once);

            // Check that our code file was the only thing zipped and "uploaded"
            var unZippedFiles = Directory.GetFiles(_testLocation.OutputFolder, "*.*", SearchOption.AllDirectories)
                .Select(file => file.Substring(_testLocation.OutputFolder.Length + 1))
                .ToArray();

            Assert.NotEmpty(unZippedFiles);
            Assert.Single(unZippedFiles);
            Assert.Equal("app.js", unZippedFiles.FirstOrDefault());
            AssertSuccessfulDeployMetric();
        }

        [Fact]
        public void UpdateFunctionFromZipFile()
        {
            _getExistingConfigurationResponse = new GetFunctionConfigurationResponse()
            {
                FunctionName = _uploadFunctionState.Request.FunctionName
            };

            _uploadFunctionState.SourcePath = Path.Combine(_testLocation.InputFolder, "sample.zip");

            _sut.UploadFunction(_uploadFunctionState);

            _uploadHelpers.Verify(mock => mock.WaitForUpdatableState(It.IsAny<IAmazonLambda>(), It.IsAny<string>()), Times.Once);
            _lambda.Verify(mock => mock.UpdateFunctionCode(It.IsAny<UpdateFunctionCodeRequest>()), Times.Once);
            AssertSuccessfulDeployMetric(false);
        }

        public void Dispose()
        {
            _testLocation.Dispose();
        }

        private void UnzipToOutputFolder(Stream stream)
        {
            var zipFile = Path.Combine(_testLocation.TestFolder, "foo.zip");
            using (var fileStream = File.OpenWrite(zipFile))
            {
                stream.CopyTo(fileStream);
            }

            ZipUtil.ExtractZip(zipFile, _testLocation.OutputFolder, true);
        }

        private IEnumerable<string> NormalizePaths(IEnumerable<string> paths)
        {
            return paths
                .Select(path => path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
        }

        private void AssertSuccessfulDeployMetric(bool isNewResource = true)
        {
            _toolkitContextFixture.TelemetryFixture.AssertTelemetryRecordCalls(1);
            LambdaAssert.MetricIsLambdaDeploy(_toolkitContextFixture.TelemetryFixture.LoggedMetrics.Single(),
                Result.Succeeded,
                new LambdaTelemetryUtils.RecordLambdaDeployProperties()
                {
                    AccountId = _uploadFunctionState.AccountId,
                    RegionId = _uploadFunctionState.Region.Id,
                    Runtime = _uploadFunctionState.Request.Runtime,
                    TargetFramework = "",
                    NewResource = isNewResource,
                });
        }
    }
}
