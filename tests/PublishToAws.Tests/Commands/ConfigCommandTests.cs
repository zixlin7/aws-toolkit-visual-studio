﻿using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class ConfigCommandTests
    {
        private readonly PublishFooterCommandFixture _commandFixture = new PublishFooterCommandFixture();
        private TestPublishToAwsDocumentViewModel ViewModel => _commandFixture.ViewModel;
        private readonly ConfigCommand _sut;

        public ConfigCommandTests()
        {
            _sut = new ConfigCommand(ViewModel);
        }

        [Fact]
        public void ExecuteCommand()
        {
            _sut.Execute(null);

            Assert.Equal(PublishViewStage.Configure, ViewModel.ViewStage);
        }

        [Fact]
        public void CanExecute()
        {
            Assert.True(_sut.CanExecute(null));
        }

        [Theory]
        [InlineData(PublishViewStage.Configure)]
        [InlineData(PublishViewStage.Publish)]
        public void CanExecute_NotCorrectView(PublishViewStage viewStage)
        {
            ViewModel.ViewStage = viewStage;
            Assert.False(_sut.CanExecute(null));
        }
    }
}
