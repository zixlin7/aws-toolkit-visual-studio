using System;
using System.IO;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

using LearnMoreCommandFactory = Amazon.AWSToolkit.Commands.LearnMoreCommandFactory;

namespace AWSToolkit.Tests.Commands
{
    public class LearnMoreCommandTests
    {
        [Fact]
        public void ShouldOpenUrl()
        {
            // arrange.
            var spyShellProvider = new SpyBrowserToolkitShellProvider();
            var expectedUrl = "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/publish-experience.html";

            var command = LearnMoreCommandFactory.Create(spyShellProvider);

            // act.
            command.Execute(null);

            // assert.
            Assert.Equal(expectedUrl, spyShellProvider.Url);
        }

        [Fact]
        public void ShouldWrapException()
        {
            var shellProvider = CreateShellProviderThatThrows(new IOException("Browser could not be opened"));
            var command = LearnMoreCommandFactory.Create(shellProvider);
            Assert.Throws<CommandException>(() => command.Execute(null));
        }

        private IAWSToolkitShellProvider CreateShellProviderThatThrows(Exception e)
        {
            var shellProvider = new ToolkitContextFixture().ToolkitHost;
            shellProvider.Setup(mock => mock.OpenInBrowser(It.IsAny<string>(), It.IsAny<bool>())).Throws(e);
            return shellProvider.Object;
        }

        public class SpyBrowserToolkitShellProvider : NoOpToolkitShellProvider
        {
            public string Url { get; private set; }

            public override void OpenInBrowser(string url, bool preferInternalBrowser)
            {
                Url = url;
            }
        }
    }
}
