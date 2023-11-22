using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AwsToolkit.CodeWhisperer.Tests.TestUtilities;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.Models.Text;

using FluentAssertions;

using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions
{
    [Collection(VsMockCollection.CollectionName)]
    public class ReferenceLoggerTests
    {
        private readonly FakeOutputWindow _outputWindow = new FakeOutputWindow();
        private readonly ReferenceLogger _sut;

        private readonly LogReferenceRequest _sampleReferenceRequest;

        public ReferenceLoggerTests(GlobalServiceProvider globalServiceProvider)
        {
            globalServiceProvider.Reset();

            var sampleReference = new SuggestionReference()
            {
                Name = "reference-name",
                Url = "reference-url",
                LicenseName = "license-name",
                StartIndex = 0,
                EndIndex = 5,
            };

            _sampleReferenceRequest = new LogReferenceRequest()
            {
                Suggestion =
                    new Suggestion()
                    {
                        Text = "abcdefghijkl",
                        References = new List<SuggestionReference>() { sampleReference }
                    },
                SuggestionReference = sampleReference,
                Filename = "some-filename",
                Position = new Position(0, 0),
            };

            var taskFactoryProvider = new ToolkitJoinableTaskFactoryProvider(ThreadHelper.JoinableTaskContext);
            _sut = new ReferenceLogger(null, taskFactoryProvider,
                (IServiceProvider serviceProvider, JoinableTaskFactory taskFactory) => _outputWindow);
        }

        [Fact]
        public async Task ShowAsync()
        {
            _outputWindow.IsShown = false;

            await _sut.ShowAsync();

            _outputWindow.IsShown.Should().BeTrue();
        }

        [Fact]
        public async Task LogReferenceAsync()
        {
            await _sut.LogReferenceAsync(_sampleReferenceRequest);

            _outputWindow.Messages.Should().HaveCount(1);
            var actualMessage = _outputWindow.Messages[0];
            actualMessage.Should().Contain(_sampleReferenceRequest.Filename);
            actualMessage.Should().Contain(_sampleReferenceRequest.SuggestionReference.Name);
            actualMessage.Should().Contain(_sampleReferenceRequest.SuggestionReference.Url);
            actualMessage.Should().Contain(_sampleReferenceRequest.SuggestionReference.LicenseName);
            actualMessage.Should().Contain("abcde");
        }
    }
}
