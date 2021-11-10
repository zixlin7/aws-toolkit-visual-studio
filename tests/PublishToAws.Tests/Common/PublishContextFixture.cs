using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.Notifications.Progress;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Package;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Publish.Services;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.Runtime;

using Microsoft.VisualStudio.Threading;

using Moq;

namespace Amazon.AWSToolkit.Tests.Publishing.Common
{
    /// <summary>
    /// Convenience class to create a starter set of PublishContext Mocks
    /// </summary>
    public class PublishContextFixture : ToolkitContextFixture
    {
        public PublishContext PublishContext { get; }

        public Mock<IAWSToolkitShellProvider> ToolkitShellProvider { get; } = new Mock<IAWSToolkitShellProvider>();
        public Mock<IPublishToAwsPackage> PublishPackage { get; } = new Mock<IPublishToAwsPackage>();
        public Mock<ICliServer> CliServer { get; } = new Mock<ICliServer>();

        public Mock<IPublishSettingsRepository> PublishSettingsRepository { get; } =
            new Mock<IPublishSettingsRepository>();
        public CancellationTokenSource PackageCancellationTokenSource { get; } = new CancellationTokenSource();

        public PublishContextFixture() : base()
        {
#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskCollection = new JoinableTaskContext();
#pragma warning restore VSSDK005
            var taskFactory = taskCollection.Factory;

            PublishPackage.SetupGet(mock => mock.JoinableTaskFactory).Returns(taskFactory);
            PublishPackage.SetupGet(mock => mock.DisposalToken).Returns(() => PackageCancellationTokenSource.Token);
            StubPublishPackageGetServiceAsync(typeof(SCliServer), CliServer.Object);

            PublishSettingsRepository.Setup(mock => mock.GetAsync()).ReturnsAsync(PublishSettings.CreateDefault());

            SetupShellProviderProgressDialog(new FakeProgressDialog());
            StubExecuteOnUIThread();

            PublishContext = new PublishContext()
            {
                PublishPackage = PublishPackage.Object,
                ToolkitContext = ToolkitContext,
                ToolkitShellProvider = ToolkitShellProvider.Object,
                InitializeCliTask = Task.CompletedTask,
                PublishSettingsRepository = PublishSettingsRepository.Object
            };
        }

        public void StubPublishPackageGetServiceAsync(Type serviceType, ICliServer cliServer)
        {
            PublishPackage.Setup(mock => mock.GetServiceAsync(serviceType)).ReturnsAsync(cliServer);
        }

        public void StubCliServerStartAsyncToThrow()
        {
            CliServer.Setup(mock => mock.StartAsync(It.IsAny<CancellationToken>()))
                .Throws<Exception>();
        }

        public void StubCliServerGetRestClientToThrow()
        {
            CliServer.Setup(mock => mock.GetRestClient(It.IsAny<Func<Task<AWSCredentials>>>()))
                .Throws<Exception>();
        }

        public void StubCliServerGetDeploymentClientToThrow()
        {
            CliServer.Setup(mock => mock.GetDeploymentClient())
                .Throws<Exception>();
        }

        public void SetupShellProviderProgressDialog(IProgressDialog progressDialog)
        {
            ToolkitShellProvider.Setup(mock => mock.CreateProgressDialog()).ReturnsAsync(progressDialog);
        }
        private void StubExecuteOnUIThread()
        {
            ToolkitShellProvider.Setup(mock => mock.ExecuteOnUIThread(It.IsAny<Action>()))
                .Callback((Action action) => action());
        }
    }
}
