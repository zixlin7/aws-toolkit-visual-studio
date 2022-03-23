using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ECS;
using Amazon.AWSToolkit.Lambda.WizardPages;
using Amazon.AWSToolkit.Regions;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class WizardExtensionMethodsTests
    {
        private readonly Mock<IAWSWizard> _wizard = new Mock<IAWSWizard>();
        private readonly IAWSWizard _nullWizard = null;

        private readonly AccountViewModel _account =
            new AccountViewModel(null, null, new SharedCredentialIdentifier("profile-name"), null);

        private readonly AccountViewModel _overloadTestAccount =
            new AccountViewModel(null, null, new SharedCredentialIdentifier("profile-overload-name"), null);

        private readonly ToolkitRegion _overloadTestRegion = new ToolkitRegion()
        {
            PartitionId = "aws",
            DisplayName = "US East",
            Id = "us-east-1",
        };

        private readonly ToolkitRegion _region = new ToolkitRegion()
        {
            PartitionId = "aws",
            DisplayName = "US West",
            Id = "us-west-2",
        };

        private object _property = null;

        public WizardExtensionMethodsTests()
        {
            SetupGetPropertyMock<AccountViewModel>();
            SetupGetPropertyMock<ToolkitRegion>();
            SetupOverloadGetPropertyMock();
        }

        [Fact]
        public void GetSelectedAccount()
        {
            _property = _account;
            var account = _wizard.Object.GetSelectedAccount();

            Assert.NotNull(account);
            Assert.Equal(_property, account);
        }

        [Fact]
        public void GetSelectedAccountOverload()
        {
            _property = _overloadTestAccount;
            var account = _wizard.Object.GetSelectedAccount(UploadFunctionWizardProperties.UserAccount);

            Assert.NotNull(account);
            Assert.Equal(_property, account);
        }

        [Fact]
        public void GetSelectedAccount_Null()
        {
            Assert.Null(_nullWizard.GetSelectedAccount());
        }

        [Fact]
        public void GetSelectedAccountOverload_Null()
        {
            Assert.Null(_nullWizard.GetSelectedAccount(null));
            Assert.Null(_nullWizard.GetSelectedAccount(""));
            Assert.Null(_nullWizard.GetSelectedAccount(UploadFunctionWizardProperties.UserAccount));
        }

        [Fact]
        public void SetSelectedAccount()
        {
            _wizard.Object.SetSelectedAccount(_account);
            _wizard.VerifySet(mock => mock[It.IsAny<string>()] = _account);
        }

        [Fact]
        public void SetSelectedAccountOverload()
        {
            _wizard.Object.SetSelectedAccount(_overloadTestAccount, UploadFunctionWizardProperties.UserAccount);
            _wizard.VerifySet(mock => mock[UploadFunctionWizardProperties.UserAccount] = _overloadTestAccount);
        }

        [Fact]
        public void GetSelectedRegion()
        {
            _property = _region;
            var region = _wizard.Object.GetSelectedRegion();

            Assert.NotNull(region);
            Assert.Equal(_property, region);
        }

        [Fact]
        public void GetSelectedRegionOverload()
        {
            _property = _overloadTestRegion;
            var region = _wizard.Object.GetSelectedRegion(PublishContainerToAWSWizardProperties.Region);

            Assert.NotNull(region);
            Assert.Equal(_property, region);
        }

        [Fact]
        public void GetSelectedRegion_Null()
        {
            Assert.Null(_nullWizard.GetSelectedRegion());
        }

        [Fact]
        public void GetSelectedRegionOverload_Null()
        {
            Assert.Null(_nullWizard.GetSelectedRegion(null));
            Assert.Null(_nullWizard.GetSelectedRegion(""));
            Assert.Null(_nullWizard.GetSelectedRegion(PublishContainerToAWSWizardProperties.Region));
        }

        [Fact]
        public void SetSelectedRegion()
        {
            _wizard.Object.SetSelectedRegion(_region);
            _wizard.VerifySet(mock => mock[It.IsAny<string>()] = _region);
        }

        [Fact]
        public void SetSelectedRegionOverload()
        {
            _wizard.Object.SetSelectedRegion(_overloadTestRegion, PublishContainerToAWSWizardProperties.Region);
            _wizard.VerifySet(mock => mock[PublishContainerToAWSWizardProperties.Region] = _overloadTestRegion);
        }

        [Fact]
        public void TestTryGetProperty()
        {
            // This test file isn't setup to test properties that don't exist (see SetupGetPropertyMock()), so just do all setup locally.
            Mock<IAWSWizard> wizard = new Mock<IAWSWizard>();

            wizard.Setup(mock => mock.IsPropertySet("validObject")).Returns(true);
            wizard.Setup(mock => mock.IsPropertySet("validInt")).Returns(true);
            wizard.Setup(mock => mock.IsPropertySet("validBool")).Returns(true);

            wizard.Setup(mock => mock.GetProperty("validObject")).Returns<string>(key => "Hi, I am a valid object! :)");
            wizard.Setup(mock => mock.GetProperty("validInt")).Returns<string>(key => 99);
            wizard.Setup(mock => mock.GetProperty("validBool")).Returns<string>(key => true);

            // Positive tests
            Assert.True(wizard.Object.TryGetProperty<string>("validObject", out string objectValue), "Property exists and is valid object");
            Assert.Equal("Hi, I am a valid object! :)", objectValue);

            Assert.True(wizard.Object.TryGetProperty<int>("validInt", out int intValue), "Property exists and is valid int");
            Assert.Equal(99, intValue);

            Assert.True(wizard.Object.TryGetProperty<bool>("validBool", out bool boolValue), "Property exists and is valid bool");
            Assert.True(boolValue);

            // Negative tests
            Assert.False(wizard.Object.TryGetProperty<string>("I don't exist", out objectValue), "Property doesn't exist");
            Assert.False(wizard.Object.TryGetProperty<bool>("validObject", out boolValue), "Property exists, but is wrong type");
        }

        private void SetupGetPropertyMock<T>()
        {
            _wizard.Setup(mock => mock.GetProperty<T>(It.IsAny<string>())).Returns<string>(key => (T) _property);
        }

        private void SetupOverloadGetPropertyMock()
        {
            _wizard.Setup(mock => mock.GetProperty<AccountViewModel>(UploadFunctionWizardProperties.UserAccount)).Returns<string>(key => (AccountViewModel) _property);
            _wizard.Setup(mock => mock.GetProperty<ToolkitRegion>(PublishContainerToAWSWizardProperties.Region))
                .Returns<string>(key => (ToolkitRegion) _property);
        }
    }
}
