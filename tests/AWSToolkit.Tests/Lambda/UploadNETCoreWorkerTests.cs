using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.DeploymentWorkers;
using Amazon.Lambda;
using Moq;
using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class UploadNETCoreWorkerTests
    {
        private UploadFunctionController.UploadFunctionState _uploadFunctionState;

        [Fact]
        public void ValidateDockerImageTagIsEmpty()
        {
            SetUploadFunctionState(null, null);
            Assert.Equal(0, UploadNETCoreWorker.GetDockerImageTag(_uploadFunctionState).Length);
            SetUploadFunctionState("", "");
            Assert.Equal(0, UploadNETCoreWorker.GetDockerImageTag(_uploadFunctionState).Length);
        }

        [Fact]
        public void ValidateDockerImageTagHasOnlyRepo()
        {
            SetUploadFunctionState("someRepo", null);
            Assert.Equal("someRepo", UploadNETCoreWorker.GetDockerImageTag(_uploadFunctionState));
            SetUploadFunctionState("someRepo", "");
            Assert.Equal("someRepo", UploadNETCoreWorker.GetDockerImageTag(_uploadFunctionState));
        }

        [Fact]
        public void ValidateDockerImageTagHasRepoAndTag()
        {
            SetUploadFunctionState("someRepo", "someTag");
            Assert.Equal("someRepo:someTag", UploadNETCoreWorker.GetDockerImageTag(_uploadFunctionState));
        }

        private void SetUploadFunctionState(string imageRepo, string imageTag)
        {
            _uploadFunctionState = new UploadFunctionController.UploadFunctionState()
            {
                ImageRepo = imageRepo,
                ImageTag = imageTag
            };
        }

    }
}