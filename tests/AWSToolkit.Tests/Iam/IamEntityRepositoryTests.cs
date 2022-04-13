using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.IdentityManagement;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Iam
{
    public class IamEntityRepositoryTests
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly IamEntityRepository _sut;

        private readonly Mock<AmazonIdentityManagementServiceClient> _iamClient =
            new Mock<AmazonIdentityManagementServiceClient>();

        public IamEntityRepositoryTests()
        {
            SetupClientManagerWithIamMock();

            _sut = new IamEntityRepository(_toolkitContextFixture.ToolkitContext);
        }

        private void SetupClientManagerWithIamMock()
        {
            _toolkitContextFixture.ServiceClientManager
                .Setup(mock =>
                    mock.CreateServiceClient<AmazonIdentityManagementServiceClient>(It.IsAny<ICredentialIdentifier>(),
                        It.IsAny<ToolkitRegion>()))
                .Returns(_iamClient.Object);
        }

        [Fact]
        public async Task ListIamRolesAsync()
        {
            int roleCount = 5;
            SetupListRoles(roleCount);

            var roles = await _sut.ListIamRolesAsync(FakeCredentialIdentifier.Create("profile-name"), new ToolkitRegion());
            Assert.Equal(roleCount, roles.Count);
        }

        private void SetupListRoles(int roleCount)
        {
            _iamClient.Setup(mock => mock.ListRolesAsync(It.IsAny<ListRolesRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new ListRolesResponse()
                {
                    Roles = Enumerable.Range(0, roleCount).Select(i => new Role() { Arn = $"some-role-arn-{i}" }).ToList()
                });
        }
    }
}
