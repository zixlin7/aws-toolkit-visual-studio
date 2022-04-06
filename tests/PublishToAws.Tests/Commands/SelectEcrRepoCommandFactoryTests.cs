using System.Windows.Input;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;

using Moq;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class SelectEcrRepoCommandFactoryTests
    {
        private readonly EcrRepositoryConfigurationDetail _repoDetail = new EcrRepositoryConfigurationDetail()
        {
            Value = "some-repo",
        };

        private readonly Mock<IPublishToAwsProperties> _publishProperties = new Mock<IPublishToAwsProperties>();
        private readonly Mock<IDialogFactory> _dialogFactory = new Mock<IDialogFactory>();
        private readonly Mock<IEcrRepositorySelectionDialog> _ecrRepoDialog = new Mock<IEcrRepositorySelectionDialog>();
        private readonly ICommand _command;

        public SelectEcrRepoCommandFactoryTests()
        {
            _dialogFactory.Setup(mock => mock.CreateEcrRepositorySelectionDialog())
                .Returns(_ecrRepoDialog.Object);

            SetDialogRepositoryName("foo");

            _command = CreateCommand();
        }

        [Fact]
        public void DialogShouldUpdateProperty()
        {
            SetDialogResult(true);

            _command.Execute(null);
            Assert.Equal("foo", _repoDetail.Value);
        }

        [Fact]
        public void CancelDialogShouldNotUpdateProperty()
        {
            SetDialogResult(false);

            _command.Execute(null);
            Assert.Equal("some-repo", _repoDetail.Value);
        }

        private void SetDialogResult(bool showDialogResult)
        {
            _ecrRepoDialog.Setup(mock => mock.Show()).Returns(showDialogResult);
        }

        private void SetDialogRepositoryName(string repoName)
        {
            _ecrRepoDialog.SetupGet(mock => mock.RepositoryName).Returns(repoName);
        }

        private ICommand CreateCommand()
        {
            return SelectEcrRepoCommandFactory.Create(_repoDetail, _publishProperties.Object, _dialogFactory.Object);
        }
    }
}
