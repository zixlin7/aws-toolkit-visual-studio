using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.IdentityManagement;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using CommonUI.Models;

using Microsoft.VisualStudio.Threading;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class IamRoleSelectionViewModelTests
    {
        private readonly IamRoleSelectionViewModel _sut;
        private readonly Mock<IIamEntityRepository> _iamEntities = new Mock<IIamEntityRepository>();

        private readonly FakeCredentialIdentifier _sampleCredentialsId = FakeCredentialIdentifier.Create("fake-profile");
        private readonly ToolkitRegion _sampleToolkitRegion = new ToolkitRegion();
        private readonly List<string> _roleArns = new List<string>();

        public IamRoleSelectionViewModelTests()
        {
#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskContext = new JoinableTaskContext();
#pragma warning restore VSSDK005

            SetupListIamRoleArns();

            _sut = new IamRoleSelectionViewModel(_iamEntities.Object, taskContext.Factory);
            _sut.CredentialsId = _sampleCredentialsId;
            _sut.Region = _sampleToolkitRegion;
        }

        private void SetupListIamRoleArns()
        {
            _iamEntities.Setup(mock =>
                    mock.ListIamRoleArnsAsync(It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()))
                .ReturnsAsync(() => _roleArns);
        }

        [StaFact]
        public async Task RefreshRolesAsync()
        {
            int arnCount = 10;
            PopulateSampleRoleArns(arnCount);

            await _sut.RefreshRolesAsync();

            Assert.Equal(arnCount, _sut.RoleArns.Count);
            Assert.Equal(_roleArns, _sut.RoleArns);

            _iamEntities.Verify(mock => mock.ListIamRoleArnsAsync(_sut.CredentialsId, _sut.Region), Times.Once);
        }

        private void PopulateSampleRoleArns(int arnCount)
        {
            Enumerable.Range(0, arnCount)
                .ToList()
                .ForEach(i =>
                {
                    _roleArns.Add($"sample-role-arn-{i}");
                });
        }

        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("hello", "he")]
        [InlineData("hello", "l")]
        [InlineData("hello", "H")]
        [InlineData("Hello", "h")]
        public void IsObjectFiltered_Match(object candidate, string filter)
        {
            Assert.True(IamRoleSelectionViewModel.IsObjectFiltered(candidate, filter));
        }

        [Theory]
        [InlineData("hello", "hello!")]
        [InlineData("hello", "x")]
        [InlineData("hello", "Q")]
        public void IsObjectFiltered_NoMatch(object candidate, string filter)
        {
            Assert.False(IamRoleSelectionViewModel.IsObjectFiltered(candidate, filter));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        public void IsObjectFiltered_NoFilter(string filter)
        {
            Assert.True(IamRoleSelectionViewModel.IsObjectFiltered("hello world", filter));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(3)]
        [InlineData(false)]
        public void IsObjectFiltered_NonString(object candidate)
        {
            Assert.False(IamRoleSelectionViewModel.IsObjectFiltered(candidate, "3"));
        }
    }
}
