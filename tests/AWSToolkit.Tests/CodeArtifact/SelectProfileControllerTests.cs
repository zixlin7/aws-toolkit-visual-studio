using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeArtifact.Controller;
using Amazon.AWSToolkit.CodeArtifact.View;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Shared;
using Moq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace AWSToolkit.Tests.CodeArtifact
{
    public class SelectProfileControllerTests
    {
        [StaFact]
        public void NoRegisteredAccount()
        {
            List<AccountViewModel> registeredAccounts = new List<AccountViewModel>();
            var mockShell = new Mock<IAWSToolkitShellProvider>();
            var selectProfileController = new SelectProfileController(registeredAccounts, mockShell.Object);
            var result = selectProfileController.Execute(null);
            Assert.False(result.Success);
        }

        [Fact]
        public void SelectedAccountNull()
        {
            List<AccountViewModel> registeredAccounts = new List<AccountViewModel>();
            registeredAccounts.Add(It.IsAny<AccountViewModel>());
            var mockShell = new Mock<IAWSToolkitShellProvider>();

            var selectProfileController = new SelectProfileController(registeredAccounts, mockShell.Object);
            var result = selectProfileController.Persist(null);
            Assert.False(result.Success);
        }
    }
}
