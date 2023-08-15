using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.Runtime;
using Amazon.SSO;
using Amazon.SSO.Model;

using AWSToolkit.Tests.Credentials.Core;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CommonUI.CredentialProfiles.AddEditWizard
{
    public class PaginatedEnumerable<T> : List<T>, IPaginatedEnumerable<T> { }

    public class SsoConnectedStepViewModelTests : IAsyncLifetime
    {
        private SsoConnectedStepViewModel _sut;

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private readonly ServiceProvider _serviceProvider = new ServiceProvider();

        private readonly Mock<IAddEditProfileWizard> _addEditProfileWizardMock = new Mock<IAddEditProfileWizard>();

        private readonly List<ICredentialIdentifier> _credentialIdentifiers = new List<ICredentialIdentifier>();

        public async Task InitializeAsync()
        {
            var connectionManagerMock = new Mock<IAwsConnectionManager>();
            connectionManagerMock.SetupGet(mock => mock.IdentityResolver).Returns(new FakeIdentityResolver());

            _toolkitContextFixture.ToolkitContext.ConnectionManager = connectionManagerMock.Object;
            _toolkitContextFixture.CredentialManager.Setup(mock => mock.GetCredentialIdentifiers()).Returns(_credentialIdentifiers);

            var ssoProfilePropertiesMock = new Mock<ISsoProfilePropertiesProvider>();
            ssoProfilePropertiesMock.Setup(mock => mock.ProfileProperties).Returns(new ProfileProperties());

            _serviceProvider.SetService(_toolkitContextFixture.ToolkitContext);
            _serviceProvider.SetService(_addEditProfileWizardMock.Object);
            _serviceProvider.SetService(ssoProfilePropertiesMock.Object);

            _sut = await ViewModelTests.BootstrapViewModel<SsoConnectedStepViewModel>(_serviceProvider);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public void ExistingDuplicateProfileNameIsError()
        {
            var roleName = "myRoleName";
            var accountId = "123456789012";
            var sessionName = "mySsoSession";
            var profileName = $"{sessionName}-{roleName}-{accountId}";

            _serviceProvider.RequireService<ISsoProfilePropertiesProvider>().ProfileProperties.Name = sessionName;
            _credentialIdentifiers.Add(new SharedCredentialIdentifier(profileName));

            AddToSelectedRoleAccounts(roleName, accountId);

            Assert.False(_sut.SaveCommand.CanExecute(null));
        }

        [Fact]
        public void BackToConnectionSetupGoesBackToConfigurationDetails()
        {
            _sut.BackToConnectionSetupCommand.Execute(null);
            Assert.Equal(WizardStep.Configuration, _serviceProvider.RequireService<IAddEditProfileWizard>().CurrentStep);
        }

        [Fact]
        public void SaveCreatesMultipleProfilesWhenMultipleRoleAccountsSelected()
        {
            var saveCount = 5;
            var roleName = "myRoleName";
            var accountId = "123456789012";
            var sessionName = "mySsoSession";

            _serviceProvider.RequireService<ISsoProfilePropertiesProvider>().ProfileProperties.Name = sessionName;

            for (var i = 0; i < saveCount; ++i)
            {
                AddToSelectedRoleAccounts($"{roleName}{i}", accountId);
            }

            _sut.SaveCommand.Execute(null);

            _addEditProfileWizardMock.Verify(wiz => wiz.SaveAsync(It.IsAny<ProfileProperties>(), It.IsAny<CredentialFileType>(),
                It.IsAny<bool>()), Times.Exactly(saveCount));
        }

        [Fact]
        public async Task LoadAccountRolesClearsAndSetsSsoAccountRoles()
        {
            var accountId = "123456789012";
            var token = "whatever";
            var region = RegionEndpoint.USEast1;

            var ssoClientMock = new Mock<IAmazonSSO>();

            // Mock ListAccounts
            var accounts = new PaginatedEnumerable<AccountInfo>() { new AccountInfo() { AccountId = accountId } };
            var listAccountsPaginatorMock = new Mock<IListAccountsPaginator>();
            listAccountsPaginatorMock.Setup(mock => mock.AccountList).Returns(accounts);
            ssoClientMock.Setup(mock => mock.Paginators.ListAccounts(It.IsAny<ListAccountsRequest>())).Returns(listAccountsPaginatorMock.Object);

            // Mock ListAccountRoles
            var roles = new PaginatedEnumerable<RoleInfo>()
            {
                new RoleInfo() { AccountId = accountId, RoleName = "myRole1" },
                new RoleInfo() { AccountId = accountId, RoleName = "myRole2" }
            };
            var listAccountRolesPaginatorMock = new Mock<IListAccountRolesPaginator>();
            listAccountRolesPaginatorMock.Setup(mock => mock.RoleList).Returns(roles);
            ssoClientMock.Setup(mock => mock.Paginators.ListAccountRoles(It.IsAny<ListAccountRolesRequest>())).Returns(listAccountRolesPaginatorMock.Object);

            Assert.Empty(_sut.SsoAccountRoles);

            await _sut.LoadAccountRoles(token, region, ssoClientMock.Object);

            Assert.Equal(_sut.SsoAccountRoles.Count, 2);

            _toolkitContextFixture.ToolkitHost.Verify(th => th.ShowError(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task NoRoleAccountsFoundShowsError()
        {
            var token = "whatever";
            var region = RegionEndpoint.USEast1;

            var ssoClientMock = new Mock<IAmazonSSO>();

            await _sut.LoadAccountRoles(token, region, ssoClientMock.Object);

            _toolkitContextFixture.ToolkitHost.Verify(th => th.ShowError(It.IsAny<string>()), Times.Once());
        }

        private RoleInfo AddToSelectedRoleAccounts(string roleName, string accountId)
        {
            var roleInfo = new RoleInfo()
            {
                AccountId = accountId,
                RoleName = roleName
            };
            _sut.SelectedSsoAccountRoles.Add(roleInfo);

            return roleInfo;
        }
    }
}
