using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.IdentityManagement;
using Amazon.AWSToolkit.IdentityManagement.Models;
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
        private readonly List<IamRole> _roles = new List<IamRole>();

        public IamRoleSelectionViewModelTests()
        {
#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskContext = new JoinableTaskContext();
#pragma warning restore VSSDK005

            SetupListIamRoles();

            _sut = new IamRoleSelectionViewModel(_iamEntities.Object, taskContext.Factory);
            _sut.CredentialsId = _sampleCredentialsId;
            _sut.Region = _sampleToolkitRegion;
        }

        private void SetupListIamRoles()
        {
            _iamEntities.Setup(mock =>
                    mock.ListIamRolesAsync(It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()))
                .ReturnsAsync(() => _roles);
        }

        [StaFact]
        public async Task RefreshRolesAsync()
        {
            int roleCount = 10;
            PopulateSampleRoles(roleCount);

            await _sut.RefreshRolesAsync();

            Assert.Equal(roleCount, _sut.RoleArns.Count);
            Assert.Equal(_roles.Select(r => r.Arn), _sut.RoleArns);

            _iamEntities.Verify(mock => mock.ListIamRolesAsync(_sut.CredentialsId, _sut.Region), Times.Once);
        }

        [StaFact]
        public async Task RefreshRolesAsync_WithServicePrincipalFilter()
        {
            _sut.ServicePrincipalFilter = "some-service-principal";

            int totalRoles = 10;
            PopulateSampleRoles(totalRoles);

            int expectedRoleCount = 4;
            var expectedRoles = _roles.Take(expectedRoleCount).ToList();
            expectedRoles.ForEach(r => r.AssumeRolePolicyDocument = $"some policy doc that includes: {_sut.ServicePrincipalFilter}");

            await _sut.RefreshRolesAsync();

            Assert.Equal(expectedRoleCount, _sut.RoleArns.Count);
            Assert.Equal(expectedRoles.Select(r => r.Arn), _sut.RoleArns);

            _iamEntities.Verify(mock => mock.ListIamRolesAsync(_sut.CredentialsId, _sut.Region), Times.Once);
        }

        private void PopulateSampleRoles(int roleCount)
        {
            Enumerable.Range(0, roleCount)
                .ToList()
                .ForEach(i =>
                {
                    _roles.Add(new IamRole()
                    {
                        Arn = $"sample-role-arn-{i}",
                        Id = $"sample-role-id-{i}",
                        Name = $"sample-role-name-{i}",
                    });
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
